﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using NAudio.Wave;
using NWaves.Filters.Base;

namespace upSidetone.Sound {

    public delegate void SampleEvent(SwitchableDelayedSound sender);

    public class SwitchableDelayedSound : ISampleProvider {

        // allows changing which ISampleProvider to use up until first sample is Read()

        // Can init with current Pos and when to Start sample

        // Breaks ISampleProvider:
        //  - Assumes single channel (mono)
        //  - [fixed] Read() returns -1 when empty signal -- no longer doing this for now

        // Could be using OffsetSampleProvider for the skip part: https://github.com/naudio/NAudio/blob/master/NAudio.Core/Wave/SampleProviders/OffsetSampleProvider.cs

        public long StartAt { set; get; }
        public long SamplesCursor { private set; get; } // todo: (optionally?) disable and use external cursor (only?)

        public event SampleEvent SampleStarted;



        public long DurationSamples { set; get; } // May include pause after sound; Info not internally used by this class.
        public LeverKind Lever { set; get; } // info not needed specifically by the class, but handy for reference
        public SwitchableDelayedSound Next { get; set; } // not used internally



        public bool IsDone { get; private set; }
        public bool IsLockedIn { get; private set; }

        public ISampleProvider? Chosen { get; private set; }

        public WaveFormat WaveFormat { get; private set; }

        public SwitchableDelayedSound(WaveFormat waveFormat) {
            WaveFormat = waveFormat;
        }

        public SwitchableDelayedSound(ISampleProvider initialChoice) {
            if (initialChoice == null) {
                throw new ArgumentNullException(nameof(initialChoice));
            }
            Chosen = initialChoice;
            WaveFormat = initialChoice.WaveFormat;
        }
        int ISampleProvider.Read(float[] buffer, int offset, int count) {
            return Read(buffer, offset, count, null);
        }
        public int Read(float[] buffer, int offset, int count, long? cursor) {
            if (!IsLockedIn) {
                // use provided cursor if available; otherwise fallback to our own cursor (SamplesCursor)
                long cur = cursor.HasValue ? cursor.Value : SamplesCursor;

                if (StartAt > cur + count) { 

                    SamplesCursor += count;
                    //return -1; // special "no change" signal i made up; optimization  (not sure if it was working so commented out)
                    for (int n = 0; n < count; n++) {
                        buffer[n + offset] = 0;
                    }
                    return count;

                } else {
                    //int readOffset = (int)(Pos + offset - Start);
                    //int toRead = (int)(Pos + count - Start);

                    int i = 0;
                    for (long pos = cur; pos < cur + count; pos++) {
                        if (pos == StartAt) {
                            //todo: could also give a final chance to change
                            SampleStarted?.Invoke(this); // todo: in a thread?

                            IsLockedIn = true;
                            var read1 = Chosen?.Read(buffer, i, count - i) ?? 0;
                            SamplesCursor += read1;  // no longer need to track Pos
                            if (read1 == 0 || read1 < count - i) IsDone = true;
                            return i + read1;
                        } else {
                            buffer[offset + i] = 0;
                            i++;
                        }
                    }

                    //var read = Chosen?.Read(buffer, readOffset, count - toRead) ?? 0;
                    //return read + 

                    // should have found Start by now
                    SamplesCursor += i;
                    return i;
                }
            }

            var read = Chosen?.Read(buffer, offset, count) ?? 0;
            if (read == 0 || read < count) IsDone = true;
            SamplesCursor += read; // no longer need to track Pos
            return read;
        }

        public bool SetChoice(ISampleProvider choice) {
            // returns true on success; false if you were too late
            if (IsLockedIn) {
                return false; // too late
            }

            if (choice != null && (choice.WaveFormat.SampleRate != WaveFormat.SampleRate || choice.WaveFormat.Channels != WaveFormat.Channels)) {
                throw new ArgumentException("WaveFormat mismatched. Can't change it I guess.");
            }
            Chosen = choice;
            return true;
        }
    }
}