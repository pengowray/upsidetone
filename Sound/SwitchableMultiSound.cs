using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using NAudio.Wave;

namespace upSidetone.Sound {
    public class SwitchableMultiSound<K> : ISampleProvider {

        bool LockedIn;
        K? Pick;
        Dictionary<K, ISampleProvider> Choices;
        ISampleProvider? Chosen;

        public WaveFormat WaveFormat { get; set; }

        // allows changing which ISampleProvider to use until first sample is Read()

        // needlessly complex. use SwitchableDelayedSound instead

        int Read(float[] buffer, int offset, int count) {
            if (!LockedIn) {
                LockedIn = true;

                if (Choices != null) {
                    if (Choices == null || Pick == null || Choices[Pick] == null) {
                        Chosen = null;
                    } else {
                        Chosen = Choices[Pick];
                    }
                }

                // clear irrelevant choices (maybe not needed)
                Choices = null;

            }

            return Chosen?.Read(buffer, offset, count) ?? 0;
        }

        int ISampleProvider.Read(float[] buffer, int offset, int count) {
            throw new NotImplementedException();
        }


        public bool SetChoice(ISampleProvider Choice, K pos) {
            // returns true on success; false if you were too late
            if (LockedIn) {
                return false; // too late
            }

            Choices[pos] = Choice;
            Pick = pos;
            return true;
        }

        public bool SetChoice(K pos) {
            // returns true on success; false if you were too late
            if (LockedIn) {
                return false; // too late
            }

            Pick = pos;
            return true;
        }

    }
}