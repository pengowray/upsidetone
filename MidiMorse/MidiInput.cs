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

    public class MidiInput {

        // https://github.com/naudio/NAudio/blob/master/Docs/MidiInAndOut.md

        Sounder Sounder;
        MidiIn? Midi;

           
        public void Enable(Sounder sounder) {
            Sounder = sounder ?? new Sounder();
            //Sounder.Enable();
        }

        public IEnumerable<string> DeviceNames() {
            for (int device = 0; device < MidiIn.NumberOfDevices; device++) {
                yield return MidiIn.DeviceInfo(device).ProductName;
            }
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
            MainWindow.Debug(e?.ToString());
            if (e?.MidiEvent?.CommandCode == MidiCommandCode.NoteOn) {
                Sounder.StraightKeyDown();
            } else if (e?.MidiEvent?.CommandCode == MidiCommandCode.NoteOff) {
                Sounder.StraightKeyUp();
            }
        }

        private void MidiIn_ErrorReceived(object? sender, MidiInMessageEventArgs e) {
            //throw new NotImplementedException();
            MainWindow.Debug("midi error: " + e?.ToString());
        }


    }
}
