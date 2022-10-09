using upSidetone.Sound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace upSidetone.MouseAndKeyboard {
    internal class KeyboardInputWPF {

        // keyboard handling for a Windows (WPF) app

        ToneMaker Sounder;

        public KeyboardInputWPF() {
        }

        public void SetSounder(ToneMaker sounder) {
            //Note: MidiInput doesn't manage Sounder and doesn't dispose of it
            Sounder = sounder;
        }

        public void KeyEvent(KeyEventArgs e) {

            // todo: down key: straight key only (not paddle dit/dah)

            if (e.IsDown) {
                if (e.Key == Key.LeftCtrl || e.Key == Key.OemOpenBrackets || e.Key == Key.Z || e.Key == Key.Left) {
                    Sounder.StraightKeyDown(1);
                } else if (e.Key == Key.RightCtrl || e.Key == Key.OemCloseBrackets || e.Key == Key.X || e.Key == Key.Right) {
                    Sounder.StraightKeyDown(2);
                }
            } else if (e.IsUp) {
                if (e.Key == Key.LeftCtrl || e.Key == Key.OemOpenBrackets || e.Key == Key.Z || e.Key == Key.Left) {
                    Sounder.StraightKeyUp();
                } else if (e.Key == Key.RightCtrl || e.Key == Key.OemCloseBrackets || e.Key == Key.X || e.Key == Key.Right) {
                    Sounder.StraightKeyUp(); 
                }
            }

        }

    }
}
