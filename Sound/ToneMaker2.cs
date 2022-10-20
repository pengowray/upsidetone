using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.Metrics;

using NAudio.Mixer;
using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Navigation;
using System.Windows.Media.Animation;
using System.Windows.Forms;
//using Voicemeeter;

//using NWaves.Audio;

namespace upSidetone.Sound {

    //rewrite of ToneMaker_Old (previously called "Sounder")

    public class ToneMaker : IDisposable {

        //TODO: per lever
        private double Wpm;
        private double DitSeconds;

        private double Freq;
        private double Gain; // Volume

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

        public bool SetFreq(double freq) {
            // returns true on success

            if (freq < 2 || freq > 20000) {
                // throw error?
                return false;
            }

            Freq = freq;

            if (Beep != null) 
                Beep.Frequency = freq;

            return true;
        }

        public bool SetGain(double gain) {
            // returns true on success

            if (gain < 0 || gain > 1) {
                // throw error?
                return false;
            }

            Gain = gain;

            if (Beep != null)
                Beep.Gain = gain;

            return true;
        }

        //SwitchableDelayedSound Playing;
        //LeverKind PlayingLever;
        //SwitchableDelayedSound Next;
        //LeverKind NextLever;
        List<SwitchableDelayedSound> Playing = new();
        //public List<LeverKind> Played = new(); // dits and dahs. or just a string of ".-*" ?

        BeepAttackDecay Options = BeepAttackDecay.Preset_Smooth;
        //BeepAttackDecay Options = BeepAttackDecay.Preset_SharpNoClick; // not working? TODO (endless beep)

        //SwitchableDelayedSound AndThen;

        AudioOut AudioOut;
        ScoreProvider ScoreProvider;
        //MixingSampleProvider ScoreProvider;
        Beep Beep;
        private bool disposedValue;

        Queue<LeverKind> Required = new();
        LeverKind[]? Fill;
        int FillPos = 0;

        public WaveFormat ParentWaveFormat => AudioOut?.Format;

        // note: We use mono because mono is assumed by FadeOutSampleProvider and SwitchableDelayedSound;
        // TODO: make them work with stereo / abitrary channels
        public WaveFormat WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(ParentWaveFormat.SampleRate, 1);

        public ToneMaker(AudioOut audioOut) {
            
            SetWPM(18.0); // default wpm
            SetFreq(550);

            AudioOut = audioOut;
            //ScoreProvider = new MixingSampleProvider(ParentWaveFormat);
            //ScoreProvider = new ScoreProvider(ParentWaveFormat);
            ScoreProvider = new ScoreProvider(WaveFormat);
            ScoreProvider.ReadFully = true;
            Beep = new Beep(WaveFormat);
            Beep.Frequency = Freq;
        }

        public void Enable() {
            AudioOut.Mixer.AddMixerInput(ScoreProvider.ToStereo());
        }

        public void ListenToLevers(Levers levers) {
            levers.LeverDown += Levers_LeverDown;
            levers.LeverUp += Levers_LeverUp;
            levers.LeverDoubled += Levers_LeverDoubled;
        }

        public void StopListeningToLevers(Levers levers) {
            levers.LeverDown -= Levers_LeverDown;
            levers.LeverUp -= Levers_LeverUp;
            levers.LeverDoubled -= Levers_LeverDoubled;
        }

        private void Levers_LeverDoubled(Levers levers, LeverKind lever, bool doublePressed, bool priorityIncreased) {
            // todo
        }

        private void Levers_LeverUp(Levers levers, LeverKind lever, LeverKind require, LeverKind[]? fill) {
            //TODO: check if correct lever released
            //TODO: check if other lever is down

            //if bringing up active straight key...

            if (lever == LeverKind.Straight || lever == LeverKind.PoliteStraight) {
                //TODO: bring up straight key

                var toUp = Playing.Where(sound => !sound.IsLockedIn && sound.Lever == lever);
                //toUp.Stop(); //TODO
            }

            // the rest is the same as Levers_LeverDown()
            Levers_LeverDown(levers, lever, require, fill);
        }


        private void Levers_LeverDown(Levers levers, LeverKind lever, LeverKind require, LeverKind[]? fill) {
            if (require != LeverKind.None) 
                Required.Enqueue(require);
            Fill = fill;
            FillPos = 0;

            //ThreeBeeps(levers, lever);
            RefillBeeps();
            //Debug.WriteLine("queu1: " + String.Join(" ", Playing.Select(p => p.Lever)));
        }


