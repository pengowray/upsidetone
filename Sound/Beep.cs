using NAudio.Gui;
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
        O1_ResetOscillator = 1 << 1, // (default)
        O1_InfiniteWave = 1 << 2, // keep same phase as other sine waves with same Phase
        Choices_O1 = O1_ResetOscillator | O1_InfiniteWave,

        // Phase (pick one)
        O2_Oscillator_0 = 1 << 3, // (default)
        O2_Oscillator_90 = 1 << 4, // cosine oscillator / 90 degrees out of phase; if infinite wave then cosine starts at start of time
        O2_Oscillator_180 = 1 << 5, // inverted oscillator / 180 degrees out of phase; phase inversion; mutually exclusive with other phase settings
        O2_Oscillator_270 = 1 << 6, // for completeness
        Choices_O2 = O2_Oscillator_0 | O2_Oscillator_90 | O2_Oscillator_180 | O2_Oscillator_270,

        // pick one [attack]
        O3_A_NoZeroCross = 1 << 7, // default
        O3_A_CrossZero = 1 << 8, // wait until crossing zero before starting; //TODO: optional direction
        O3_A_Aligned = 1 << 9, // [todo] quantized: calculate the start of a sine wave or the next 0 crossing // TODO: option to choose phase
        O3_A_Milhouse = 1 << 10, // wait until change direction (e.g. near +1 or -1)

        // pick one [decay]
        O4_D_NoZeroCross = 1 << 11, // default
        O4_D_CrossZero = 1 << 12, // wait until crossing zero before starting; //TODO: optional direction
        O4_D_Aligned = 1 << 13, // [todo] quantized: calculate the end of a sine wave or next 0 crossing //TODO: phase option
        O4_D_Milhouse = 1 << 14, // wait until change direction (e.g. near +1 or -1); like quantizing badly

        // pick one [attack smoothing]
        O5_A_NoSmooth = 1 << 15, // no fade in (default)
        O5_A_Smoothed = 1 << 16, // fade in

        // pick one [decay smoothing]
        O6_D_NoSmooth = 1 << 17, // no fade out (default)
        O6_D_Smoothed = 1 << 18, // fade out

        // Defaults (individual setting defaults; For actual defaults use: Preset_Smooth and Decay_Preset_Smooth
        A_Defaults = O1_ResetOscillator | O2_Oscillator_0 | O3_A_NoZeroCross | O5_A_NoSmooth,
        D_Defaults = O4_D_NoZeroCross | O6_D_NoSmooth,
        Defaults = A_Defaults | D_Defaults,

        //experimental:
        A_Preset_PhaseAgnostic = O1_InfiniteWave, // PhaseAgnostic
        A_Preset_CosinePop = O2_Oscillator_90, // always click / Milhouse Mode / Minimalist House IDM / Micro-rhythms / Kiki

        // differential phase-shift keying (DPSK) between dots and dashes; 180 degrees between them. Unknown or straigh key inputs will remaining 0 degrees (in between)
        A_Preset_DPSK_Dit = O1_InfiniteWave | O5_A_Smoothed | O2_Oscillator_270, 
        A_Preset_DPSK_Dah = O1_InfiniteWave | O5_A_Smoothed | O2_Oscillator_90,
        A_Preset_DPSK_Other = O1_InfiniteWave | O5_A_Smoothed + O2_Oscillator_0, // straight key or unknown

        // Decay Presets [todo: sinc/erf/etc]
        D_Preset_Smooth = O6_D_Smoothed | O4_D_NoZeroCross,
        D_Preset_Sharp = O4_D_CrossZero | O6_D_NoSmooth,

        // Experimental Decay Presets
        D_Preset_Milhouse = O4_D_Milhouse | O6_D_NoSmooth,

        // General presets (attack + decay) InfiniteWave + defaults but Oscillator_90 for fun. [Oscillator may be anything.]

        Preset_Smooth         = O1_ResetOscillator | O2_Oscillator_0 | O3_A_NoZeroCross | O4_D_NoZeroCross | O5_A_Smoothed | O6_D_Smoothed,
        Preset_SharpNoClick   = O1_ResetOscillator | O2_Oscillator_0 | O3_A_NoZeroCross | O4_D_CrossZero | O5_A_NoSmooth | O6_D_NoSmooth,
        Preset_DoubleMilhouse = O1_ResetOscillator | O2_Oscillator_90 | O3_A_NoZeroCross | O4_D_Milhouse | O5_A_NoSmooth | O6_D_NoSmooth,

        // "unsynchronized" / "free running" osscillator. start and end at any phase without smoothing or other mitigation, but also not deliberate (so not Milhouse)
        Preset_ClickFreely_0 = O1_InfiniteWave | O2_Oscillator_0 | O3_A_NoZeroCross | O5_A_NoSmooth | O6_D_NoSmooth | O4_D_NoZeroCross,
        Preset_ClickFreely_90 = O1_InfiniteWave | O2_Oscillator_90 | O3_A_NoZeroCross | O5_A_NoSmooth | O6_D_NoSmooth | O4_D_NoZeroCross,
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
        public WaveFormat WaveFormat;
        
        // todo: package up options into audio settings or something

        //float AttackSeconds = 0.015f;
        //float ReleaseSeconds = 0.015f;
        float AttackSeconds = 0.008f; // linear fade in
        float ReleaseSeconds = 0.008f; // linear fade out
        float ReleaseSecondsAdsr = 0.015f; // uses a different curve

        public double Frequency = 550;
        public double Gain = 0.5;

        public Beep(WaveFormat format) {
            WaveFormat = format;
        }
        public ISampleProvider MakeBeep(long startSamples, long? lengthSamples, long? johncage, BeepAttackDecay options = BeepAttackDecay.Preset_Smooth) {
            //TODO: fader

            double phase = 0;
            if (options == BeepAttackDecay.O2_Oscillator_90) {
                phase = .5;
            } else if (options == BeepAttackDecay.O2_Oscillator_180) {
                phase = 1.0;
            } else if (options == BeepAttackDecay.O2_Oscillator_270) {
                phase = 1.5;
            }

            SineGenerator sine;
            //TODO: technically don't need sampleLength if Smoothed
            if (options == BeepAttackDecay.O1_ResetOscillator) {
                sine = new SineGenerator(WaveFormat.SampleRate, WaveFormat.Channels, 0, sampleLength: lengthSamples, startPhase: phase) {
                    Frequency = Frequency,
                    Gain = Gain
                };
                sine.Gain = Gain;
            } else {
                sine = new SineGenerator(WaveFormat.SampleRate, WaveFormat.Channels, startSamples, sampleLength: lengthSamples, startPhase: phase) {
                    Frequency = Frequency,
                    Gain = Gain
                };
                sine.Gain = Gain;
            }

            if (lengthSamples.HasValue) {
                // cut off regardless, but may fade first (fader)
                sine.EndInSamples(lengthSamples.Value); // cuts at zero 
            }

            FadeOutSampleProvider fader = null;
            //if ((options & (BeepAttackDecay.A_Smoothed | BeepAttackDecay.D_Smoothed)) > 0) {
            if (options.HasFlag(BeepAttackDecay.O5_A_Smoothed) || options.HasFlag(BeepAttackDecay.O6_D_Smoothed)) {

                fader = new FadeOutSampleProvider(sine);
                if (options.HasFlag(BeepAttackDecay.O5_A_Smoothed)) {
                    fader.SetFadeInSeconds(AttackSeconds);
                }
                if (options.HasFlag(BeepAttackDecay.O6_D_Smoothed) && lengthSamples.HasValue) {

                    fader.SetFadeOutSamples((int)(lengthSamples - (ReleaseSeconds * WaveFormat.SampleRate)), (int)(ReleaseSeconds * WaveFormat.SampleRate));
                } else if (options.HasFlag(BeepAttackDecay.O6_D_Smoothed) && !lengthSamples.HasValue) {
                    //var StraightAdsr = new AdsrSampleProvider(fader.ToMono(1, 0)) {
                    var StraightAdsr = new AdsrSampleProvider(fader) {
                        //AttackSeconds = AttackSeconds,
                        ReleaseSeconds = ReleaseSecondsAdsr // gets used when stopping
                    };
                    return StraightAdsr;

                }

                return fader;
            }

            return sine;

            //follow by silence [todo]
            //new SilenceProvider(sine.WaveFormat);




        }
    }
}
