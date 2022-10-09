using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using NAudio.Wave;
using NWaves.Filters.Base;

namespace upSidetone.Sound {
    public class SwitchableDelayedSound : ISampleProvider {

        // allows changing which ISampleProvider to use up until first sample is Read()

        // Can init with current Pos and when to Start sample

        // Breaks ISampleProvider:
        //  - Read() returns -1 when empty signal
        //  - Assumes single channel (mono)

        public long StartAt { set; get; }
        public long nSamples { set; get; }

        bool LockedIn;
        ISampleProvider? Chosen;

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
            if (!LockedIn) {
                if (StartAt > nSamples + count) {
                    nSamples += count;
                    return -1; // special "no change" signal i made up; optimization 

                } else {
                    //int readOffset = (int)(Pos + offset - Start);
                    //int toRead = (int)(Pos + count - Start);

                    int i = 0;
                    for (long pos = nSamples; pos < nSamples + count; pos++) {
                        if (pos == StartAt) {
                            LockedIn = true;
                            var read1 = Chosen?.Read(buffer, i, count - i) ?? 0;
                            nSamples += read1;  // no longer need to track Pos
                            return i + read1;
                        } else {
                            buffer[offset + i] = 0;
                            i++;
                        }
                    }

                    //var read = Chosen?.Read(buffer, readOffset, count - toRead) ?? 0;
                    //return read + 

                    // should have found Start by now
                    nSamples += i; 
                    return i;
                }
            }

            var read = Chosen?.Read(buffer, offset, count) ?? 0;
            nSamples += read; // no longer need to track Pos
            return read;
        }

        public bool SetChoice(ISampleProvider choice) {
            // returns true on success; false if you were too late
            if (LockedIn) {
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