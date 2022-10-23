using upSidetone.Sound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace upSidetone.InputDevices {
    internal class KeyboardInputWPF {

        // keyboard handling for a Windows (WPF) app

        public Levers Levers;

        public KeyboardInputWPF() {
        }

        public void KeyEvent(KeyEventArgs e) {

            // todo: down key: straight key only (not paddle dit/dah)

            if (e.IsDown) {
                if (e.Key == Key.LeftCtrl || e.Key == Key.OemOpenBrackets || e.Key == Key.Z || e.Key == Key.Left) {
                    e.Handled = true;
                    Levers?.PushLeverDown(VirtualLever.Left);
                } else if (e.Key == Key.RightCtrl || e.Key == Key.OemCloseBrackets || e.Key == Key.X || e.Key == Key.Right) {
                    e.Handled = true;
                    Levers?.PushLeverDown(VirtualLever.Right);
                }
            } else if (e.IsUp) {
                if (e.Key == Key.LeftCtrl || e.Key == Key.OemOpenBrackets || e.Key == Key.Z || e.Key == Key.Left) {
                    e.Handled = true;
                    Levers?.ReleaseLever(VirtualLever.Left);
                } else if (e.Key == Key.RightCtrl || e.Key == Key.OemCloseBrackets || e.Key == Key.X || e.Key == Key.Right) {
                    e.Handled = true;
                    Levers?.ReleaseLever(VirtualLever.Right);
                }
            }

        }

    }
}
