using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using upSidetone;
using upSidetone.Sound;
using NAudio.Midi;
using System.Windows.Forms;

namespace upSidetone.MidiMorse {

    public class MidiInput : IDisposable {

        // https://github.com/naudio/NAudio/blob/master/Docs/MidiInAndOut.md

        public Levers? Levers;
        bool Enabled = true; //TODO: put all notes up when disabled

        MidiIn? Midi;
        private bool disposedValue;
        Dictionary<int, string> DownNotes = new(); // notenumber -> description

        const int NON_DEVICE_LABELS = 1; // "none" and "all"
        const string NONE_LABEL = "(none)";
        const string ALL_LABEL = "(any)"; //TODO


        public IEnumerable<string> DeviceNames() {

            yield return NONE_LABEL; // "(none)"
            //yield return ALL_LABEL; // "(any)" // TODO

            for (int device = 0; device < MidiIn.NumberOfDevices; device++) {
                yield return MidiIn.DeviceInfo(device).ProductName;
            }
        }

        public bool SelectDevice(string selectedDevice) {
            // returns true on success

            if (string.IsNullOrEmpty(selectedDevice) || selectedDevice == NONE_LABEL) {
                SelectDevice(-1);
                return false;
            }

            if (selectedDevice == ALL_LABEL) {
                SelectDevice(-2); // TODO
                return false;
            }

            //int? index = DeviceNames()..Select((n,i) => new Tuple(n, i)).Where((n, i) => n == selectedDevice).Select((n, i) => i).FirstOrDefault();
            var devices = DeviceNames().ToArray();
            for (int i = 0; i < devices.Length; i++) {
                if (devices[i] == selectedDevice) {
                    return SelectDevice(i - NON_DEVICE_LABELS);
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

            if (selectedDeviceIndex == -1) // NONE_LABEL ("none")
                return false;

            Midi = new MidiIn(selectedDeviceIndex);
            Midi.MessageReceived += MidiIn_MessageReceived;
            Midi.ErrorReceived += MidiIn_ErrorReceived; ;
            Midi.Start();
            MainWindow.DebugOut($"Midi started " + selectedDeviceIndex);
            return true;
        }

        private VirtualLever NoteToLever(int notenum) {
            // TODO: let user define
            // notes go left, right, left, right...
            bool left = ((notenum % 2) == 0);
            var lever = (left ? VirtualLever.Left : VirtualLever.Right);
            return lever;

        }

        private void MidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e) {
            //MainWindow.Debug(e?.ToString()); 
            if (e?.MidiEvent?.CommandCode == MidiCommandCode.NoteOn) {

                //Levers?.StraightKeyDown();

                var info = e.MidiEvent as NoteOnEvent;
                if (info != null) {
                    var notenum = info.NoteNumber;
                    var name = info.NoteName;
                    DownNotes[notenum] = name;

                    //TODO: use an event (have main window listen to update events)
                    Levers?.PushLeverDown(NoteToLever(notenum));

                    MainWindow.Me.RefreshPianoDisplay();
                }

            } else if (e?.MidiEvent?.CommandCode == MidiCommandCode.NoteOff) {
                var info = e.MidiEvent as NoteEvent;
                if (info != null) {
                    var notenum = info.NoteNumber;
                    DownNotes.Remove(notenum);

                    Levers?.ReleaseLever(NoteToLever(notenum));

                    //TODO: use an event (have main window listen to update events)
                    MainWindow.Me.RefreshPianoDisplay();
                }
            }
        }

        public string? GetDownNotes() {
            return string.Join(" ", DownNotes.OrderBy(n => n.Key).Select(n => n.Value));
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

                //Note: MidiInput doesn't manage Levers and doesn't dispose of it
                Levers = null; // let something else dispose it
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
