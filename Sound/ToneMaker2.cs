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
using System.Windows.Navigation;
using Voicemeeter;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
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

        public WaveFormat WaveFormat => AudioOut?.Format;

        public ToneMaker(AudioOut audioOut) {
            AudioOut = audioOut;
            ScoreProvider = new ScoreProvider(WaveFormat);
            //ScoreProvider = new MixingSampleProvider(WaveFormat);
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

            var toUp = Playing.Where(sound => !sound.IsLockedIn && sound.Lever == lever);
            GetNextUpAndDeleteRest(true);

        }


        private void Levers_LeverDown(Levers levers, LeverKind lever) {
            ThreeBeeps(levers, lever);

        }

        private SwitchableDelayedSound? GetNextUpAndDeleteRest(bool justdel = false) { 
            var next = Playing.FirstOrDefault(p => !p.IsLockedIn);
            if (next == null) {
                return null;
            }

            List<SwitchableDelayedSound> newPlaying = new();
            foreach (var sound in Playing) {
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

        [Obsolete]
        public void StraightKeyDown(int meh = 0) {
            //PlayBeepNext(1);
        }
        [Obsolete]
        public void StraightKeyUp() {
            //PlayDitNext();
        }
        private void Next_SampleStarted(SwitchableDelayedSound sender) {

            _ = Task.Run(() => {
                sender.SampleStarted -= Next_SampleStarted;
                bool inPlaying = Playing.Contains(sender);
                if (inPlaying) {
                    ThreeBeeps(null, LeverKind.Dits);
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

        private SwitchableDelayedSound MakeBeep(LeverKind lever, SwitchableDelayedSound? afterThis = null) {
        
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

            var sound = new SwitchableDelayedSound(beep);
            //sound.Lever = lever;
            //sound.Next = next;

            if (beepSamples != null) sound.DurationSamples = beepSamples.Value + ditLenSamples;

            if (afterThis != null) {
                sound.StartAt = afterThis.StartAt + afterThis.DurationSamples;
                if (afterThis.Next == null) {
                    afterThis.Next = sound;
                } else {
                    //TODO: remove event
                    afterThis.Next.SetChoice(sound);
                }
            } else {
                //TODO: set to null and let it get autofilled... but then harder to queue after it
                sound.StartAt = ScoreProvider.SampleCursor;
            }

            //sound.StartAt = whenSamples;
            //sound.StartAt = // TODO: delay

            //todo
            //StraightAdsr = new AdsrSampleProvider(faded.ToMono(1, 0)) {
            //AttackSeconds = AttackSeconds,
            //ReleaseSeconds = ReleaseSeconds // does not get used unless stopping early

            Playing.Add(sound);
            ScoreProvider.AddMixerInput(sound);
            sound.SampleStarted += Next_SampleStarted; // to clear Playing list

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

