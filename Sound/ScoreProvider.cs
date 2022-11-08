using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using NAudio.Utils;
using NAudio.Wave;


// rewrite of MixingSampleProvider 
// https://github.com/naudio/NAudio/blob/master/NAudio.Core/Wave/SampleProviders/MixingSampleProvider.cs
// https://github.com/naudio/NAudio/blob/master/license.txt (Mit License: Copyright 2020 Mark Heath)

// differences:
// - keeps track of current sample (SampleCursor)
// - defaults to ReadFully = true

namespace upSidetone.Sound {
    /// <summary>
    /// A sample provider mixer, allowing inputs to be added and removed
    /// </summary>
    public class ScoreProvider : ISampleProvider {
        public long SampleCursor = 0; // how many samples have been read ever

        private readonly List<SwitchableDelayedSound> sources; // was: ISampleProvider
        //private readonly List<SwitchableDelayedSound> sounds; // sources of type SwitchableDelayedSound
        private float[] sourceBuffer;
        private const int MaxInputs = 1024; // protect ourselves against doing something silly

        public string DebugText() {
            var sounds = sources;
            //var sounds = sources.Select(s => s as SwitchableDelayedSound);
            string playing = String.Join(" ", sounds.Select(p => p == null ? "null" : $"{p.Lever}{(p.IsLockedIn ? "🔒" : "")}"));
            return playing;
        }

        /// <summary>
        /// Creates a new MixingSampleProvider, with no inputs, but a specified WaveFormat
        /// </summary>
        /// <param name="waveFormat">The WaveFormat of this mixer. All inputs must be in this format</param>
        public ScoreProvider(WaveFormat waveFormat) {
            if (waveFormat.Encoding != WaveFormatEncoding.IeeeFloat) {
                throw new ArgumentException("Mixer wave format must be IEEE float");
            }
            sources = new List<SwitchableDelayedSound>();
            WaveFormat = waveFormat;
            ReadFully = true;
        }

        /// <summary>
        /// Creates a new MixingSampleProvider, based on the given inputs
        /// </summary>
        /// <param name="sources">Mixer inputs - must all have the same waveformat, and must
        /// all be of the same WaveFormat. There must be at least one input</param>
        public ScoreProvider(IEnumerable<SwitchableDelayedSound> sources) {
            this.sources = new List<SwitchableDelayedSound>();
            foreach (var source in sources) {
                AddMixerInput(source);
            }
            if (this.sources.Count == 0) {
                throw new ArgumentException("Must provide at least one input in this constructor");
            }
            ReadFully = true;
        }

        /// <summary>
        /// Returns the mixer inputs (read-only - use AddMixerInput to add an input
        /// </summary>
        public IEnumerable<ISampleProvider> MixerInputs => sources;

        /// <summary>
        /// When set to true, the Read method always returns the number
        /// of samples requested, even if there are no inputs, or if the
        /// current inputs reach their end. Setting this to true effectively
        /// makes this a never-ending sample provider, so take care if you plan
        /// to write it out to a file.
        /// </summary>
        public bool ReadFully { get; set; }

        /// <summary>
        /// Adds a WaveProvider as a Mixer input.
        /// Must be PCM or IEEE float already
        /// </summary>
        /// <param name="mixerInput">IWaveProvider mixer input</param>
        public void AddMixerInput(IWaveProvider mixerInput) {
            //https://github.com/naudio/NAudio/blob/master/NAudio.Core/Wave/SampleProviders/SampleProviderConverters.cs
            //AddMixerInput(SampleProviderConverters.ConvertWaveProviderIntoSampleProvider(mixerInput));
            throw new NotImplementedException("not implemented: SampleProviderConverters not accessible");
        }

