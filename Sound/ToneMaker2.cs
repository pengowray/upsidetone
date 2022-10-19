using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.Metrics;

using NAudio.Mixer;
using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Navigation;
using System.Windows.Media.Animation;
//using Voicemeeter;

//using NWaves.Audio;

namespace upSidetone.Sound {

    //rewrite of ToneMaker_Old (previously called "Sounder")

    public class ToneMaker : IDisposable {

        //TODO: per lever
        const double wpm = 12.0;
        double ditSeconds = 60.0 / (50.0 * wpm);

        //SwitchableDelayedSound Playing;
        //LeverKind PlayingLever;
        //SwitchableDelayedSound Next;
        //LeverKind NextLever;
        List<SwitchableDelayedSound> Playing = new();
        //public List<LeverKind> Played = new(); // dits and dahs. or just a string of ".-*" ?

        BeepAttackDecay Options = BeepAttackDecay.Preset_Smooth;
        //BeepAttackDecay Options = BeepAttackDecay.Preset_SharpNoClick; // not working? TODO (endless beep)

        //SwitchableDelayedSound AndThen;

        AudioOut AudioOut;
        ScoreProvider ScoreProvider;
        //MixingSampleProvider ScoreProvider;
        Beep Beep;
        private bool disposedValue;

        Queue<LeverKind> Required = new();
        LeverKind[]? Fill;
        int FillPos = 0;

        public WaveFormat ParentWaveFormat => AudioOut?.Format;

        // note: We use mono because mono is assumed by FadeOutSampleProvider and SwitchableDelayedSound;
        // TODO: make them work with stereo / abitrary channels
        public WaveFormat WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(ParentWaveFormat.SampleRate, 1);

        public ToneMaker(AudioOut audioOut) {
            AudioOut = audioOut;
            //ScoreProvider = new MixingSampleProvider(ParentWaveFormat);
            //ScoreProvider = new ScoreProvider(ParentWaveFormat);
            ScoreProvider = new ScoreProvider(WaveFormat);
            ScoreProvider.ReadFully = true;
            Beep = new Beep(WaveFormat);
        }

        public void Enable() {
            AudioOut.Mixer.AddMixerInput(ScoreProvider.ToStereo());
        }

        public void ListenToLevers(Levers levers) {
            levers.LeverDown += Levers_LeverDown;
            levers.LeverUp += Levers_LeverUp;
            levers.LeverDoubled += Levers_LeverDoubled;
        }

        private void Levers_LeverDoubled(Levers levers, LeverKind lever, bool doublePressed, bool priorityIncreased) {
            // todo
        }

        private void Levers_LeverUp(Levers levers, LeverKind lever, LeverKind require, LeverKind[]? fill) {
            //TODO: check if correct lever released
            //TODO: check if other lever is down

            //if bringing up active straight key...

            if (lever == LeverKind.Straight || lever == LeverKind.PoliteStraight) {
                //TODO: bring up straight key

                var toUp = Playing.Where(sound => !sound.IsLockedIn && sound.Lever == lever);
                //toUp.Stop(); //TODO
            }

            // the rest is the same as Levers_LeverDown()
            Levers_LeverDown(levers, lever, require, fill);
        }


        private void Levers_LeverDown(Levers levers, LeverKind lever, LeverKind require, LeverKind[]? fill) {
            if (require != LeverKind.None) Required.Enqueue(require);
            Fill = fill;
            FillPos = 0;

            //ThreeBeeps(levers, lever);
            RefillBeeps();
        }

        private SwitchableDelayedSound? GetNextUpAndDeleteRest(bool justdel = false) { 
            var next = Playing.FirstOrDefault(p => !p.IsLockedIn);
            if (next == null) {
                return null;
            }

            List<SwitchableDelayedSound> newPlaying = new();
            foreach (var sound in Playing.ToArray()) {
                if (!sound.IsLockedIn) {
                    if (!justdel && !newPlaying.Any()) {
                        newPlaying.Add(sound);
                    } else {
                        // delete
                        ScoreProvider.RemoveMixerInput(sound);
                        sound.SetChoice(null);
                        sound.SampleStarted -= Next_SampleStarted;
                    }
                }
            }
            Playing = newPlaying;

            return Playing.FirstOrDefault();
        }


        private LeverKind GetNextLever() {
            if (Required.TryDequeue(out LeverKind lever)) {
                // assume not None because we don't let that get queued
                if (lever == LeverKind.None) {
                    //try again. (risky recursion)
                    return GetNextLever();
                }
                return lever;
            }

            if (Fill == null || Fill.Length == 0) {
                // nothing queued
                return LeverKind.None;
            }

            var next = Fill[FillPos];
            FillPos = (FillPos + 1) % Fill.Length;
            return next;
        }

        private void FillMissingBeeps() {
            var playing = Playing.Where(x => !x.IsLockedIn);
            if (!playing.Any()) {
                RefillBeeps();
            } else {
                var last = playing.Last();
                int count = playing.Count();
                for (int i = count; i <= 3; i++) {
                    var nextLever = GetNextLever();
                    if (nextLever == LeverKind.None) 
                        return;

                    var beep = MakeBeep(nextLever, last);
                    last = beep;
                }
            }
        }

