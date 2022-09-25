using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Mixer;
using NAudio.CoreAudioApi;
using System.Windows.Forms;
using System.Management;
//using NWaves.Audio;

namespace UpSidetone.Sound
{
    public class Sounder : IDisposable {

        AudioOut AudioOut;
        public WaveFormat? Format => AudioOut?.Format;

        SignalGenerator? Sine;
        SignalGenerator? Sine2;
        SignalGenerator? Sine3;

        float Volume = 0.2f;
        //List<AdsrSampleProvider> AdsrList = new();
        AdsrSampleProvider? Adsr;
        MixingSampleProvider? Mixer => AudioOut?.Mixer;

        private bool disposedValue;

        public Sounder(AudioOut audioOut) {
            AudioOut = audioOut;
        }

        public void Enable() {
            // start running.
            try {

                Sine = new SignalGenerator(Format.SampleRate, channel: 1) {
                    Gain = Volume,
                    Frequency = 138.5 * 3,  //650,  A major chord A, C#, E
                    Type = SignalGeneratorType.Sin
                };
                Sine2 = new SignalGenerator(Format.SampleRate, channel: 1) {
                    Gain = Volume,
                    Frequency = 164.5 * 3,
                    Type = SignalGeneratorType.Sin
                };
                Sine3 = new SignalGenerator(Format.SampleRate, channel: 1) {
                    Gain = Volume,
                    Frequency = 220.5 * 3,
                    Type = SignalGeneratorType.Sin
                };

                //MainWindow.Debug($"2. Format sampleRate/channels (Device: {OutDevice?.OutputWaveFormat?.ToString()} | {OutDevice?.OutputWaveFormat?.SampleRate}/{OutDevice?.OutputWaveFormat?.Channels}): {Format} => {Format.SampleRate}/{Format.Channels}");

            } catch (Exception e) {

                string err = $"sounder enable exception ({e?.GetType()}): " + e?.Message?.ToString();
            }

        }


        public void DitKeyDown() {
            //mainOutput
        }

        public void StraightKeyDown(int note = 1) {
            // https://github.com/naudio/NAudio/blob/master/Docs/PlaySineWave.md
            // https://csharp.hotexamples.com/examples/NAudio.Wave/DirectSoundOut/Play/php-directsoundout-play-method-examples.html


            if (Adsr == null) {

                //https://stackoverflow.com/a/23357560/443019

                var wave = Sine;
                if (note == 2) wave = Sine2;
                if (note == 3) wave = Sine3;

                if (Sine != null) {
                    Adsr = new AdsrSampleProvider(wave.ToMono(1, 0)) {
                        AttackSeconds = 0.015f,
                        ReleaseSeconds = 0.015f
                    };
                }

                if (Mixer != null) {
                    if (Mixer.WaveFormat.Channels == 2) {
                        Mixer?.AddMixerInput(Adsr.ToStereo(1, 1));
                    } else {
                        Mixer?.AddMixerInput(Adsr);
                    }

                } else {
                    MainWindow.Debug("Mixer null");
                }
                
            }

            
        }

        public void StraightKeyUp() {
            Adsr?.Stop();
            Adsr = null;
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)

                    Adsr?.Stop();
                    //Mixer?.RemoveAllMixerInputs(); // probably not needed here

                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer

                // TODO: set large fields to null
                AudioOut = null;
                //Mixer = null;
                Adsr = null;
                Sine = null;

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
