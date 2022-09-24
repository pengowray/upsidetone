using MorseKeyer;
using MorseKeyer.Sound;
using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VirtualMorseKeyer.MidiMorse {

    public class MidiInput : IDisposable {

        // https://github.com/naudio/NAudio/blob/master/Docs/MidiInAndOut.md

        Sounder? Sounder;
        MidiIn? Midi;
        private bool disposedValue;

        public void Enable(Sounder sounder = null) {
            //Sounder = sounder ?? new Sounder();
            Sounder = sounder;
            //Sounder.Enable();
        }

        public IEnumerable<string> DeviceNames() {
            for (int device = 0; device < MidiIn.NumberOfDevices; device++) {
                yield return MidiIn.DeviceInfo(device).ProductName;
            }
        }

        public void SetSounder(Sounder sounder) {
            //Note: MidiInput doesn't manage Sounder and doesn't dispose of it
            Sounder = sounder;
        }

        public bool SelectDevice(string selectedDevice) {
            // returns true on success

            if (string.IsNullOrEmpty(selectedDevice)) {
                SelectDevice(-1);
                return false;
            }

            //int? index = DeviceNames()..Select((n,i) => new Tuple(n, i)).Where((n, i) => n == selectedDevice).Select((n, i) => i).FirstOrDefault();
            var devices = DeviceNames().ToArray();
            for (int i = 0; i < devices.Length; i++) {
                if (devices[i] == selectedDevice) {
                    return SelectDevice(i);
                }
            }

            return false;
        }

        public bool SelectDevice(int selectedDeviceIndex = -1) {
            // returns true on success

            if (Midi != null) {
                //remove old device
                Midi.MessageReceived -= MidiIn_MessageReceived;
                Midi.ErrorReceived -= MidiIn_ErrorReceived;
                Midi.Stop();
                Midi.Close();
            }

            if (selectedDeviceIndex < 0)
                return false;

            Midi = new MidiIn(selectedDeviceIndex);
            Midi.MessageReceived += MidiIn_MessageReceived;
            Midi.ErrorReceived += MidiIn_ErrorReceived; ;
            Midi.Start();
            MainWindow.Debug($"Midi started " + selectedDeviceIndex);
            return true;
        }

        private void MidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e) {
            //MainWindow.Debug(e?.ToString()); // can't debug here, wrong thread
            if (e?.MidiEvent?.CommandCode == MidiCommandCode.NoteOn) {
                Sounder?.StraightKeyDown();
            } else if (e?.MidiEvent?.CommandCode == MidiCommandCode.NoteOff) {
                Sounder?.StraightKeyUp();
            }
        }

        private void MidiIn_ErrorReceived(object? sender, MidiInMessageEventArgs e) {
            //throw new NotImplementedException();
            //MainWindow.Debug("midi error: " + e?.ToString()); // can't debug here, wrong thread
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null

                //Note: MidiInput doesn't manage Sounder and doesn't dispose of it
                Sounder = null; // let something else dispose it
                Midi?.Dispose();

                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MidiInput()
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
