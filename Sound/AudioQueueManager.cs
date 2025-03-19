using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

using NAudio.Wave;
using NWaves.Filters.Base;
using static System.Windows.Forms.Design.AxImporter;

namespace upSidetone.Sound {

    public class AudioQueueManager : IDisposable {

        // abstract out the sound score and fill list? into a logical thing separate to audio generation?
        // may end up as a rewrite of ToneMaker2

        //public WaveFormat WaveFormat { get; private set; }

        ScoreProvider Score; // for SampleCursor + WaveFormat (ok managed by this class now)
        Timings Timing;
        Beep Beep;
        private double Tone = 440;

        public bool SetTone(double tone) {
            // returns true on success

            if (tone < 2 || tone > 20000) {
                // throw error?
                return false;
            }

            Tone = tone;

            if (Beep != null)
                Beep.Tone = tone;

            return true;
        }

        //KeyerState State = new();
        //SymbolQueue? SymbolQueue = null;
        KeyerQueueState QueueState = new() {
            FillState = new FillState()
        };

        ConcurrentQueue<KeyingEventArgs> KeyingQueued = new();
        //KeyingEventArgs[]? KeyingQueued = null;
        //ConcurrentQueue<QSound> SoundQueued = new();

        // Used to signal the waiting thread
        AutoResetEvent AutoEvent = new AutoResetEvent(initialState: false);
        System.Timers.Timer Timer; // or could use System.Threading.Timer 
        Thread Thread;
        bool Cancel = false; // if true, stop running thread (TODO: use CancellationToken instead?)
        private bool disposedValue;

        public AudioQueueManager(WaveFormat waveFormat) {
            Timing = new Timings(waveFormat); // TODO: make sure to update samplerate
            Score = new ScoreProvider(waveFormat);
        }


        public void ListenToKeyer(Keying levers) {
            levers.LeverKeyed += Levers_LeverKeyed;
        }

        public void StopListeningToKeyer(Keying levers) {
            levers.LeverKeyed -= Levers_LeverKeyed;
        }

        private void Levers_LeverKeyed(KeyingEventArgs args) {
            KeyingQueued.Enqueue(args);
            AutoEvent.Set();
        }

        public void StartThread() {
            try {
                Thread = new Thread(new ThreadStart(() => RunLoop()));
                Thread.Start();
                Timer = new();
                Timer.Interval = 50; // ms
                Timer.Elapsed += (object? sender, ElapsedEventArgs e) => AutoEvent.Set();

            } catch (Exception e) {
                Debug.WriteLine(e);
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }

        }


        private void RunLoop() {
            while (!Cancel) { 
                // Wait for the signal
                AutoEvent.WaitOne();

                // Do some processing after the signal is received
                RunOnce();
            }

        }


        private bool RunOnce() {

            // todo: if two events queued at once, handle specially (e.g. two paddles released together)

            // todo: allow process to be interrupted (e.g. if a sound starts playing), and start over.
            long readCursor = Score.SampleCursor;

            var oldState = QueueState;

            var newState = ProcessQueue(readCursor);
            newState = Refill(newState, readCursor);

            DoAudio(newState, oldState);

            return true; // success
        }

        private void DoAudio(KeyerQueueState? newState, KeyerQueueState oldState) {
            if (Beep == null) {
                Beep = new Beep(Timing);
            }
            if (newState != null && newState.SymbolQueue != null) { 
                foreach (var symbol in newState.SymbolQueue.Items()) {
                    if (symbol != null) {
                        if (symbol.Sound == null) {
                            // add sound
                            var sound = new SwitchableDelayedSound(Timing.WaveFormat);
                            sound.StartAt = symbol.Timestamp;
                            sound.DurationSamples = symbol.
                            ISampleProvider beep = null;
                            if (sound.Lever != LeverKind.None) {
                                //beep = Beep.MakeBeep(sound.StartAt, sound.DurationSamples, johncage: ditLenSamples, options: BeepAttackDecay.Preset_Smooth);
                                beep = Beep.MakeBeep(sound.StartAt, sound.DurationSamples, johncage: ditLenSamples, options: BeepAttackDecay.Preset_Smooth);
                                sound.SetChoice(beep);
                            }
                            
                            symbol.Sound = sound;
                        }
                    }
                }
            }

            //TODO: remove dead Sounds from oldState that aren't in newState

            //TODO: optimize: don't recreate Sounds if possible
        }

        private KeyerQueueState? ProcessQueue(long readCursor) {

            //TODO: optimize
            var state = QueueState;
            while (true) {
                bool found = KeyingQueued.TryDequeue(out var item);
                if (!found || item == null)
                    break;

                var newState = new KeyerQueueState() {
                    RequireNext = item.requireNext,
                    FillState = new FillState() {
                        Fill = item.fill,
                        BFill = item.bFill,
                    },
                    StraightDown = state?.StraightDown ?? LeverKind.None, // keep previous
                    SymbolQueue = state?.SymbolQueue // keep previous
                };
                state = newState;

                //TODO: special case for readCursor == 0 -- i.e. haven't started yet

                state = QueueState?.ClearDoneAndQueued(readCursor);
                state = AddRequired(state, readCursor);
            }

            return state;
        }

        public KeyerQueueState? AddRequired(KeyerQueueState? queue, long time) {
            // rewrite of first half of ToneMaker.GetNextLever()

            var requiredNext = queue?.RequireNext ?? LeverKind.None;

            if (requiredNext == LeverKind.None || queue == null) {
                return queue;
            }

            // check if straight key already down
            if (queue.StraightDown != LeverKind.None) { 
                //TODO: raise straight key
            }

            //var queueSound = new(, Requiredness.Required);

            var newState = queue.ShallowClone();

            long? len = Timing.LeverSamples(requiredNext);

            var item = new SymbolItem(time, len, Timing.ToSamples(1), requiredNext, required: true, null);
            newState.AddNextInPlace(item);
            newState.RequireNext = LeverKind.None;

            if (requiredNext == LeverKind.Oscillate || requiredNext == LeverKind.Straight) {
                newState.StraightDown = requiredNext;
            }

            return newState;
        }

        public KeyerQueueState? Refill(KeyerQueueState? queueState, long time) {
            // assumes we're ready to add fill items (e.g. cleared old ones)
            if (queueState == null)
                return null;

            if (queueState.FillState == null)
                return queueState; // i think

            int fillmore = 3 - (queueState.SymbolQueue?.Length ?? 0);

            if (fillmore <= 0) {
                return queueState;
            }

            //List<SymbolItem> items = new List<SymbolItem>(4);
            var newState = queueState.DeeperClone();

            while (fillmore > 0) {
                var fill = newState.FillState?.NextFill();
                if (fill == null)
                    break;

                var timestamp = newState.GetNextTimestamp(time) ?? time; // TODO: take time from end of previous fill if available
                //Debug.Assert(timestamp.HasValue, "Timestamp should always have a value here");
                fill.Timestamp = timestamp;
                fill.Length = Timing.LeverSamples(fill.Lever);
                fill.Gap = Timing.LeverSamples(LeverKind.Dit);

                newState.AddNextInPlace(fill);

                fillmore--;
            }

            return newState;
        }
        

        public void Keyed(KeyingEventArgs args) {
            KeyingQueued?.Enqueue(args);
            // todo: wake check thread 
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null

                Cancel = true;
                Timer?.Stop();
                Timer = null;
                Thread = null;
                Score = null; // Stop it first?

                // TODO: what else?

                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AudioQueueManager()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
