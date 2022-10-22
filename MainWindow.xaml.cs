using IPrompt;
using upSidetone.Sound;
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

using upSidetone.InputDevices;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;

namespace upSidetone {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable {

        static public MainWindow Me { get; private set; } // Singleton (Lazy hack mostly for debugging)

        int Latency = 90;

        AudioOut AudioOut;
        ToneMaker SoundGen;
        Levers Levers;
        MidiInput MidiInput;
        KeyboardInputWPF KeyboardInput;
        MorseMouses MorseMouses;
        PaddleSerialPort Port;

        private bool disposedValue;

        public MainWindow() {
            Me = this;
            
            Debug.WriteLine("hello.");


            Closed += MainWindow_Closed;

            InitializeComponent();

            Levers = new Levers();
            Levers.Mode = KeyerMode.Ultimatic; //KeyerMode.IambicA;

            MidiInput = new MidiInput();
            MidiInput.Levers = Levers;
            //MidiInput.Enabled = true;

            KeyboardInput = new KeyboardInputWPF(); //todo: Enable()
            KeyboardInput.Levers = Levers;

            MorseMouses = new MorseMouses();
            MorseMouses.Levers = Levers;
            MorseMouses.StartPolling();

            //GetOrCreateSounder(); // create default?

            foreach (var device in AudioOut.Devices()) {
                AudioOutputSelect.Items.Add(device);

            }

            foreach (var device in MidiInput.DeviceNames()) {
                MidiSelect.Items.Add(device);
            }

            AudioOutputSelect.SelectedIndex = 0;
            MidiSelect.SelectedIndex = 0;

            KeyerModeSelect.Items.Add("Straight key");
            KeyerModeSelect.Items.Add("Iambic A");
            KeyerModeSelect.Items.Add("Iambic B");
            KeyerModeSelect.Items.Add("Ultimatic");
            KeyerModeSelect.Items.Add("Isopoda"); // like a bug but actually not
            KeyerModeSelect.SelectedIndex = 3; // Ultimatic

            Volume.Text = "50";
            Frequency.Text = "550";
            WPM.Text = "18";
            Flipped.IsChecked = false;

            SoundGen?.SetGain(0.5);

            foreach (var device in PaddleSerialPort.GetPortNames()) {
                SerialPorts.Items.Add(device);
            }
            SerialPorts.SelectedIndex = 0;

            SerialPortSetup.Items.Add("CTS + DSR");
            SerialPortSetup.SelectedIndex = 0;

            Debug.WriteLine("...hello.");

        }

        private ToneMaker GetOrCreateSounder() {
            if (SoundGen == null) {
                string? selected = AudioOutputSelect.SelectedValue as string; // ok if null

                AudioOut?.Dispose();
                AudioOut = new AudioOut();
                AudioOut.Enable(selected, Latency);

                SoundGen?.StopListeningToLevers(Levers); // remove old
                SoundGen?.Dispose();
                SoundGen = new ToneMaker(AudioOut);
                SoundGen.Enable();
                SoundGen.ListenToLevers(Levers);

                //MidiInput?.SetLevers(Sounder);
                //KeyboardInput?.SetSounder(Sounder);
                //MorseMouses?.SetSounder(Sounder);


                DeviceInfoText.Text = AudioOut?.GetReport() ?? "";
                ////(commented out): Offer help? This is too much to squeeze in here
                //if (AudioOut == null || AudioOut.OutDevice == null) {
                //    DeviceInfoText.Text = "WDM (aka WASAPI) offers low latency. MME has the strongest compatibility. Some audio devices have very low latency ASIO drivers.";
                //}

                if (AudioOut?.IsAsio() ?? false) {
                    AsioOutputOptionsButton.IsEnabled = true;
                } else {
                    AsioOutputOptionsButton.IsEnabled = false;
                }
                //AudioOut.DeviceInfoDebug();
            }
            return SoundGen;
        }

        private void MidiSelect_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var selected = MidiSelect.SelectedValue as string;
            DebugOut(selected);
            var success = MidiInput.SelectDevice(selected);
            DebugOut("success: " + success);
        }

        void ReloadAudioDevice() {
            var selected = AudioOutputSelect.SelectedValue as string;
            DebugOut("changing audio to: " + selected);

            AudioOut?.Dispose();
            SoundGen?.Dispose();
            AudioOut = null;
            SoundGen = null;
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
            /*
            var sounder = GetOrCreateSounder();
            if (e.LeftButton == MouseButtonState.Pressed) {
                //Sounder?.StraightKeyDown(1);
                Levers?.PushLeverDown(VirtualLever.Left);
            } else {
                Levers?.PushLeverDown(VirtualLever.Right);
            }
            */
        }

        private void Button_MouseUp(object sender, MouseButtonEventArgs e) {
            // commented out:
            // already using MorseMouses.ListenToAll
            // MouseButtonState is ambiguous when multiple buttons are in use (would need to track them)

            /*
            if (e.LeftButton == MouseButtonState.Released) {
                Levers?.ReleaseLever(VirtualLever.Left);
            }

            if (e.RightButton == MouseButtonState.Released) {
                Levers?.ReleaseLever(VirtualLever.Right);
            }
            */
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e) {
            // stop it getting stuck
            //Sounder?.StraightKeyUp();
            if (MorseMouses != null) MorseMouses.ListenToAll = false;
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e) {
            if (MorseMouses != null) MorseMouses.ListenToAll = true;
        }

