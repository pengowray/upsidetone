using UpSidetone.Sound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace VirtualMorseKeyer.KeybIn {
    internal class KeyboardInputWPF {

        // keyboard handling for a Windows (WPF) app

        Sounder Sounder;

        public KeyboardInputWPF() {
        }

        public void SetSounder(Sounder sounder) {
            //Note: MidiInput doesn't manage Sounder and doesn't dispose of it
            Sounder = sounder;
        }

        public void KeyEvent(KeyEventArgs e) {
            if (e.IsDown) {
                if (e.Key == Key.LeftCtrl || e.Key == Key.OemOpenBrackets || e.Key == Key.Z) {
                    Sounder.StraightKeyDown(1);
                } else if (e.Key == Key.RightCtrl || e.Key == Key.OemCloseBrackets || e.Key == Key.X) {
                    Sounder.StraightKeyDown(2);
                }
            } else if (e.IsUp) {
                if (e.Key == Key.LeftCtrl || e.Key == Key.OemOpenBrackets || e.Key == Key.Z) {
                    Sounder.StraightKeyUp();
                } else if (e.Key == Key.RightCtrl || e.Key == Key.OemCloseBrackets || e.Key == Key.X) {
                    Sounder.StraightKeyUp(); 
                }
            }

        }

    }
}