        /// <summary>
        /// Adds a new mixer input
        /// </summary>
        /// <param name="mixerInput">Mixer input</param>
        public void AddMixerInput(SwitchableDelayedSound mixerInput) {
            // we'll just call the lock around add since we are protecting against an AddMixerInput at
            // the same time as a Read, rather than two AddMixerInput calls at the same time
            lock (sources) {
                if (sources.Count >= MaxInputs) {
                    throw new InvalidOperationException("Too many mixer inputs. " + DebugText() );
                }
                sources.Add(mixerInput);
            }
            if (WaveFormat == null) {
                WaveFormat = mixerInput.WaveFormat;
            } else {
                if (WaveFormat.SampleRate != mixerInput.WaveFormat.SampleRate ||
                    WaveFormat.Channels != mixerInput.WaveFormat.Channels) {
                    throw new ArgumentException("All mixer inputs must have the same WaveFormat");
                }
            }
        }

        /// <summary>
        /// Raised when a mixer input has been removed because it has ended
        /// </summary>
        public event EventHandler<SampleProviderEventArgs> MixerInputEnded;

        /// <summary>
        /// Removes a mixer input
        /// </summary>
        /// <param name="mixerInput">Mixer input to remove</param>
        public void RemoveMixerInput(SwitchableDelayedSound mixerInput) {
            lock (sources) {
                sources.Remove(mixerInput);
            }
        }

        /// <summary>
        /// Removes all mixer inputs
        /// </summary>
        public void RemoveAllMixerInputs() {
            lock (sources) {
                sources.Clear();
            }
        }

        /// <summary>
        /// The output WaveFormat of this sample provider
        /// </summary>
        public WaveFormat WaveFormat { get; private set; }

        /// <summary>
        /// Reads samples from this sample provider
        /// </summary>
        /// <param name="buffer">Sample buffer</param>
        /// <param name="offset">Offset into sample buffer</param>
        /// <param name="count">Number of samples required</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int count) {
            long cursor = SampleCursor;
            int outputSamples = 0;
            if (ReadFully) {
                // set early for anyone adding new samples at SampleCursor 
                SampleCursor += count;
            }

            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, count);
            lock (sources) {
                int index = sources.Count - 1;
                while (index >= 0) {
                    var source = sources[index];
                    int samplesRead;
                    //if (source is SwitchableDelayedSound sSource) {
                    //    samplesRead = sSource.Read(sourceBuffer, 0, count, cursor);
                    //} else {
                    //    samplesRead = source.Read(sourceBuffer, 0, count);
                    //}
                    samplesRead = source.Read(sourceBuffer, 0, count, cursor);
                    int outIndex = offset;
                    for (int n = 0; n < samplesRead; n++) {
                        if (n >= outputSamples) {
                            buffer[outIndex++] = sourceBuffer[n];
                        } else {
                            buffer[outIndex++] += sourceBuffer[n];
                        }
                    }
                    outputSamples = Math.Max(samplesRead, outputSamples);
                    if (samplesRead < count) {
                        MixerInputEnded?.Invoke(this, new SampleProviderEventArgs(source));
                        sources.RemoveAt(index);
                    }
                    index--;
                }
            }
            // optionally ensure we return a full buffer
            if (ReadFully && outputSamples < count) {
                int outputIndex = offset + outputSamples;
                while (outputIndex < offset + count) {
                    buffer[outputIndex++] = 0;
                }
                outputSamples = count;
            }
            if (!ReadFully) { // because already done if ReadFully
                SampleCursor += outputSamples;
            }
            return outputSamples;
        }
    }

    /// <summary>
    /// SampleProvider event args
    /// </summary>
    public class SampleProviderEventArgs : EventArgs {
        /// <summary>
        /// Constructs a new SampleProviderEventArgs
        /// </summary>
        public SampleProviderEventArgs(ISampleProvider sampleProvider) {
            SampleProvider = sampleProvider;
        }

        /// <summary>
        /// The Sample Provider
        /// </summary>
        public ISampleProvider SampleProvider { get; private set; }
    }
}
