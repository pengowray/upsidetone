using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using NAudio.Wave;

namespace upSidetone.Sound {
    public class SwitchableSound : ISampleProvider {

        // allows changing which ISampleProvider to use up until first sample is Read()

        bool LockedIn;
        ISampleProvider? Chosen;

        public WaveFormat WaveFormat { get; private set; }

        public SwitchableSound(WaveFormat waveFormat) {
            WaveFormat = waveFormat;
        }

        public SwitchableSound(ISampleProvider initialChoice) {
            if (initialChoice == null) {
                throw new ArgumentNullException(nameof(initialChoice));
            }
            Chosen = initialChoice;
            WaveFormat = initialChoice.WaveFormat;
        }

        int ISampleProvider.Read(float[] buffer, int offset, int count) {
            if (!LockedIn) {
                LockedIn = true;
            }

            return Chosen?.Read(buffer, offset, count) ?? 0;
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