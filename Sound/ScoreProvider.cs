using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;
using NAudio.Utils;
using NAudio.Wave.SampleProviders;
using upSidetone.Sound;

// Loosely based on MixingSampleProvider

// in the end, the only practical difference is this one is counting the number of samples listened

namespace upSidetone.Sound {
    public class ScoreProvider : ISampleProvider {

        private const int MaxInputs = 1024;

        private readonly List<SwitchableDelayedSound> sources;
        //private readonly Dictionary<long, SwitchableSound> queued; // start time (meh, let each sound deal with it themselves)
        private float[] sourceBuffer;
        private long nSamples = 0;
        public long CurrentSamplePos => nSamples;

        public IEnumerable<ISampleProvider> MixerInputs => sources;

        public bool ReadFully { get; set; }

        public WaveFormat WaveFormat { get; private set; }

        public event EventHandler<SampleProviderEventArgs> MixerInputEnded;
        //public event EventHandler<SampleProviderEventArgs> MixerInputStarted; // TODO

        public ScoreProvider(WaveFormat waveFormat) {
            if (waveFormat.Encoding != WaveFormatEncoding.IeeeFloat) {
                throw new ArgumentException("Mixer wave format must be IEEE float"); // Y tho
            }

            sources = new();
            WaveFormat = waveFormat;
        }

        //public void AddMixerInput(IWaveProvider mixerInput, long when) {
        //    //AddMixerInput(SampleProviderConverters.ConvertWaveProviderIntoSampleProvider(mixerInput));
        //    throw new NotImplementedException("not implemented: SampleProviderConverters not accessible");
        //}

        public void AddMixerInput(SwitchableDelayedSound mixerInput) {
            lock (sources) {
                if (sources.Count >= MaxInputs) {
                    throw new InvalidOperationException("Too many mixer inputs");
                }

                sources.Add(mixerInput);
            }

            if (WaveFormat == null) {
                WaveFormat = mixerInput.WaveFormat;
            } else if (WaveFormat.SampleRate != mixerInput.WaveFormat.SampleRate || WaveFormat.Channels != mixerInput.WaveFormat.Channels) {
                throw new ArgumentException("All mixer inputs must have the same WaveFormat");
            }


        }

        public void RemoveMixerInput(SwitchableDelayedSound mixerInput) {
            lock (sources) {
                sources.Remove(mixerInput);
            }
        }

        public void RemoveAllMixerInputs() {
            lock (sources) {
                sources.Clear();
            }
        }

        public int Read(float[] buffer, int offset, int count) {
            int num = 0;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, count);
            lock (sources) {
                Array.Clear(buffer, offset, count);
                for (int num2 = sources.Count - 1; num2 >= 0; num2--) {
                    ISampleProvider sampleProvider = sources[num2];
                    int num3 = sampleProvider.Read(sourceBuffer, 0, count);
                    int num4 = offset;

                    if (num3 == -1) {
                        // hasn't started yet (hack)
                        continue;
                    }

                    for (int i = 0; i < num3; i++) {
                        if (i >= num) {
                            buffer[num4++] = sourceBuffer[i];
                        } else {
                            buffer[num4++] += sourceBuffer[i];
                        }
                    }

                    num = Math.Max(num3, num);
                    if (num3 < count) {
                        this.MixerInputEnded?.Invoke(this, new SampleProviderEventArgs(sampleProvider));
                        sources.RemoveAt(num2);
                    }
                }
            }

            if (ReadFully && num < count) {
                int num5 = offset + num;
                while (num5 < offset + count) {
                    buffer[num5++] = 0f;
                }
                num = count;
            }

            nSamples += num;
            return num;
        }
    }
}