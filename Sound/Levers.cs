﻿using Microsoft.VisualBasic.Logging;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace upSidetone.Sound {

    // will be converted to a LeverKind based on keyer mode
    public enum VirtualLever {
        None,
        Left,
        Right,
        // closeCircuit,
        // straight key only (or none if in another mode)
    }

    public enum LeverKind {
        //todo: let any kind of lever be "polite" or "impolite"
        //todo: abitrary dit/dah lengths
        //todo: "swings" -- change length based on where in letter
        //todo: attach a dial for WPM
        //todo: solo indefinite -- only allowe triggering if nothing else playing
        //todo: add queuing mode flags (basically embed KeyerMode)
        //todo: max "indefinite" length, e.g. 8 seconds before fading out

        None,
        Straight, // (indefinite) straight key (will extend other elements if pressed while they're going)
        PoliteStraight, // for BugStyle: will queue up and leave a pause between other elements
        Dit,
        Dah,
        Stop, // signal to not repeat fill

        //TODO:
        //MuteFade, // cease immediately but allow fade out
        //MuteAfterCurrent, // let current symbol finish
        //MuteAfterQueued (iambic B)
    }

    public enum KeyerMode {
        None,
        StraightKey, // or plain oscillator
        BugStyle,
        NoIambic, // aka "no squeeze". ignore second paddle until first released (i guess?) TODO: check how used elsewhere
        SqueezeMute, // second paddle silences until both released (accessibility) TODO
        IambicA,
        IambicB,
        Ultimatic,
        //BestGuess // 
    }

    // Not sure but maybe replace Sounder with this... actually use Sounder2

    // use this just for tracking which levers are down per source (todo)

    public delegate void LeverEvent(Levers levers, LeverKind lever, LeverKind require, LeverKind[]? fill);
    public delegate void LeverDoublePressed(Levers levers, LeverKind lever, bool doublePressed, bool priorityIncreased);

    public class Levers {

        //ToneMaker Sounder; 

        //TODO: also track source of press (e.g. via which mouse button)

        public KeyerMode Mode = KeyerMode.IambicA;

        // ignore if pressed a second time when already down;
        // if true, LeverDoubled event will never fire, and double presses won't change the order of levers
        public bool IgnoreDoublePresses;

        // swap left and right (virtual levers)
        public bool SwapLeftRight;

        List<LeverKind> Down = new(); // keys pressed and in what order

        public event LeverEvent? LeverDown;
        public event LeverEvent? LeverUp;
        public event LeverDoublePressed? LeverDoubled;

        public IEnumerable<LeverKind> GetLeversDown() {
            return Down.AsEnumerable();
        }

        public Levers() {
        }

        public Levers(KeyerMode keyerMode) {
            Mode = keyerMode;
        }

        public LeverKind LeverFreomVirtual(VirtualLever lever) {
            if (lever == VirtualLever.None) {
                return LeverKind.None;
            }

            bool left = lever == VirtualLever.Left;
            if (SwapLeftRight) left = !left;

            switch (Mode) {
                case KeyerMode.StraightKey:
                    return LeverKind.Straight;

                case KeyerMode.BugStyle:
                    if (left) {
                        return LeverKind.Dit;
                    } else {
                        return LeverKind.PoliteStraight;
                    }

                case KeyerMode.IambicA:
                case KeyerMode.IambicB:
                case KeyerMode.NoIambic:
                case KeyerMode.SqueezeMute:
                case KeyerMode.Ultimatic:
                    if (left) {
                        return LeverKind.Dit;
                    } else {
                        return LeverKind.Dah;
                    }

                default:
                    return LeverKind.None;
            }
        }



        public void PushLeverDown(VirtualLever vlever) {
            var lever = LeverFreomVirtual(vlever);
            PushLeverDown(lever);
        }
        public void ReleaseLever(VirtualLever vlever) {
            var lever = LeverFreomVirtual(vlever);
            ReleaseLever(lever);
        }


        public void PushLeverDown(LeverKind lever) {
            if (lever == LeverKind.None) {
                //TODO: throw or just ignore?
                //throw new ArgumentException("No lever specified");
                return;
            }

            LeverKind require = lever;
            LeverKind[]? fill = null;

            lock (Down) {
                bool doublePressed = false;
                if (Down.Contains(lever)) {
                    if (IgnoreDoublePresses) {
                        return;
                    }
                    doublePressed = true;
                    if (Down.LastOrDefault() == lever) {
                        //TODO: don't fire while locked
                        // already pressed previously, and was last lever pressed
                        LeverDoubled?.Invoke(this, lever, doublePressed, priorityIncreased: false);
                        return;
                    } else {
                        // already pressed down, but another lever was pressed more recently
                        Down.Remove(lever);
                        Down.Add(lever);
                        // in case there's a time this actually matters, fire an event.
                        LeverDoubled?.Invoke(this, lever, doublePressed, priorityIncreased: true);
                        return;
                    }
                }

                // defaults
                //LeverKind[] require = new LeverKind[] { lever }; // TODO: if there's a case where we need to add more than one then change this back to array or IEnumerable
                if (lever != LeverKind.PoliteStraight && lever != LeverKind.Straight) {
                    fill = RepeatFill(lever);
                }

                if (Mode == KeyerMode.BugStyle) {
                    // defaults are fine

                } else if (Mode == KeyerMode.Ultimatic) {
                    // defaults are fine?

                } else if (Mode == KeyerMode.IambicA) { 
                    if (lever == LeverKind.Dah && Down.Contains(LeverKind.Dit)) {
                        fill = RepeatFill(LeverKind.Dit, LeverKind.Dah);
                        //Debug.WriteLine($"dah dit dah...: {lever} " + String.Join(" ", fill.Take(4))); 
                    } else if (lever == LeverKind.Dit && Down.Contains(LeverKind.Dah)) {
                        fill = RepeatFill(LeverKind.Dah, LeverKind.Dit);
                        //Debug.WriteLine($"dit dah dit...: {lever} " + String.Join(" ", fill.Take(4)));
                    }

                }

                Down.Add(lever);
            }
            LeverDown?.Invoke(this, lever, require, fill);
        }

        private LeverKind[] RepeatFill(params LeverKind[] fillPattern) {
            return fillPattern;
        }

        private void ReleaseLever(LeverKind lever) {
            if (lever == LeverKind.None) {
                //TODO: throw or just ignore?
                //throw new ArgumentException("No lever specified");
                return;
            }

            LeverKind[] defaultFill = null; // default fill
            KeyerMode mode;
            int pos;
            bool wasLast;

            lock (Down) {

                if (!Down.Contains(lever)) {
                    //TODO: double release event?
                    return;
                }

                mode = Mode;

                pos = Down.IndexOf(lever);
                wasLast = pos == Down.Count - 1;
                Down.Remove(lever);
                var newLast = Down.LastOrDefault();

                if (newLast != LeverKind.None) {
                    if (newLast != LeverKind.Straight && newLast != LeverKind.PoliteStraight) {
                        defaultFill = RepeatFill(newLast, LeverKind.None);
                    } else {
                        defaultFill = RepeatFill(newLast);
                    }
                }
            }
                
            if (mode == KeyerMode.IambicA) {
                if (lever == LeverKind.Dit && Down.Contains(LeverKind.Dah)) {
                    LeverKind require = wasLast ? LeverKind.None : LeverKind.Dah;
                    LeverUp?.Invoke(this, lever, require, defaultFill);
                    return;
                } else if (lever == LeverKind.Dah && Down.Contains(LeverKind.Dit)) {
                    LeverKind require = wasLast ? LeverKind.None : LeverKind.Dit;
                    LeverUp?.Invoke(this, lever, require, defaultFill);
                    return;
                }
            } else if (mode == KeyerMode.Ultimatic) {
                if (lever == LeverKind.Dit && Down.Contains(LeverKind.Dah)) {
                    LeverKind require = wasLast ? LeverKind.None : LeverKind.Dah;
                    LeverUp?.Invoke(this, lever, require, defaultFill);
                    return;
                } else if (lever == LeverKind.Dah && Down.Contains(LeverKind.Dit)) {
                    LeverKind require = wasLast ? LeverKind.None : LeverKind.Dit;
                    LeverUp?.Invoke(this, lever, require, defaultFill);
                    return;
                }
            } else if (mode == KeyerMode.BugStyle) {
                if (lever == LeverKind.Dit && Down.Contains(LeverKind.PoliteStraight)) {

                    //LeverKind require = wasLast ? LeverKind.None : LeverKind.PoliteStraight;
                    //LeverUp?.Invoke(this, lever, LeverKind.None, defaultFill);

                    //var fill = wasLast ? null : RepeatFill(LeverKind.PoliteStraight, LeverKind.Stop);
                    var fill = RepeatFill(LeverKind.PoliteStraight, LeverKind.Stop);
                    LeverUp?.Invoke(this, lever, LeverKind.None, fill);
                    
                    return;
                } else if (lever == LeverKind.PoliteStraight && Down.Contains(LeverKind.Dit)) {
                    LeverKind require = wasLast ? LeverKind.None : LeverKind.Dit;
                    LeverUp?.Invoke(this, lever, require, defaultFill);
                    return;
                }
            }

            LeverUp?.Invoke(this, lever, LeverKind.None, defaultFill);

        }


        public void ReleaseAll() {
            if (!Down.Any()) {
                //TODO: amaybe invoke anyway?
                return;
            }

            if (Down.Count == 1) {
                var release = Down.FirstOrDefault();
                Down.Clear();
                LeverUp?.Invoke(this, release, LeverKind.None, null);
                return;
            }

            Down.Clear();
            var which = LeverKind.None; // really should be "all"
            LeverUp?.Invoke(this, which, LeverKind.None, null);

        }
    }
}
