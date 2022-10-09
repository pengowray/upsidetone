using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// Loosely based on: NAudio.Wave.SampleProviders.SignalGenerator (MIT License)

namespace upSidetone.Sound {
    public class SineGenerator : ISampleProvider {

        private readonly WaveFormat waveFormat;

        private long nSample;
        private long? EndingAfterSample;
        //private long? SilenceUntilSample; // TODO

        private bool? CrossZeroEndMode; // ending if not null; true = starting at positive, end when cross zero to negative. false = currently negative
        private float LastRead;
        private bool End;

        public WaveFormat WaveFormat => waveFormat;

        public double Frequency { get; set; }

        public double StartPhase { get; set; } // 0 to 2

        public double FrequencyLog => Math.Log(Frequency);

        public double Gain { get; set; }

        public bool[] PhaseReverse { get; }


        public SignalGeneratorType Type { get => SignalGeneratorType.Sin; }


        public SineGenerator() : this(44100, 2) {
        }

        public SineGenerator(int sampleRate, int channel, long beginAtSample = 0, long? sampleLength = null, double startPhase = 0) {
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channel);
            Frequency = 440.0;
            StartPhase = startPhase;
            nSample = beginAtSample;
            if (sampleLength.HasValue) {
                EndingAfterSample = beginAtSample + sampleLength.Value;
            }

            Gain = 1.0;
            PhaseReverse = new bool[channel];
        }

        public int Read(float[] buffer, int offset, int count) {
            int num = offset;
            double num4 = Math.PI * 2.0 * Frequency / (double)waveFormat.SampleRate;
            int phaseSamples = (StartPhase == 0) ? 0 : (int)(Frequency * StartPhase * Math.PI * 2.0); // phase 0 to 2

            for (int i = 0; i < count / waveFormat.Channels; i++) {

                if (End) {
                    LastRead = 0f;
                    return i; // end

                } else {
                    double num2 = Gain * Math.Sin(((double)nSample + phaseSamples) * num4);
                    LastRead = (float)num2;
                    nSample++;

                    if (CrossZeroEndMode.HasValue) {
                        if (CrossZeroEndMode.Value && LastRead <= 0) {
                            LastRead = 0f;
                            End = true;
                        } else if (CrossZeroEndMode.Value && LastRead >= 0) {
                            LastRead = 0f;
                            End = true;
                        }
                    } else if (EndingAfterSample.HasValue && nSample >= EndingAfterSample) {
                        EndAfterZeroCross();
                    }

                }

                for (int j = 0; j < waveFormat.Channels; j++) {
                    if (PhaseReverse[j]) {
                        buffer[num++] = (0.0f - LastRead); ;
                    } else {
                        buffer[num++] = LastRead;
                    }
                }
            }

            return count;
        }

        public void EndAfterZeroCross() {
            if (LastRead > 0) {
                CrossZeroEndMode = true;
            } else if (LastRead < 0) {
                CrossZeroEndMode = false;
            } else { 
                // already zero, or in bad state (NaN/Inf)
                End = true;
            }
        }

        public void EndIn(TimeSpan time) {
            EndInSamples((long)(time.TotalSeconds * WaveFormat.SampleRate));
        }

        public void EndInSamples(long samples) {
            EndingAfterSample = samples + nSample;
        }

        public void EndAt(TimeSpan time) {
            EndAtSamples((long)(time.TotalSeconds * WaveFormat.SampleRate));
        }

        public void EndAtSamples(long samples) {
            EndingAfterSample = samples;
        }


        public void Stop() {
            End = true;
        }

        public void Reset() {
            // TODO: don't reset while reading
            nSample = 0;
            End = false;
            CrossZeroEndMode = null;
            EndingAfterSample = null;
        }

    }
}
