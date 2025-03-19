using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace upSidetone.Sound {
    public class Timings {
        // must set SampleRate or WaveFormat
        private int? _SampleRate;
        private WaveFormat? _WaveFormat;
        public WaveFormat? WaveFormat { 
            ///preferably get SampleRate instead
            get { 
                if (_WaveFormat== null) {
                    _WaveFormat = new WaveFormat(SampleRate, 1);
                }
                return _WaveFormat;
            }
        }
        public int SampleRate => _SampleRate ?? _WaveFormat?.SampleRate ?? 44000; // probably shouldn't use a default


        private double Wpm;
        public double DitSeconds { get; private set; }

            

        public Timings(int sampleRate, double wpm = 20) { 
            _SampleRate = sampleRate;
            SetWPM(wpm);
        }

        public Timings(WaveFormat waveFormat, double wpm = 20) {
            _WaveFormat = waveFormat;
            SetWPM(wpm);
        }


        public bool SetWPM(double wpm) {
            // returns true on success

            if (wpm < 1 || wpm > 200) {
                // throw error?
                return false;
            }

            Wpm = wpm;
            DitSeconds = 60.0 / (50.0 * wpm);
            return true;
        }

        public long? LeverSamples(LeverKind lever, int symbol = -1) {
            //TODO: banana boat swing

            double? ditLen = null;
            if (lever == LeverKind.Dit) ditLen = 1;
            else if (lever == LeverKind.Dah) ditLen = 3;

            if (ditLen.HasValue) {
                return (long)(DitSeconds * ditLen.Value); //optimize: cache values? meh
            } else {
                return null;
            }

        }

        public long? ToSamples(double? dits) {
            if (dits.HasValue) {
                return (long)(DitSeconds * dits.Value);
            } else {
                return null;
            }
        }

    }
}
