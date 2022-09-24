using MorseKeyer.Sound;
using System;
using System.Collections.Generic;
using System.Linq;
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
using VirtualMorseKeyer.MidiMorse;

namespace MorseKeyer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable {

        AudioOut AudioOut;
        Sounder Sounder;
        MidiInput MidiInput;
        static private MainWindow Me;
        private bool disposedValue;

        public MainWindow() {
            Me = this;

            Closed += MainWindow_Closed;

            InitializeComponent();
            MidiInput = new MidiInput();
            MidiInput.Enable(Sounder); // sounder will be null but that's ok

            //GetOrCreateSounder(); // create default?

            foreach (var device in AudioOut.Devices()) {
                AudioOutputSelect.Items.Add(device);

            }

            foreach (var device in MidiInput.DeviceNames()) {
                MidiSelect.Items.Add(device);
            }
        }

        private Sounder GetOrCreateSounder() {
            if (Sounder == null) {
                var selected = AudioOutputSelect.SelectedValue as string;

                AudioOut = new AudioOut();
                AudioOut.Enable(selected); //TODO: latency option
                Sounder = new Sounder(AudioOut);
                Sounder.Enable();
                MidiInput.SetSounder(Sounder);

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

        private void AudioOutputSelect_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var selected = AudioOutputSelect.SelectedValue as string;
            Debug("changing audio to: " + selected);
            AudioOut?.Dispose();
            Sounder?.Dispose();
            AudioOut = null;
            Sounder = null;
            GetOrCreateSounder(); // create a new sounder immediately so Midi can talk to it

            //TODO: make a sounder wrapper for midi etc to keep pressing buttons on even when audio device changed?
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

    }
}
