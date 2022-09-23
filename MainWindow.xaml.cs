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

        Sounder Sounder;
        MidiInput MidiInput;
        static private MainWindow Me;
        private bool disposedValue;

        public MainWindow() {
            Me = this;

            Closed += MainWindow_Closed;

            InitializeComponent();
            Sounder = new Sounder();
            Sounder.DeviceInfoDebug();
            Sounder.Enable();

            MidiInput = new MidiInput();
            MidiInput.Enable(Sounder);

            foreach (var device in Sounder.Devices()) {
                AudioOutputSelect.Items.Add(device);

            }

            foreach (var device in MidiInput.DeviceNames()) {
                MidiSelect.Items.Add(device);
            }


        }

        private void MainWindow_Closed(object? sender, EventArgs e) {
            Dispose();
        }


        private void Button_Click(object sender, RoutedEventArgs e) {
            //todo: delete me
            //Sounder.StraightKeyDown();
        }

        private void Button_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                Sounder.StraightKeyDown(1);
            } else if (e.MiddleButton == MouseButtonState.Pressed) {
                Sounder.StraightKeyDown(2);
            } else if (e.RightButton == MouseButtonState.Pressed) {
                Sounder.StraightKeyDown(3);
            }
        }

        private void Button_MouseUp(object sender, MouseButtonEventArgs e) {
            Sounder.StraightKeyUp();
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e) {
            // stop it getting stuck
            Sounder.StraightKeyUp();
        }

        private void MidiSelect_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var selected = MidiSelect.SelectedValue as string;
            Debug(selected);
            var success = MidiInput.SelectDevice(selected);
            Debug("success: " + success);
        }

        public void Log(string text) {
            DebugText.Text = DebugText.Text + "\n" + text;
        }

        public static void Debug(string text) {
            Me.Log(text);
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                    Sounder?.Dispose();
                    MidiInput?.Dispose();

                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer

                // TODO: set large fields to null
                Sounder = null;
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
