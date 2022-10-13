using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Mixer;
using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using static System.Net.Mime.MediaTypeNames;
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

        //BeepAttackDecay Options = BeepAttackDecay.Preset_Smooth;
        BeepAttackDecay Options = BeepAttackDecay.Preset_SharpNoClick;

        //SwitchableDelayedSound AndThen;

        AudioOut AudioOut;
        //ScoreProvider ScoreProvider;
        MixingSampleProvider ScoreProvider;
        Beep Beep;
        private bool disposedValue;

        public WaveFormat WaveFormat => AudioOut?.Format;

        public ToneMaker(AudioOut audioOut) {
            AudioOut = audioOut;
            //ScoreProvider = new ScoreProvider(audioOut.Format);
            ScoreProvider = new MixingSampleProvider(WaveFormat);
            ScoreProvider.ReadFully = true;
            Beep = new Beep(WaveFormat);
        }

        public void Enable() {
            AudioOut.Mixer.AddMixerInput(ScoreProvider);
        }

        public void ListenToLevers(Levers levers) {
            levers.LeverDown += Levers_LeverDown;
            levers.LeverUp += Levers_LeverUp;
            levers.LeverDoubled += Levers_LeverDoubled;
        }

        private void Levers_LeverDoubled(Levers levers, LeverKind lever, bool doublePressed, bool priorityIncreased) {
            // todo
        }

        private void Levers_LeverUp(Levers levers, LeverKind lever) {
            //TODO: check if correct lever released
            //TODO: check if other lever is down

            //if bringing up active straight key...


            //if (levers.Mode == )

            var down = levers.GetLeversDown();
            if (down.Any()) {
                //TODO: e.g. do other lever

            }

            var toUp = Playing.Where(sound => !sound.IsLockedIn && sound.Lever == lever);

            foreach (var sound in toUp) {
                if (sound != null) {
                    sound.SetChoice(null);
                    //toUp.SampleStarted -= Next_SampleStarted; //todo: need this for ones that wont ever be triggered
                }
            }

        }


        private void Levers_LeverDown(Levers levers, LeverKind lever) {

            var next = Playing.FirstOrDefault(p => !p.IsLockedIn);
            if (next != null) {
                // todo: delete anything already after next in Playing

                var beep = MakeBeep(lever);
                next.SetChoice(beep);
                

                // queue after next
                var afterNextBeep = MakeBeep(lever);
                if (next.Next != null) {
                    next.Next.SetChoice(afterNextBeep);
                } else {
                    // this doesn't actually queue anything
                    next.Next = afterNextBeep;
                    next.Next.SampleStarted += Next_SampleStarted;
                }

                Playing.Add(beep.Next);
                Playing.Add(afterNextBeep);
                return;
            }

            var sound = MakeBeep(lever);
            ScoreProvider.AddMixerInput(sound);
            Playing.Add(sound);
            if (sound.Next != null) { // (should never be null)
                sound.Next.SampleStarted += Next_SampleStarted; // to clear Playing list
                Playing.Add(sound.Next);
                if (lever == LeverKind.Dits || lever == LeverKind.Dahs) {
                    //TODO: check if dit or dah next
                    // queue next sound
                    var nextBeep = MakeBeep(lever);
                    sound.Next.SetChoice(nextBeep);
                }
            }

        }


        [Obsolete]
        public void StraightKeyDown(int meh = 0) {
            //PlayBeepNext(1);
        }
        [Obsolete]
        public void StraightKeyUp() {
            //PlayDitNext();
        }


        private void Next_SampleStarted(SwitchableDelayedSound sender) {
            sender.SampleStarted -= Next_SampleStarted;
            
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

            if (Playing.Count == 0 && (sender.Lever == LeverKind.Dits || sender.Lever == LeverKind.Dahs)) {
                // queue up next
                //if (sender.Next != null) // todo
                //var nextSound = MakeBeep(sender.Lever, sender);
                //Playing.Add(nextSound);
                //nextSound.SampleStarted += Next_SampleStarted;
                //ScoreProvider.AddMixerInput(nextSound);
            }
        }

        private SwitchableDelayedSound MakeBeep(LeverKind lever, SwitchableDelayedSound afterThis = null) {
        
            //double ditLen, double whenInDits = 0;
            double? ditLen = null;
            if (lever == LeverKind.Dits) ditLen = 1;
            if (lever == LeverKind.Dahs) ditLen = 3;

            long ditLenSamples = (long)(ditSeconds * WaveFormat.SampleRate);
            long? beepSamples = ditLen.HasValue ? (long)(ditSeconds * ditLen * WaveFormat.SampleRate) : null;

            //long whenSamples = (long)(ditSeconds * whenInDits * AudioOut.Format.SampleRate);
            //long currentPos = ScoreProvider.CurrentSamplePos; // used here for calculating waveform phase; not for positioning the note
            //long currentPos = (!Options.HasFlag(BeepAttackDecay.InfiniteWave)) ? ScoreProvider.CurrentSamplePos : 0;
            long currentPos = 0; //  0 + whenSamples; // todo: replace 0 with currentPos depending on settings

            var beep = Beep.MakeBeep(currentPos, beepSamples, johncage: ditLenSamples, options: Options);

            var next = new SwitchableDelayedSound(WaveFormat);
            next.StartAt = ditLenSamples;
            var beepWithNext = beep.FollowedBy(next);

            var sound = new SwitchableDelayedSound(beepWithNext);
            sound.Lever = lever;
            sound.Next = next;
            if (beepSamples != null) sound.DurationSamples = beepSamples.Value + ditLenSamples;

            //sound.StartAt = whenSamples;
            //sound.StartAt = // TODO: delay

            //todo
            //StraightAdsr = new AdsrSampleProvider(faded.ToMono(1, 0)) {
            //AttackSeconds = AttackSeconds,
            //ReleaseSeconds = ReleaseSeconds // does not get used unless stopping early

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

