using Microsoft.VisualBasic.Logging;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
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
        Dits,
        Dahs,

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

    public delegate void LeverEvent(Levers levers, LeverKind lever);
    public delegate void LeverDoublePressed(Levers levers, LeverKind lever, bool doublePressed, bool priorityIncreased);

    public class Levers {

        //ToneMaker Sounder; 

        //TODO: also track source of press (e.g. via which mouse button)

        public KeyerMode Mode = KeyerMode.BugStyle;

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
                        return LeverKind.Dits;
                    } else {
                        return LeverKind.PoliteStraight;
                    }

                case KeyerMode.IambicA:
                case KeyerMode.IambicB:
                case KeyerMode.NoIambic:
                case KeyerMode.SqueezeMute:
                case KeyerMode.Ultimatic:
                    if (left) {
                        return LeverKind.Dits;
                    } else {
                        return LeverKind.Dahs;
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
                //TODO: or just ignore?
                throw new ArgumentException("No lever specified");
                //return;
            }

            lock (Down) {
                bool doublePressed = false;
                bool priorityIncreased = false;
                if (Down.Contains(lever)) {
                    if (IgnoreDoublePresses) {
                        return;
                    }
                    doublePressed = true;
                    if (Down.LastOrDefault() == lever) {
                        // already pressed previously, and was last lever pressed
                        LeverDoubled?.Invoke(this, lever, doublePressed, priorityIncreased);
                        return;
                    } else {
                        // already pressed down, but another lever was pressed more recently
                        Down.Remove(lever);
                        Down.Add(lever);
                        priorityIncreased = true;
                        // in case there's a time this actually matters, fire an event.
                        LeverDoubled?.Invoke(this, lever, doublePressed, priorityIncreased);
                        return;
                    }
                }
                Down.Add(lever);
                LeverDown?.Invoke(this, lever);
            }
        }

        private void ReleaseLever(LeverKind lever) {
            if (lever == LeverKind.None) {
                //TODO: or just ignore?
                throw new ArgumentException("No lever specified");
                //return;
            }

            lock (Down) {
                if (Down.Contains(lever)) {
                    Down.Remove(lever);
                }
            }
            LeverUp?.Invoke(this, lever);
        }

    }
}