        public void Log(string text) {
            // make sure we're on our own thread
            //this.Dispatcher.Invoke(() => {
            //    if (piano != null) piano.Text = text;
            //    DebugText.Text = DebugText.Text + "\n" + text;
            //});

            System.Diagnostics.Debug.WriteLine(text);
        }

        public static void WriteLine(string text) {
            // writes a line for mouse debugging
            if (Me != null && Me.MousePianoText != null) {
                //Me.Log("mouse: " + text, Me.MousePianoText);
                Me.Dispatcher.Invoke(() => {
                    if (Me != null && Me.MousePianoText != null) { // double check
                        Me.MousePianoText.Text = text;
                    }
                });

            }
        }

        public static void DebugOut(string text) {
            Me?.Log(text);
        }

        private void MainWindow_Closed(object? sender, EventArgs e) {
            if (MorseMouses != null) MorseMouses.ListenToAll = true;
            Dispose();
        }


        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // dispose managed state (managed objects)
                    SoundGen?.Dispose();
                    MidiInput?.Dispose();
                    MorseMouses?.Dispose();
                    //Levers?.Dispose(); // TODO
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                AudioOut?.Dispose(); // really needs to be disposed (in case might hold onto ASIO)

                // set large fields to null
                SoundGen = null;
                AudioOut = null;
                MidiInput = null;
                Me = null;
                Levers = null;

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
            // https://www.c-sharpcorner.com/article/dialogs-in-wpf-mvvm/

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
            //TODO: only release for the key button
            Levers?.ReleaseLever(VirtualLever.Left);
            Levers?.ReleaseLever(VirtualLever.Right);

        }

        private void AsioOutputOptionsButton_Click(object sender, RoutedEventArgs e) {
            AudioOut?.LaunchAsioControlPanel();
        }

        public void RefreshPianoDisplay() {
            // run on main thread (if called from midi's thread)
            this.Dispatcher.Invoke(() => {
                MidiPianoText.Text = MidiInput?.GetDownNotes() ?? "";
            });
        }

        internal void SetMouseNames(IEnumerable<string> mice, int selectedIndex = 0) {
            this.Dispatcher.Invoke(() => {
                foreach (var mouse in mice) {
                    MouseSelect.Items.Add(mouse);
                }
                MouseSelect.SelectedIndex = selectedIndex;
            });
        }

        private void MouseSelect_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (MorseMouses != null) {
                MorseMouses.ChosenMouse = MouseSelect.SelectedIndex;
            }
        }

        private void KeyerModeSelect_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Levers.ReleaseAll();

            if (KeyerModeSelect.SelectedIndex == 0) {
                Levers.Mode = KeyerMode.StraightKey;
            } else if (KeyerModeSelect.SelectedIndex == 1) {
                Levers.Mode = KeyerMode.IambicA;
            } else if (KeyerModeSelect.SelectedIndex == 2) {
                Levers.Mode = KeyerMode.IambicB;
            } else if (KeyerModeSelect.SelectedIndex == 3) {
                Levers.Mode = KeyerMode.Ultimatic;
            } else if (KeyerModeSelect.SelectedIndex == 4) {
                Levers.Mode = KeyerMode.Isopoda;
            }
        }

        private void Volume_TextChanged(object sender, TextChangedEventArgs e) {
            if (Volume != null && double.TryParse(Volume.Text, out double result)) {
                double vol = result / 100.0;
                if (vol >= 0 && vol <= 1) {
                    // "Setting volume not supported on DirectSoundOut, adjust the volume on your WaveProvider instead"
                    //if (AudioOut?.OutDevice != null) AudioOut.OutDevice.Volume = vol;
                    var success = SoundGen?.SetGain(vol) ?? false;
                }
            }
        }

        private void Frequency_TextChanged(object sender, TextChangedEventArgs e) {
            if (Frequency != null && double.TryParse(Frequency.Text, out double result)) {
                var success = SoundGen?.SetFreq(result) ?? false;
            }

        }
        private void WPM_TextChanged(object sender, TextChangedEventArgs e) {
            if (WPM != null && double.TryParse(WPM.Text, out double result)) {
                var success = SoundGen?.SetWPM(result) ?? false;
            }
            
        }

        private void Flipped_Checked(object sender, RoutedEventArgs e) {
            bool? flipped = Flipped.IsChecked;
            if (Levers == null) return;
            if (flipped.HasValue && flipped.Value) {
                Levers.SwapLeftRight = true;
            } else {
                Levers.SwapLeftRight = false;
            }
        }

        private void MouseListRefresh_Click(object sender, RoutedEventArgs e) {

        }

        private void SerialPorts_SelectionChanged(object sender, SelectionChangedEventArgs arg) {
            Port?.Dispose();
            var selected = SerialPorts.SelectedValue as string;
            if (selected == null || selected == "(none)") {
                Port?.Dispose();
                PortPinsPianoUpdate("");
                return;
            }

            try {
                Debug.WriteLine("Port connection attempt...");

                Port = new PaddleSerialPort(selected, Levers);
                Port.Enable();
                Debug.WriteLine("Port connected: " + selected);
                arg.Handled = true;
            } catch (Exception e) {
                Debug.WriteLine("Port connect failed: " + selected + " " + e.GetType() + ": " + e.Message);
            }

        }

        public void PortPinsPianoUpdate(string text) {
            if (SerialPortPianoText == null) return;

            //if (InvokeRequired) 
            this.Dispatcher.Invoke(() => {
                if (SerialPortPianoText != null) SerialPortPianoText.Text = text;
            });
        }
    }
}
