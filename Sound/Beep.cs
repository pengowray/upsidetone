using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace upSidetone.Sound {


    // ridiciulous options set
    [Flags]
    public enum BeepAttackDecay {
        //None = 0, // No option selected

        //Reserved = 1 << 0,  // for no reason

        // pick one
        ResetOscillator = 1 << 1, // (default)
        InfiniteWave = 1 << 2, // keep same phase as other sine waves with same Phase
        Choices_Oscillator = ResetOscillator | InfiniteWave,

        // Phase (pick one)
        Oscillator_0 = 1 << 3, // (default)
        Oscillator_90 = 1 << 4, // cosine oscillator / 90 degrees out of phase; if infinite wave then cosine starts at start of time
        Oscillator_180 = 1 << 5, // inverted oscillator / 180 degrees out of phase; phase inversion; mutually exclusive with other phase settings
        Oscillator_270 = 1 << 6, // for completeness
        Choices_Phase = Oscillator_0 | Oscillator_90 | Oscillator_180 | Oscillator_270,

        // pick one [attack]
        A_NoZeroCross = 1 << 7, // default
        A_CrossZero = 1 << 8, // wait until crossing zero before starting; //TODO: optional direction
        A_Aligned = 1 << 9, // [todo] quantized: calculate the start of a sine wave or the next 0 crossing // TODO: option to choose phase
        A_Milhouse = 1 << 10, // wait until change direction (e.g. near +1 or -1)

        // pick one [decay]
        D_NoZeroCross = 1 << 11, // default
        D_CrossZero = 1 << 12, // wait until crossing zero before starting; //TODO: optional direction
        D_Aligned = 1 << 13, // [todo] quantized: calculate the end of a sine wave or next 0 crossing //TODO: phase option
        D_Milhouse = 1 << 14, // wait until change direction (e.g. near +1 or -1); like quantizing badly

        // pick one [attack smoothing]
        A_NoSmooth = 1 << 15, // no fade in (default)
        A_Smoothed = 1 << 16, // fade in

        // pick one [decay smoothing]
        D_NoSmooth = 1 << 17, // no fade out (default)
        D_Smoothed = 1 << 18, // fade out

        // Defaults (individual setting defaults; For actual defaults use: Preset_Smooth and Decay_Preset_Smooth
        A_Defaults = ResetOscillator | Oscillator_0 | A_NoZeroCross | A_NoSmooth,
        D_Defaults = D_NoZeroCross | D_NoSmooth,
        Defaults = A_Defaults | D_Defaults,

        // Presets
        A_Preset_Smooth = A_Smoothed | A_NoZeroCross | Oscillator_0 | ResetOscillator,
        A_Preset_Sharp = ResetOscillator | Oscillator_0 | ResetOscillator | A_NoSmooth | A_NoZeroCross,

        //experimental:
        A_Preset_PhaseAgnostic = InfiniteWave, // PhaseAgnostic
        A_Preset_CosinePop = Oscillator_90, // always click / Milhouse Mode / Minimalist House IDM / Micro-rhythms / Kiki

        // differential phase-shift keying (DPSK) between dots and dashes; 180 degrees between them. Unknown or straigh key inputs will remaining 0 degrees (in between)
        A_Preset_DPSK_Dit = InfiniteWave | A_Smoothed | Oscillator_270, 
        A_Preset_DPSK_Dah = InfiniteWave | A_Smoothed | Oscillator_90,
        A_Preset_DPSK_Other = InfiniteWave | A_Smoothed + Oscillator_0, // straight key or unknown

        // Decay Presets [todo: sinc/erf/etc]
        D_Preset_Smooth = D_Smoothed | D_NoZeroCross,
        D_Preset_Sharp = D_CrossZero | D_NoSmooth,

        // Experimental Decay Presets
        D_Preset_Milhouse = D_Milhouse | D_NoSmooth,

        // General presets (attack + decay) InfiniteWave + defaults but Oscillator_90 for fun. [Oscillator may be anything.]

        Preset_Smooth  = ResetOscillator | Oscillator_0 | A_Smoothed | D_Smoothed | A_NoZeroCross | D_NoZeroCross,
        Preset_DoubleMilhouse = ResetOscillator | Oscillator_90 | A_NoSmooth | A_NoZeroCross | D_NoSmooth | D_Milhouse,

        // "unsynchronized" / "free running" osscillator. start and end at any phase without smoothing or other mitigation, but also not deliberate (so not Milhouse)
        Preset_ClickFreely_0 = InfiniteWave | Oscillator_0 | A_NoZeroCross | A_NoSmooth | D_NoSmooth | D_NoZeroCross,
        Preset_ClickFreely_90 = InfiniteWave | Oscillator_90 | A_NoZeroCross | A_NoSmooth | D_NoSmooth | D_NoZeroCross,
    }

    // commented out because: just use above
    //[Flags]
    //public enum BeepDecay {
    //    //None = 0,
    //    NoZeroCross = 64, // default
    //    CrossZero = 128, // wait until crossing zero before starting; //TODO: direction
    //
    //    // pick one
    //    NoSmooth = 256, // no fade in (default)
    //    Smoothed = 512, // fade in
    //
    //    //TODO: Experimental Milhouse mode
    //
    //    Preset_Smooth = Smoothed + NoZeroCross,
    //    Preset_Sharp = CrossZero + NoSmooth,
    //}


    //TODO: rename 
    public class Beep {
        WaveFormat WaveFormat;
        DateTime EndTime;
        DateTime QueueNextBefore;
        SineGenerator? Sine;

        // todo: package up options into audio settings or something
        float Phase;
        //bool ReleaseQuantized; // end after crossing zero
        //bool OscillatorReset; // start at 0 degrees, sine 0; 
        BeepAttackDecay AttackDecayOptions = BeepAttackDecay.Preset_Smooth;
        float AttackSeconds = 0.015f; 
        float ReleaseSeconds = 0.015f;

        float Frequency = 550;

        public Beep(WaveFormat format) {
            WaveFormat = format;
        }

        public ISampleProvider MakeBeep(long startSamples, long? lengthSamples, long? johncage, BeepAttackDecay options = BeepAttackDecay.Preset_Smooth) {
            //TODO: fader

            double phase = 0;
            if (options == BeepAttackDecay.Oscillator_90) {
                phase = .5;
            } else if (options == BeepAttackDecay.Oscillator_180) {
                phase = 1.0;
            } else if (options == BeepAttackDecay.Oscillator_270) {
                phase = 1.5;
            }

            SineGenerator sine;
            if (options == BeepAttackDecay.ResetOscillator) {
                sine = new SineGenerator(WaveFormat.SampleRate, WaveFormat.Channels, 0, startPhase: phase) {
                    Frequency = Frequency,
                };
            } else {
                sine = new SineGenerator(WaveFormat.SampleRate, WaveFormat.Channels, startSamples, startPhase: phase) {
                    Frequency = Frequency,
                };
            }

            if (lengthSamples.HasValue) {
                // cut off regardless, but may fade first (fader)
                sine.EndInSamples((long)lengthSamples); // cuts at zero 
            }

            FadeOutSampleProvider fader = null;
            //if ((options & (BeepAttackDecay.A_Smoothed | BeepAttackDecay.D_Smoothed)) > 0) {
            if (options.HasFlag(BeepAttackDecay.A_Smoothed) || options.HasFlag(BeepAttackDecay.D_Smoothed)) {

                fader = new FadeOutSampleProvider(sine);
                if (options.HasFlag(BeepAttackDecay.A_Smoothed)) {
                    fader.SetFadeInSeconds(AttackSeconds);
                }
                if (options.HasFlag(BeepAttackDecay.D_Smoothed) && lengthSamples.HasValue) {
                    
                    fader.SetFadeOutSamples((int)(lengthSamples - ReleaseSeconds * WaveFormat.SampleRate), (int)(ReleaseSeconds * WaveFormat.SampleRate));
                }

                return fader;
            }

            return sine;

            //follow by silence [todo]
            //new SilenceProvider(sine.WaveFormat);




        }
    }
}
