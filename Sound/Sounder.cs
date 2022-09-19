using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MorseKeyer.Sound
{
    public class Sounder {

        private int Latency = 50;
        IWavePlayer waveOutDevice;
        SignalGenerator Sine;

        //WaveStream mainOutputStream;



        public Sounder(int latency = 50) {
            Latency = latency;

            Sine = new SignalGenerator() {
                Gain = 0.4,
                Frequency = 550,
                Type = SignalGeneratorType.Sin
            };
        }

        public void Enable() {
            // start running.
            try {
                waveOutDevice = new DirectSoundOut(Latency);
                
                
            } catch (Exception driverCreateException) {
                Console.WriteLine(String.Format("{0}", driverCreateException.Message));
                return;
            }

            //TODO: sounds
            /*
            mainOutputStream = CreateInputStream(fileName);
            try {
                waveOutDevice.Init(mainOutputStream);
            } catch (Exception initException) {
                Console.WriteLine(String.Format("{0}", initException.Message), "Error Initializing Output");
                return;
            }
            */

        }

        public void DitKeyDown() {
            //mainOutput
        }

        public void StraightKeyDown() {
            // https://github.com/naudio/NAudio/blob/master/Docs/PlaySineWave.md
            // https://csharp.hotexamples.com/examples/NAudio.Wave/DirectSoundOut/Play/php-directsoundout-play-method-examples.html


            //Sine.Take(TimeSpan.FromSeconds(20));
            //using (var wo = new WaveOutEvent()) {
            //    wo.Init(sine.Take(TimeSpan.FromSeconds(1)));
            //    wo.Play();
            //    while (wo.PlaybackState == PlaybackState.Playing) {
            //        Thread.Sleep(500);
            //    }
            //}

            waveOutDevice.Init(Sine);
            //waveOutDevice.Init(Sine.Take(TimeSpan.FromSeconds(1)));
            waveOutDevice.Play();
            Console.WriteLine("playing...");

        }

        public void StraightKeyUp() {
            if (waveOutDevice?.PlaybackState == PlaybackState.Playing) {
                waveOutDevice.Pause();
            }
        }



    }
}