        private SwitchableDelayedSound? GetLastUp() {
            // finds the next blank sound, or otherwise the last sound which you can put another sound after.

            return Playing.LastOrDefault();

        }
        private SwitchableDelayedSound? GetNextUpAndDeleteRest() {
            // finds the next non-required, non-locked in Sound that can be replaced, or otherwise the best Sound to place a sound after.

            // returns in order of preference:
            // 1. first not required & not locked in -- for replacing
            // 2. first not required but locked in -- for going after
            // 3. last required -- for going after

            //int requiredIndex = Playing.FindLastIndex(p => !p.IsLockedIn && p.RequiredPlay);

            //SwitchableDelayedSound? lastRequired = null;
            SwitchableDelayedSound? lastCanPutOneAfter = null;
            SwitchableDelayedSound? firstCanReplace = null;
            //int ogReturnIndex = -2;

            List<SwitchableDelayedSound> newPlaying = new();
            int index = 0;
            foreach (var sound in Playing.ToArray()) {
                if ((sound.RequiredPlay || sound.IsLockedIn) && !IsDone(sound)) {
                    // maybe can put a sound after it
                    lastCanPutOneAfter = sound;
                    newPlaying.Add(sound); 
                    
                } else if (!sound.RequiredPlay && !sound.IsLockedIn && firstCanReplace == null) {
                    // a better choice showed up after: not locked in (i.e. placeholder)
                    firstCanReplace = sound;
                    newPlaying.Add(sound);

                } else {
                    // delete (unless in the middle of playing)
                    if (sound.IsLockedIn && !IsDone(sound)) {
                        newPlaying.Add(sound); 
                    } else {
                        Delete(sound);
                    }
                }

                index++;
            }
            Playing = newPlaying;

            bool clearNextUp = true;
            if (clearNextUp && firstCanReplace != null && !firstCanReplace.IsLockedIn) {
                // clear next up
                // todo: might as well always do this?
                firstCanReplace.SetChoice(null);
            }


            return firstCanReplace ?? lastCanPutOneAfter;
        }


        private Tuple<LeverKind, bool> GetNextLever() {
            // returns LeverKind and if it's "required" (e.g. not deleted from the queue if paddles released)

            if (Required.TryDequeue(out LeverKind lever)) {
                // assume not None because we don't let that get queued
                if (lever == LeverKind.None) {
                    //try again. (risky recursion)
                    return GetNextLever();
                }
                return new(lever, true);
            }

            if (Fill == null || Fill.Length == 0) {
                // nothing queued
                return new(LeverKind.None, false);
            }

            var next = Fill[FillPos];
            FillPos = (FillPos + 1) % Fill.Length;
            return new(next, false);
        }

        private void FillMissingBeeps() {
            // (done?) TODO: don't remove "required" beeps

            var playing = Playing.Where(x => !x.IsLockedIn);
            if (!playing.Any()) {
                RefillBeeps();
                // AddPlaceholder(); // included in above
                return;

            } else {
                var last = playing.Last();
                int count = playing.Count();
                for (int i = count; i <= 3; i++) {
                    var nextLever = GetNextLever(); // XXX

                    var beep = MakeBeep(nextLever.Item1, last, replaceIfPossible:false, required: nextLever.Item2);
                    last = beep;

                    if (nextLever.Item1 == LeverKind.None) {
                        // only do one "None" beep (which acts as a placeholder)
                        return;
                    }
                }

                AddPlaceholder();

            }
        }

        private void RefillBeeps() {

            var nextBeep = GetNextUpAndDeleteRest();
            var nextLever = GetNextLever();

            // "optimization"
            //if (nextLever.Item1 == LeverKind.None) {
            //    AddPlaceholder();
            //    return;
            //}

            // always do one, even if none
            var sound = MakeBeep(nextLever.Item1, afterThis: nextBeep, replaceIfPossible: true, required: nextLever.Item2);

            var prev = sound;
            for (int i = 0; i < 3; i++) {
                if (prev.Chosen == null) {
                    break;
                }

                var afterNextLever = GetNextLever();
                var newSound = MakeBeep(afterNextLever.Item1, afterThis: prev, required: afterNextLever.Item2);
                prev = newSound;
            }

            AddPlaceholder();
        }

        private SwitchableDelayedSound AddPlaceholder() {
            // if needed and plausible, add a placeholder at the end

            var last = GetLastUp(); // = Playing.LastOrDefault();

            if (last != null && last.Chosen != null && !IsDone(last) && ((last.DurationSamples ?? 0) > 0)) {
                // ok we could have a placeholder
                var placeholder = new SwitchableDelayedSound(WaveFormat) {
                    Lever = LeverKind.None,
                    StartAt = last.StartAt + (last.DurationSamples ?? 0), // already checked DurationSamples has a value
                    DurationSamples = 0,
                    RequiredPlay = false,
                };
                last.Next = placeholder;
                Add(placeholder);
                
                return placeholder;
            }
            return null;
        }

        private void Next_SampleStarted(SwitchableDelayedSound sender) {

            // run in a thread so can get back sooner though not sure if it's actually needed

            _ = Task.Run(() => {
                sender.SampleStarted -= Next_SampleStarted;
                bool inPlaying = Playing.Contains(sender);
                if (inPlaying) {
                    //ThreeBeeps(null, LeverKind.Dits);
                    FillMissingBeeps();
                }

            }); // .ContinueWith(LogResult));


        }