        private void RefillBeeps() {

            //TODO: don't remove "required" beeps

            var nextLever = GetNextLever();

            if (nextLever == LeverKind.None) {
                GetNextUpAndDeleteRest(true);
                return;
            }

            var next = GetNextUpAndDeleteRest();

            if (next != null && !next.IsLockedIn) {
                var beep = MakeBeep(nextLever, next, actuallyReplace: true);

                var afterNextLever = GetNextLever();
                if (afterNextLever != LeverKind.None) {
                    var afterNextBeep = MakeBeep(afterNextLever, beep);
                }
                return;

            } else {

                var sound = MakeBeep(nextLever);

                var afterNextLever = GetNextLever();
                if (afterNextLever != LeverKind.None) {
                    var afterNextBeep = MakeBeep(afterNextLever, sound);

                    var oneMore = GetNextLever();
                    if (oneMore != LeverKind.None) {
                        var oneMoreBeep = MakeBeep(oneMore, afterNextBeep);
                    }
                }

            }
        }

        private void ThreeBeeps(Levers levers, LeverKind lever) {

            //var next = Playing.FirstOrDefault(p => !p.IsLockedIn);
            var next = GetNextUpAndDeleteRest();
            if (next != null && !next.IsLockedIn) {

                var beep = MakeBeep(lever, next);

                // queue after next
                var afterNextBeep = MakeBeep(lever, beep);
                return;
            }

            var sound = MakeBeep(lever);

            if (lever == LeverKind.Dits || lever == LeverKind.Dahs) {
                //TODO: check if dit or dah next
                // queue next sound
                var nextBeep = MakeBeep(lever, sound);
                var afterNextBeep = MakeBeep(lever, nextBeep);

            }

        }

        private void Next_SampleStarted(SwitchableDelayedSound sender) {

            // run in a thread so can get back sooner though not sure if it's actually needed

            _ = Task.Run(() => {
                sender.SampleStarted -= Next_SampleStarted;
                bool inPlaying = Playing.Contains(sender);
                if (inPlaying) {
                    //ThreeBeeps(null, LeverKind.Dits);
                    FillMissingBeeps();
                }

            }
            ); // .ContinueWith(LogResult));

            /*
            //TODO: lock first
            if (sender.Chosen == null) {
                // clear played
                List<SwitchableDelayedSound> NewPlaying = new();
                foreach (var item in Playing) {
                    if (item.Chosen != null && !item.IsLockedIn) {
                        NewPlaying.Add(item);
                    } else {
                        // probably not needed
                        item.SampleStarted -= Next_SampleStarted;
                    }
                }
                Playing = NewPlaying;
            }
            */

        }

        private SwitchableDelayedSound MakeBeep(LeverKind lever, SwitchableDelayedSound? afterThis = null, bool actuallyReplace = false) {
        
            //double ditLen, double whenInDits = 0;
            double? ditLen = null;
            if (lever == LeverKind.Dits) ditLen = 1;
            if (lever == LeverKind.Dahs) ditLen = 3;

            long ditLenSamples = (long)(ditSeconds * WaveFormat.SampleRate);
            long? beepSamples = ditLen.HasValue ? (long)(ditLen * ditSeconds * WaveFormat.SampleRate) : null;

            //long whenSamples = (long)(ditSeconds * whenInDits * AudioOut.Format.SampleRate);
            //long currentPos = ScoreProvider.CurrentSamplePos; // used here for calculating waveform phase; not for positioning the note
            //long currentPos = (!Options.HasFlag(BeepAttackDecay.InfiniteWave)) ? ScoreProvider.CurrentSamplePos : 0;
            long currentPos = 0; //  0 + whenSamples; // todo: replace 0 with currentPos depending on settings

            var beep = Beep.MakeBeep(currentPos, beepSamples, johncage: ditLenSamples, options: Options);

            // queue empty next? nah
            //var next = new SwitchableDelayedSound(WaveFormat);
            //next.StartAt = ditLenSamples;
            //var beepWithNext = beep.FollowedBy(next);
            //var sound = new SwitchableDelayedSound(beepWithNext);

            SwitchableDelayedSound sound;
            if (actuallyReplace) {
                sound = afterThis;
                sound.SetChoice(beep);
                afterThis = null;
            } else {
                sound = new SwitchableDelayedSound(beep);
            }

            //sound.Lever = lever;
            //sound.Next = next;

            if (beepSamples != null) {
                sound.DurationSamples = beepSamples.Value + ditLenSamples;
            } else {
                sound.DurationSamples = 0;
            }

            if (afterThis != null) { 
                // wont enter here if actuallyReplace == true (because afterThis is nulled above)
                
                if (sound.DurationSamples == 0) {
                    // crash rather than do wrong (for debuging)
                    throw new ArgumentException("Can't start after when durationless");
                    //TODO: fade out and play after
                }
                sound.StartAt = afterThis.StartAt + afterThis.DurationSamples;

                if (afterThis.Next == null) {
                    afterThis.Next = sound;
                } else {
                    //TODO: remove event
                    afterThis.Next.SetChoice(sound);
                }
            } else if (!actuallyReplace) {
                //TODO: set to null and let it get autofilled... but then harder to queue after it
                sound.StartAt = ScoreProvider.SampleCursor;
            }

            //sound.StartAt = whenSamples;
            //sound.StartAt = // TODO: delay

            //todo
            //StraightAdsr = new AdsrSampleProvider(faded.ToMono(1, 0)) {
            //AttackSeconds = AttackSeconds,
            //ReleaseSeconds = ReleaseSeconds // does not get used unless stopping early

            if (!actuallyReplace) {
                Playing.Add(sound);
                ScoreProvider.AddMixerInput(sound);
                sound.SampleStarted += Next_SampleStarted; // to clear or refill Playing list
            }

            return sound;
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Sounder()
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

