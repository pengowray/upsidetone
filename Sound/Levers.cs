using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace upSidetone.Sound {

    public enum LeverKind {
        None,
        Straight,
        Dits,
        Dahs,
    }

    // Not sure but maybe replace Sounder with this... actually use Sounder2
    
    // use this just for tracking which levers are down per source (todo)

    public class Levers {
        //bool PauseBefore;
        //bool PauseAfter;

        Sounder Sounder;


        LeverKind Kind;
        long Samples = 0; // how many samples in are we
        
        public Levers(Sounder sounder, LeverKind kind = LeverKind.Straight) {
            Sounder = sounder;
            
            
            Kind = kind;
        }

        private void PlayNow() {
            //TODO: recycle
            var sine = new SineGenerator(Sounder.Format.SampleRate, Sounder.Format.Channels);
            //var beep = new Beep(sine, Samples, );

            MixingSampleProvider? Mixer;
            //Mixer.AddMixerInput()


        }

        public void GetBeep() {
            if (Kind == LeverKind.Straight) {
                
                //new Beep(Sounder.Format.BitsPerSample, );
            } else {

            }
        }

        public void PressNext() {


        }
    }
}