        private SwitchableDelayedSound MakeBeep(LeverKind lever, SwitchableDelayedSound? afterThis = null, bool replaceIfPossible = false, bool required = false) {

            //double ditLen, double whenInDits = 0;
            double? ditLen = null;
            if (lever == LeverKind.Dit) ditLen = 1;
            if (lever == LeverKind.Dah) ditLen = 3;
            if (lever == LeverKind.None) ditLen = null; // placeholder 0 beep

            long ditLenSamples = (long)(DitSeconds * WaveFormat.SampleRate);
            long? beepSamples = ditLen.HasValue ? (long)(ditLen * DitSeconds * WaveFormat.SampleRate) : null;

            //long whenSamples = (long)(ditSeconds * whenInDits * AudioOut.Format.SampleRate);
            //long currentPos = ScoreProvider.CurrentSamplePos; // used here for calculating waveform phase; not for positioning the note
            //long currentPos = (!Options.HasFlag(BeepAttackDecay.InfiniteWave)) ? ScoreProvider.CurrentSamplePos : 0;
            long currentPos = 0; //  0 + whenSamples; // todo: replace 0 with currentPos depending on settings

            ISampleProvider beep = null;
            if (lever != LeverKind.None) {
                beep = Beep.MakeBeep(currentPos, beepSamples, johncage: ditLenSamples, options: Options);
            }

            // queue empty next? nah
            //var next = new SwitchableDelayedSound(WaveFormat);
            //next.StartAt = ditLenSamples;
            //var beepWithNext = beep.FollowedBy(next);
            //var sound = new SwitchableDelayedSound(beepWithNext);

            bool actuallyReplace = false;

            if (afterThis != null) { 
                if (afterThis.Chosen == null) {
                    // actually replace, rather than go after, a null empty sound
                    actuallyReplace = true;
                } else if (replaceIfPossible && !afterThis.IsLockedIn && !afterThis.RequiredPlay) {
                    actuallyReplace = true;
                } else if (IsDone(afterThis)) { // too unreliable
                    afterThis = null;
                }
            }

            SwitchableDelayedSound sound;
            if (actuallyReplace) {
                sound = afterThis;
                sound.SetChoice(beep);
                afterThis = null;
            } else {
                if (beep == null) {
                    sound = new SwitchableDelayedSound(WaveFormat);
                } else {
                    sound = new SwitchableDelayedSound(beep);
                }
            }
            sound.Lever = lever;
            sound.RequiredPlay = required;
            //sound.Next = next;

            if (beepSamples != null) {
                sound.DurationSamples = beepSamples.Value + ditLenSamples;
            } else if (beep == null) {
                sound.DurationSamples = 0;
            } else {
                sound.DurationSamples = null; // unknown
            }


            if (afterThis != null && !actuallyReplace) { 
                // wouldn't enter here if actuallyReplace == true anway (because afterThis is nulled above)
                
                //if (sound.DurationSamples == 0) {  
                    // crash rather than do wrong (for debuging)
                    //throw new ArgumentException("Can't start after when durationless");
                    //TODO: fade out and play after
                //}

                // assumes ditLen pause after straight key
                //TODO: polite vs not polite
                //TODO: straight key after straight key (no pause? idk)
                sound.StartAt = afterThis.StartAt + (afterThis.DurationSamples ?? ditLenSamples);
                afterThis.Next = sound;

                //if (afterThis.Next != null) {
                //    afterThis.Next.SetChoice(sound);
                //}

            } else if (!actuallyReplace) {
                //TODO: set to null and let it get autofilled... but then harder to queue after it
                sound.StartAt = ScoreProvider.SampleCursor;
            }

            //sound.StartAt = whenSamples;
            //sound.StartAt = // TODO: delay

            //todo
            //StraightAdsr = new AdsrSampleProvider(faded.ToMono(1, 0)) {
            //AttackSeconds = AttackSeconds,
            //ReleaseSeconds = ReleaseSeconds // does not get used unless stopping early

            if (!actuallyReplace) {
                Add(sound);
            }

            return sound;
        }

        private bool IsDone(SwitchableDelayedSound sound) {
            return sound.IsDone(ScoreProvider.SampleCursor);
        }

        private void Add(SwitchableDelayedSound sound) {
            Playing.Add(sound);
            ScoreProvider.AddMixerInput(sound);
            sound.SampleStarted -= Next_SampleStarted; // remove first so only added once? (not sure if needed)
            sound.SampleStarted += Next_SampleStarted; // to clear or refill Playing list
        }

        private void Remove(SwitchableDelayedSound sound) {
            if (sound == null)
                return;

            Playing.Remove(sound);
            Delete(sound);
        }

        private void Delete(SwitchableDelayedSound sound) {
            // clear everything but don't remove from Playing
            if (sound == null)
                return;

            ScoreProvider.RemoveMixerInput(sound);
            sound.SampleStarted -= Next_SampleStarted;
            sound.SetChoice(null);
            sound.Next = null;
            sound.RequiredPlay = false;
        }


        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Sounder()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }


}

