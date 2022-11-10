using Microsoft.VisualBasic.Logging;
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
using System.Windows.Media.Animation;

namespace upSidetone.Sound {

    // will be converted to a LeverKind based on keyer mode
    public enum VirtualLever {
        None,
        Left,
        Right,
        // closeCircuit,
        // straight key only (or none if in another mode)
        
    }

    // Not sure but maybe replace Sounder with this... actually use Sounder2

    // use this just for tracking which levers are down per source (todo)

    public class VirtualLeverEventArgs {
        public Levers levers;
        public VirtualLever vLever;
        public bool isDown;
        public bool isSwapped; // probably not needed, but might surface to user at some point
        //public string source; // todo: e.g. "mouse"
        public bool doublePressed;
        public bool priorityIncreased; // double pressed but was not the last pressed
    }

    //public delegate void LeverEvent(Levers levers, LeverKind lever, LeverKind require, LeverKind[]? fill, bool bFill = false);
    //public delegate void LeverDoublePressed(Levers levers, LeverKind lever, bool doublePressed, bool priorityIncreased);
    public delegate void VirtualLeverEvent(VirtualLeverEventArgs args);


    public class Levers {

        //ToneMaker Sounder; 

        //TODO: also track source of press (e.g. via which mouse button)

        // swap left and right (virtual levers)
        public bool SwapLeftRight;

        private List<VirtualLever> Down = new(); // keys pressed and in what order
        private object Lock = new object();

        // ignore if pressed a second time when already down;
        // if true, LeverDoubled event will never fire, and double presses won't change the order of levers
        public bool IgnoreDoublePresses;

        public event VirtualLeverEvent? LeverDown; // Just, like, is a lever down. For pass-thru
        public event VirtualLeverEvent? LeverUp;
        public event VirtualLeverEvent? LeverDoubled;

        public IEnumerable<VirtualLever> GetLeversDown() {
            return Down.AsEnumerable();
        }

        public Levers() {
        }

        private VirtualLeverEventArgs LeverFromVirtual(VirtualLever lever) {
            // creates and partually fills args: levers, vLever, and isSwapped
            // also swaps left and right if that's selected

            var args = new VirtualLeverEventArgs() {
                levers = this,
                vLever = lever
            };

            if (!SwapLeftRight || lever == VirtualLever.None) {
                return args;
            }

            Debug.Assert(lever == VirtualLever.Left || lever == VirtualLever.Right, "New virtual lever state not accounted for");

            args.isSwapped = true;

            if (lever == VirtualLever.Left) {
                args.vLever = VirtualLever.Right;
            } else {
                args.vLever = VirtualLever.Left;
            }
            return args;

        }
            
        public void PushLeverDown(VirtualLever vlever) {
            var args = LeverFromVirtual(vlever);
            var correctLever = args.vLever; // corrected left/right
            args.isDown = true;

            lock (Lock) {
                if (Down.Contains(correctLever)) {
                    if (IgnoreDoublePresses) {
                        return;
                    } 

                    if (Down.LastOrDefault() == correctLever) {
                        // already pressed previously, and was last lever pressed
                        args.doublePressed = true;
                    } else {
                        // already pressed down, but another lever was pressed more recently
                        // in case there's a time this actually matters, fire an event.
                        // Note: if IgnoreDoublePresses, doesn't reach here / move to end 
                        Down.Remove(correctLever);
                        Down.Add(correctLever);
                        args.doublePressed = true;
                        args.priorityIncreased = true;
                    }

                } else {
                    Down.Add(correctLever);
                }

            }

            if (args.doublePressed) {
                LeverDoubled?.Invoke(args);
            } else {
                LeverDown?.Invoke(args);
            }

        }

        public void ReleaseLever(VirtualLever vlever) {
            var args = LeverFromVirtual(vlever);
            args.isDown = false;

            var correctLever = args.vLever; // corrected left/right
            lock (Lock) {
                if (!Down.Contains(correctLever)) {
                    // no double release
                    return;
                }
                Down.Remove(correctLever);
            }

            LeverUp?.Invoke(args);
        }

        //TODO
        //public void ReleaseAll() {
        //    if (!Down.Any()) {
        //        //TODO: amaybe invoke anyway?
        //        return;
        //    }
        //
        //    if (Down.Count == 1) {
        //        var release = Down.FirstOrDefault();
        //        Down.Clear();
        //        LeverUp?.Invoke(this, release, LeverKind.None, null);
        //        return;
        //    }
        //
        //    Down.Clear();
        //    var which = LeverKind.None; // really should be "all"
        //    LeverUp?.Invoke(this, which, LeverKind.None, null);
        //
        //}
    }
}
