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
//using NWaves.Audio;

namespace upSidetone.Sound {

    //rewrite of ToneMaker_Old (previously called "Sounder")

    public class ToneMaker : IDisposable {

        //TODO: per lever
        const double wpm = 12.0;
        double ditSeconds = 60.0 / (50.0 * wpm);

        SwitchableDelayedSound Playing;
        SwitchableDelayedSound PlayNext;
        SwitchableDelayedSound AndThen;
        AudioOut AudioOut;
        //ScoreProvider ScoreProvider;
        MixingSampleProvider ScoreProvider;
        Beep Beep;
        private bool disposedValue;

        public WaveFormat Format => AudioOut?.Format;

        public ToneMaker(AudioOut audioOut) {
            AudioOut = audioOut;
            //ScoreProvider = new ScoreProvider(audioOut.Format);
            ScoreProvider = new MixingSampleProvider(audioOut.Format);
            ScoreProvider.ReadFully = true;
            Beep = new Beep(audioOut.Format);
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
            
        }

        private void Levers_LeverDown(Levers levers, LeverKind lever) {
            if (lever == LeverKind.Dits) {
                PlayDitNext();
            }
        }

        public void StraightKeyDown(int meh = 0) {
            PlayDitNext();
        }
        public void StraightKeyUp() {
            //PlayDitNext();
        }

        public void PlayDitNext() {
            long ditLenSamples = (long)(ditSeconds * AudioOut.Format.SampleRate);
            //long currentPos = ScoreProvider.CurrentSamplePos; // used here for calculating waveform phase; not for positioning the note
            long currentPos = 0; // todo
            var beep = Beep.MakeBeep(currentPos, ditLenSamples, johncage: ditLenSamples, options: BeepAttackDecay.Preset_Smooth);
            var sound = new SwitchableDelayedSound(beep);
            //sound.StartAt = // TODO: delay
            Playing = sound;
            ScoreProvider.AddMixerInput(sound); // TODO

            //test: just beep (works)
            //AudioOut?.Mixer?.AddMixerInput(beep);

            // allow arbitrary fading out
            //StraightAdsr = new AdsrSampleProvider(faded.ToMono(1, 0)) {
            //AttackSeconds = AttackSeconds,
            //ReleaseSeconds = ReleaseSeconds // does not get used unless stopping early
        }

        internal void DitKeyDown() {
            //throw new NotImplementedException();
        }

        internal void DitsKeyUp() {
            //throw new NotImplementedException();
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

