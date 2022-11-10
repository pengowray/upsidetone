using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace upSidetone.Sound {

    public enum LeverKind {
        //todo: let any kind of lever be "polite" or "impolite"
        //todo: abitrary dit/dah lengths
        //todo: "swings" -- change length based on where in letter
        //todo: attach a dial for WPM
        //todo: solo indefinite -- only allowe triggering if nothing else playing
        //todo: add queuing mode flags (basically embed KeyerMode)
        //todo: max "indefinite" length, e.g. 8 seconds before fading out

        None,
        Oscillate, // (indefinite) straight key (will extend other elements if pressed while they're going)
        Straight, // previously called "PoliteStraight" e.g. for second paddle elements hybrid: will queue up and leave a pause between other elements
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
        Oscillator, // plain oscillator, for external input (e.g. serial), no additional spacing added; perhaps use with a bug (until ACS option added) 
        StraightKey, // for straightkeys, cooties, sideswipers -- may add minimum spacing between elements; 
        IambicA,
        IambicB,
        Hybrid, // semi-automatic two-paddle "bug" — bug not a bug; don't pick this option with a bug
        Ultimatic,
        NoRepeats, // todo: find a name in the positive
        Locking, // second paddle silences (locks up) until both released (possibly a useful accessibility mode)
        HybridLocking,
        Swamp, // ignore second paddle until first released. Does not seem overly useful.
        //BestGuess // 
    }

    public class KeyingEventArgs {
        public VirtualLeverEventArgs? leverInfo;
        public Keying keyer; // this (needed?)
        public bool isDown;
        public LeverKind lever;
        public LeverKind require;
        public LeverKind[]? fill;
        public bool bFill = false;
    }
    
    public delegate void KeyingEvent(KeyingEventArgs args);


    public class Keying {
        public event KeyingEvent? LeverKeyed; // e.g. for iambic event (up or down)

        public KeyerMode Mode = KeyerMode.IambicA;
        private List<LeverKind> Down = new(); // keys pressed and in what order 
        private object Lock = new object();

        public Keying(KeyerMode keyerMode) {
            Mode = keyerMode;
        }

        public void ListenToLevers(Levers levers) {
            levers.LeverDown -= Levers_LeverDown;
            levers.LeverUp -= Levers_LeverUp;

            levers.LeverDown += Levers_LeverDown;
            levers.LeverUp += Levers_LeverUp;
        }


        public LeverKind LeverFromVirtual(VirtualLever lever) {
            if (lever == VirtualLever.None) {
                return LeverKind.None;
            }

            bool left = lever == VirtualLever.Left;
            //if (SwapLeftRight) left = !left;//

            switch (Mode) {
                case KeyerMode.StraightKey:
                    return LeverKind.Oscillate;

                case KeyerMode.Hybrid:
                case KeyerMode.HybridLocking:
                    if (left) {
                        return LeverKind.Dit;
                    } else {
                        return LeverKind.Straight;
                    }

                case KeyerMode.IambicA:
                case KeyerMode.IambicB:
                case KeyerMode.Locking:
                case KeyerMode.NoRepeats:
                case KeyerMode.Ultimatic:
                case KeyerMode.Swamp:
                    if (left) {
                        return LeverKind.Dit;
                    } else {
                        return LeverKind.Dah;
                    }

                default:
                    return LeverKind.None;
            }
        }


        private void Levers_LeverDown(VirtualLeverEventArgs args) {
            var lever = LeverFromVirtual(args.vLever);
            PushLeverDown(lever, args);
        }


        private void Levers_LeverUp(VirtualLeverEventArgs args) {
            var lever = LeverFromVirtual(args.vLever);
            ReleaseLever(lever, args);
        }


        public void PushLeverDown(VirtualLever vlever) {
            var lever = LeverFromVirtual(vlever);
            PushLeverDown(lever);
        }
        public void ReleaseLever(VirtualLever vlever) {
            var lever = LeverFromVirtual(vlever);
            ReleaseLever(lever);
        }

        public void PushLeverDown(LeverKind lever, VirtualLeverEventArgs vArgs = null) {
            if (lever == LeverKind.None) {
                //TODO: throw or just ignore?
                //throw new ArgumentException("No lever specified");
                return;
            }

            bool IgnoreDoublePresses = true;

            var args = new KeyingEventArgs() {
                leverInfo = vArgs,
                keyer = this,
                lever = lever,
                isDown = true,
                require = lever, // may change
                fill = null, // may change
                bFill = false, // may change
            };

            lock (Lock) {
                bool doublePressed = false;
                if (Down.Contains(lever)) {
                    // TODO: allow sometimes? (like in Levers)

                    if (IgnoreDoublePresses) {
                        return;
                    }
                    doublePressed = true;
                }

                // defaults
                //LeverKind[] require = new LeverKind[] { lever }; // TODO: if there's a case where we need to add more than one then change this back to array or IEnumerable
                if (lever != LeverKind.Straight && lever != LeverKind.Oscillate) {
                    args.fill = RepeatFill(lever);
                }

                if (Mode == KeyerMode.Hybrid) {
                    // defaults are fine

                } else if (Mode == KeyerMode.Ultimatic) {
                    // defaults are fine?
                } else if (Mode == KeyerMode.Swamp) {
                    if (Down.Any()) {
                        args.require = LeverKind.None;
                        args.fill = RepeatFill(Down.FirstOrDefault());
                    }

                } else if (Mode == KeyerMode.IambicA) {
                    if (lever == LeverKind.Dah && Down.Contains(LeverKind.Dit)) {
                        args.fill = RepeatFill(LeverKind.Dit, LeverKind.Dah);
                        //Debug.WriteLine($"dah dit dah...: {lever} " + String.Join(" ", fill.Take(4))); 
                    } else if (lever == LeverKind.Dit && Down.Contains(LeverKind.Dah)) {
                        args.fill = RepeatFill(LeverKind.Dah, LeverKind.Dit);
                        //Debug.WriteLine($"dit dah dit...: {lever} " + String.Join(" ", fill.Take(4)));
                    }

                } else if (Mode == KeyerMode.IambicB) {
                    if (lever == LeverKind.Dah && Down.Contains(LeverKind.Dit)) {
                        args.bFill = true;
                        args.fill = RepeatFill(LeverKind.Dit, LeverKind.Dah);
                    } else if (lever == LeverKind.Dit && Down.Contains(LeverKind.Dah)) {
                        args.bFill = true;
                        args.fill = RepeatFill(LeverKind.Dah, LeverKind.Dit);
                    }
                } else if (Mode == KeyerMode.NoRepeats) {
                    args.fill = null;
                } else if (Mode == KeyerMode.Locking) {
                    if (Down.Any()) {
                        args.require = LeverKind.None;
                        args.fill = null;
                    }
                } else if (Mode == KeyerMode.HybridLocking) {
                    if (Down.Any()) {
                        args.require = LeverKind.None;
                        args.fill = null;
                    }
                }
                Down.Add(lever);
            }
            LeverKeyed?.Invoke(args);
        }

        private LeverKind[] RepeatFill(params LeverKind[] fillPattern) {
            return fillPattern;
        }

        private void ReleaseLever(LeverKind lever, VirtualLeverEventArgs vArgs = null) {
            if (lever == LeverKind.None) {
                //TODO: throw or just ignore?
                //throw new ArgumentException("No lever specified");
                return;
            }

            var args = new KeyingEventArgs() {
                leverInfo = vArgs,
                keyer = this,
                lever = lever,
                isDown = false,
                require = LeverKind.None, // may change below
                fill = null,  // will likely change below
                bFill = false, // may change below
            };

            KeyerMode mode;
            int pos;
            bool wasLast;

            lock (Lock) {

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
                    if (newLast != LeverKind.Oscillate && newLast != LeverKind.Straight) {
                        args.fill = RepeatFill(newLast, LeverKind.None); // default fill
                    } else {
                        args.fill = RepeatFill(newLast); // default fill
                    }
                }
            }

            if (mode == KeyerMode.IambicA) {
                if (lever == LeverKind.Dit && Down.Contains(LeverKind.Dah)) {
                    args.require = wasLast ? LeverKind.Dah : LeverKind.None;
                } else if (lever == LeverKind.Dah && Down.Contains(LeverKind.Dit)) {
                    args.require = wasLast ? LeverKind.Dit : LeverKind.None;
                }
            } else if (mode == KeyerMode.IambicB) {
                //TODO: not sure
                if (lever == LeverKind.Dit && Down.Contains(LeverKind.Dah)) {
                    //LeverKind require = wasLast ? LeverKind.Dah : LeverKind.None;
                    args.require = LeverKind.None;
                } else if (lever == LeverKind.Dah && Down.Contains(LeverKind.Dit)) {
                    //LeverKind require = wasLast ? LeverKind.Dit : LeverKind.None;
                    args.require = LeverKind.None;
                }

            } else if (mode == KeyerMode.Ultimatic) {
                if (lever == LeverKind.Dit && Down.Contains(LeverKind.Dah)) {
                    args.require = wasLast ? LeverKind.Dah : LeverKind.None;
                } else if (lever == LeverKind.Dah && Down.Contains(LeverKind.Dit)) {
                    args.require = wasLast ? LeverKind.Dit : LeverKind.None;
                }
            } else if (mode == KeyerMode.Swamp) {
                if (lever == LeverKind.Dit && Down.Contains(LeverKind.Dah)) {
                    args.require = wasLast ? LeverKind.Dah : LeverKind.None;
                } else if (lever == LeverKind.Dah && Down.Contains(LeverKind.Dit)) {
                    args.require = wasLast ? LeverKind.Dit : LeverKind.None;
                }
            } else if (mode == KeyerMode.Hybrid) {
                if (lever == LeverKind.Dit && Down.Contains(LeverKind.Straight)) {
                    //var fill = RepeatFill(LeverKind.PoliteStraight, LeverKind.Stop);
                    //LeverUp?.Invoke(this, lever, LeverKind.None, fill);
                    // note: required lever should not be started if straight key released
                    args.require = wasLast ? LeverKind.Straight : LeverKind.None;
                    args.fill = null;
                } else if (lever == LeverKind.Straight && Down.Contains(LeverKind.Dit)) {
                    args.require = wasLast ? LeverKind.Dit : LeverKind.None;
                }
            } else if (mode == KeyerMode.NoRepeats) {
                args.fill = null;
            } else if (Mode == KeyerMode.Locking) {
                args.fill = null;
            } else if (Mode == KeyerMode.HybridLocking) {
                args.fill = null;
            }

            LeverKeyed?.Invoke(args);
        }


        public void ReleaseAll() {
            if (!Down.Any()) {
                //TODO: amaybe invoke anyway?
                return;
            }
            var args = new KeyingEventArgs() {
                leverInfo = null,
                keyer = this,
                lever = LeverKind.None, // default (may change below): really should be "all"
                isDown = false,
                require = LeverKind.None, 
                fill = null, 
                bFill = false,
            };

            lock (Lock) {
                if (Down.Count == 1) {
                    args.lever= Down.FirstOrDefault();
                }
                Down.Clear();
            }
            LeverKeyed?.Invoke(args);
        
        }
    }
}
