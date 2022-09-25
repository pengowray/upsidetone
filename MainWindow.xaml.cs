using IPrompt;
using MorseKeyer.Sound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VirtualMorseKeyer.KeybIn;
using VirtualMorseKeyer.MidiMorse;

namespace MorseKeyer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable {

        int Latency = 90;

        AudioOut AudioOut;
        Sounder Sounder;
        MidiInput MidiInput;
        KeyboardInputWPF KeyboardInput;
        static private MainWindow Me;
        private bool disposedValue;



        public MainWindow() {
            Me = this;

            Closed += MainWindow_Closed;

            InitializeComponent();
            MidiInput = new MidiInput();
            MidiInput.Enable(Sounder); // sounder will be null but that's ok

            KeyboardInput = new KeyboardInputWPF(); //todo: Enable()

            //GetOrCreateSounder(); // create default?

            foreach (var device in AudioOut.Devices()) {
                AudioOutputSelect.Items.Add(device);

            }

            foreach (var device in MidiInput.DeviceNames()) {
                MidiSelect.Items.Add(device);
            }

            AudioOutputSelect.SelectedIndex = 0;
            MidiSelect.SelectedIndex = 0;
        }

        private Sounder GetOrCreateSounder() {
            if (Sounder == null) {
                string? selected = AudioOutputSelect.SelectedValue as string; // ok if null

                AudioOut = new AudioOut();
                AudioOut.Enable(selected, Latency);
                Sounder = new Sounder(AudioOut);
                Sounder.Enable();

                MidiInput?.SetSounder(Sounder);
                KeyboardInput?.SetSounder(Sounder);

                DeviceInfoText.Text = AudioOut?.GetReport() ?? "";

                //AudioOut.DeviceInfoDebug();
            }
            return Sounder;
        }

        private void MidiSelect_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var selected = MidiSelect.SelectedValue as string;
            Debug(selected);
            var success = MidiInput.SelectDevice(selected);
            Debug("success: " + success);
        }

        void ReloadAudioDevice() {
            var selected = AudioOutputSelect.SelectedValue as string;
            Debug("changing audio to: " + selected);

            AudioOut?.Dispose();
            Sounder?.Dispose();
            AudioOut = null;
            Sounder = null;
            GetOrCreateSounder(); // create a new sounder immediately so Midi can talk to it

            //TODO: make a sounder wrapper/indirection, for midi etc to keep pressing buttons on even when audio device changed?
            //...Or just let AudioOut change on sounder
        }

        private void AudioOutputSelect_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            ReloadAudioDevice();
        }


        private void Button_Click(object sender, RoutedEventArgs e) {
            //todo: delete me
            //Sounder.StraightKeyDown();
        }



        private void Button_MouseDown(object sender, MouseButtonEventArgs e) {
            var sounder = GetOrCreateSounder();
            if (e.LeftButton == MouseButtonState.Pressed) {
                Sounder?.StraightKeyDown(1);
            } else if (e.MiddleButton == MouseButtonState.Pressed) {
                Sounder?.StraightKeyDown(2);
            } else if (e.RightButton == MouseButtonState.Pressed) {
                Sounder?.StraightKeyDown(3);
            }
        }

        private void Button_MouseUp(object sender, MouseButtonEventArgs e) {
            Sounder?.StraightKeyUp();
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e) {
            // stop it getting stuck
            Sounder?.StraightKeyUp();
        }

        public void Log(string text) {
            DebugText.Text = DebugText.Text + "\n" + text;
        }

        public static void Debug(string text) {
            Me.Log(text);
        }

        private void MainWindow_Closed(object? sender, EventArgs e) {
            Dispose();
        }


        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // dispose managed state (managed objects)
                    Sounder?.Dispose();
                    MidiInput?.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                AudioOut?.Dispose(); // really needs to be disposed (in case might hold onto ASIO)

                // set large fields to null
                Sounder = null;
                AudioOut = null;
                MidiInput = null;
                Me = null;

                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MainWindow()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void AudioOutOptions_Button_Click(object sender, RoutedEventArgs e) {
            // latency dialog

            // https://github.com/ramer/IPrompt

            //TODO: use my own dialog, and add preferred bitrate
            
            string result = IInputBox.Show("Desired Latency (ms):\n-1 for default.\nIgnored for ASIO drivers.", defaultResponse: Latency.ToString());
            if (result != null && int.TryParse(result, out int val)) {
                if (val >= -1 && val < 10_000) { // todo: max value? 10s probably too much
                    Latency = val;
                    ReloadAudioDevice();

                } else if (val <= -1) {
                    Latency = -1;
                    ReloadAudioDevice();
                }
            }
        }

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e) {
            KeyboardInput?.KeyEvent(e);
        }

        private void Grid_PreviewKeyUp(object sender, KeyEventArgs e) {
            KeyboardInput?.KeyEvent(e);
        }

        private void Grid_LostFocus(object sender, RoutedEventArgs e) {
            Sounder?.StraightKeyUp();

        }
    }
}
