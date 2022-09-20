using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Mixer;

namespace MorseKeyer.Sound
{
    public class Sounder {

        private int Latency = 30;
        IWavePlayer? waveOutDevice;

        SignalGenerator Sine;
        //List<AdsrSampleProvider> AdsrList = new();
        AdsrSampleProvider? Adsr;

        MixingSampleProvider Mixer;
        //WaveStream mainOutputStream;



        public Sounder(int latency = 50) {
            Latency = latency;

        }

        public void Enable() {
            // start running.
            try {

                Sine = new SignalGenerator() {
                    Gain = 0.4,
                    Frequency = 550,
                    Type = SignalGeneratorType.Sin, 
                };

                waveOutDevice = new DirectSoundOut(Latency);

                var Format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate: 44100, channels: 1);
                //Mixer = new MixingSampleProvider(44100, 2);
                Mixer = new MixingSampleProvider(Format);
                Mixer.ReadFully = true;
                waveOutDevice?.Init(Mixer);
                waveOutDevice?.Play();

            } catch (Exception driverCreateException) {
                Console.WriteLine(String.Format("{0}", driverCreateException.Message));
                return;
            }




        }

        public void DitKeyDown() {
            //mainOutput
        }

        public void StraightKeyDown() {
            // https://github.com/naudio/NAudio/blob/master/Docs/PlaySineWave.md
            // https://csharp.hotexamples.com/examples/NAudio.Wave/DirectSoundOut/Play/php-directsoundout-play-method-examples.html


            if (Adsr == null) {

                //https://stackoverflow.com/a/23357560/443019

                Adsr = new AdsrSampleProvider(Sine.ToMono()) {
                    AttackSeconds = 0.015f,
                    ReleaseSeconds = 0.015f
                };

                Mixer.AddMixerInput(Adsr);
            }

            
        }

        public void StraightKeyUp() {
            Adsr?.Stop();
            Adsr = null;
        }



    }
}
