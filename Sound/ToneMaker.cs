using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using System.Windows.Media.Animation;
using NAudio.Mixer;
using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
//using NWaves.Audio;

namespace upSidetone.Sound
{

    // replaced with ToneMaker2

    // delete me

    public class ToneMaker_Old : IDisposable {

        List<LeverKind> Down = new(); // keys pressed and in what order

        float AttackSeconds = 0.015f;
        float ReleaseSeconds = 0.015f;
        const double wpm = 12.0;
        double ditSeconds = 60.0 / (50.0 * wpm);

        KeyerMode KeyerMode = KeyerMode.Isopoda;

        AudioOut AudioOut;
        public WaveFormat? Format => AudioOut?.Format;

        SignalGenerator? Sine;
        SignalGenerator? Sine2; // for testing
        SignalGenerator? Sine3; // for testing

        float Volume = 0.2f;
        //List<AdsrSampleProvider> AdsrList = new();
        AdsrSampleProvider? DitAdsr;
        AdsrSampleProvider? StraightAdsr;
        MixingSampleProvider? Mixer => AudioOut?.Mixer;

        private bool disposedValue;

        public ToneMaker_Old(AudioOut audioOut) {
            AudioOut = audioOut;
        }

        public void Enable() {
            // start running.
            try {

                Sine = new SignalGenerator(Format.SampleRate, channel: 1) {
                    Gain = Volume,
                    Frequency = 550, // 138.5 * 3,  //650,  A major chord A, C#, E
                    Type = SignalGeneratorType.Sin
                };
                Sine2 = new SignalGenerator(Format.SampleRate, channel: 1) {
                    Gain = Volume,
                    Frequency = 550, // 164.5 * 3,
                    Type = SignalGeneratorType.Sin
                };
                Sine3 = new SignalGenerator(Format.SampleRate, channel: 1) {
                    Gain = Volume,
                    Frequency = 550, // 220.5 * 3,
                    Type = SignalGeneratorType.Sin
                };

                //MainWindow.Debug($"2. Format sampleRate/channels (Device: {OutDevice?.OutputWaveFormat?.ToString()} | {OutDevice?.OutputWaveFormat?.SampleRate}/{OutDevice?.OutputWaveFormat?.Channels}): {Format} => {Format.SampleRate}/{Format.Channels}");

            } catch (Exception e) {

                string err = $"sounder enable exception ({e?.GetType()}): " + e?.Message?.ToString();
            }

        }

        private void AddDownLever(LeverKind lever) {
            lock (Down) {
                if (Down.Contains(lever)) {
                    Down.Remove(lever); // make sure it's on the end
                }
                Down.Add(lever);
            }
        }

        private void RemoveDownLever(LeverKind lever) {
            lock (Down) {
                if (Down.Contains(lever)) {
                    Down.Remove(lever);
                }
            }
        }


        public void DoDitKeyDown(bool force = false) {
            //FadeInOutSampleProvider
            //ConcatenatingSampleProvider
            //EnvelopeGenerator

            if (!force && Down.LastOrDefault() == LeverKind.Dit) {
                // already down
                return;
            }

            AddDownLever(LeverKind.Dit);

            if (true) {

                if (StraightAdsr != null) {
                    StraightAdsr.Stop();
                    StraightAdsr = null;
                    //TODO: add silence before dit if straight key was going?
                }

                if (Sine == null) {
                    return; // maybe shutting down
                }
                //todo: dont allow negative time
                var wave = Sine.Take(TimeSpan.FromSeconds(ditSeconds));
                
                var faded = new FadeOutSampleProvider(wave);
                faded.SetFadeInSeconds(AttackSeconds * 1000);
                //faded.SetFadeOut((ditSeconds - ReleaseSeconds) * 1000.0, ReleaseSeconds * 1000.0);
                faded.FadeEnding(TimeSpan.FromSeconds(ReleaseSeconds), TimeSpan.FromSeconds(ditSeconds));
                //TODO: append dit silence
                var sil = new SilenceProvider(Format);

                //var all = new ConcatenatingSampleProvider(new[] { wave, tail });

                StraightAdsr = new AdsrSampleProvider(faded.ToMono(1, 0)) {
                    //AttackSeconds = AttackSeconds,
                    //ReleaseSeconds = ReleaseSeconds // does not get used unless stopping early
                };

                //Adsr.FollowedBy(Adsr);
                    
                if (Mixer != null) {
                    if (Mixer.WaveFormat.Channels == 2) {
                        Mixer.AddMixerInput(StraightAdsr.ToStereo(1, 1));
                    } else {
                        Mixer.AddMixerInput(StraightAdsr);
                    }
                    //Mixer.MixerInputEnded += Mixer_MixerInputEnded;
                }

                var timer = new System.Threading.Timer(DitDoneCheck, null, (int)(ditSeconds * 2000), Timeout.Infinite);
            }
        }

        private void DitDoneCheck(Object? info) {
            StraightAdsr = null;
            var last = Down.LastOrDefault();
            if (last == LeverKind.Dit) {
                // dit get still down
                DoDitKeyDown(force: true);
            } else if (last == LeverKind.Straight){
                // dit key was released and straight key is waiting
                StraightKeyDown(1, force: true); 
            }
        }

        public void StraightKeyDown(int note = 1, bool force = false) {
            // https://github.com/naudio/NAudio/blob/master/Docs/PlaySineWave.md
            // https://csharp.hotexamples.com/examples/NAudio.Wave/DirectSoundOut/Play/php-directsoundout-play-method-examples.html

            var previousLast = Down.LastOrDefault();
            AddDownLever(LeverKind.Straight);

            if (previousLast == LeverKind.Dit) {
                return; // wait until dit finished

            } else if (previousLast == LeverKind.Straight) {
                // nothing to do 
                if (!force) return;
            }

            //https://stackoverflow.com/a/23357560/443019

            var wave = Sine;
            if (note == 2) wave = Sine2;
            if (note == 3) wave = Sine3;

            if (StraightAdsr == null && Sine != null) {
                //StraightAdsr?.Stop(); // maybe redundant to stop it to start it again
                StraightAdsr = new AdsrSampleProvider(wave.ToMono(1, 0)) {
                    AttackSeconds = 0.015f,
                    ReleaseSeconds = 0.015f
                };
            }

            if (Mixer != null) {
                if (Mixer.WaveFormat.Channels == 2) {
                    Mixer?.AddMixerInput(StraightAdsr.ToStereo(1, 1));
                } else {
                    Mixer?.AddMixerInput(StraightAdsr);
                }

            } else {
                MainWindow.DebugOut("Mixer null");
            }

            
        }

        public void StraightKeyUp() {
            RemoveDownLever(LeverKind.Straight);
            StraightAdsr?.Stop();
            StraightAdsr = null;
        }

        public void DitsKeyUp() {
            RemoveDownLever(LeverKind.Dit);
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)

                    StraightAdsr?.Stop();
                    //Mixer?.RemoveAllMixerInputs(); // probably not needed here

                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer

                // TODO: set large fields to null
                AudioOut = null;
                //Mixer = null;
                StraightAdsr = null;
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
