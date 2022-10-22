using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Diagnostics;
using upSidetone.Sound;

namespace upSidetone.InputDevices {

    public class PaddleSerialPort : IDisposable {
        string Name;
        private SerialPort Port;
        Levers Levers;
        private bool disposedValue;
        

        public PaddleSerialPort(string name, Levers levers) {
            Name = name;
            Levers = levers;
        }

        public void Enable() {
            try {
                Port = new SerialPort(Name);
                Port.Handshake = Handshake.None;
                //Port = new SerialPort(Name, 300, Parity.None, 8); // commented out: don't need to pretend to set it up for data
                Port.PinChanged += Port_PinChanged;
                Port.ErrorReceived += Port_ErrorReceived;
                Port.Open();
                UpdatePiano("");
                Debug.WriteLine($"Port {Port.PortName} open: {Port.IsOpen}. handshake:{Port.Handshake}");

            } catch (Exception e) {
                // eg: "failed to open port: COM5 / System.UnauthorizedAccessException: 'Access to the path 'COM5' is denied.'"
                Debug.WriteLine($"failed to open port: {Port?.PortName} / {e.GetType()}: '{e.Message}'");
                // eg: "COM5: 'Access to the path 'COM5' is denied.'"
                UpdatePiano($"{Port?.PortName}: '{e.Message}'");
            }
        }

        private void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e) {
            Debug.WriteLine($"port error: {e.EventType}.");
            UpdatePianoTwice(e.EventType.ToString());
        }

        private void Port_PinChanged(object sender, SerialPinChangedEventArgs e) {
            if (e.EventType.HasFlag(SerialPinChange.CtsChanged)) {
                var down = Port?.CtsHolding ?? false;
                if (down) {
                    Levers?.PushLeverDown(VirtualLever.Left);
                } else {
                    Levers?.ReleaseLever(VirtualLever.Left);
                }
            }

            if (e.EventType.HasFlag(SerialPinChange.DsrChanged)) {
                var down = Port?.DsrHolding ?? false;
                if (down) {
                    Levers?.PushLeverDown(VirtualLever.Right);
                } else {
                    Levers?.ReleaseLever(VirtualLever.Right);
                }
            }

            Debug.WriteLine($"pin changed: {e.EventType}. [{string.Join(" ", ActivePins())}]");
            UpdatePianoTwice($"{e.EventType.ToString()}"); // e.g. "COM12: Open CtsChanged"
        }

        int UpdateTickets = 0;
        void UpdatePianoTwice(string also = "") {

            UpdatePiano(also);

            UpdateTickets++;
            int myTicket = UpdateTickets;
            int millisecondsDelay = 2000;
            Task.Run(async () =>
            {
                await Task.Delay(millisecondsDelay);

                if (UpdateTickets == myTicket) { // only run most recent update request
                    UpdatePiano("");
                }
            });
        }

        void UpdatePiano(string also = "") {
            if (Port != null && Port.IsOpen) {
                var pianoText = $"{Port.PortName}: " + string.Join(" ", ActivePins().Append(also));
                MainWindow.Me?.PortPinsPianoUpdate(pianoText);
            } else {
                // "also" is likely an error
                MainWindow.Me?.PortPinsPianoUpdate(also);
            }
        }
        IEnumerable<string> ActivePins() {
            if (Port != null) {
                //if (Port.IsOpen) yield return "Open"; // when open, show the port name (no need to say "Open")
                if (Port.BreakState) yield return "Break";
                if (Port.CDHolding) yield return "CD";
                if (Port.DsrHolding) yield return "DSR";
                if (Port.DtrEnable) yield return "DTR";
                if (Port.RtsEnable) yield return "RTS";
                if (Port.BytesToRead > 0) yield return $"[{Port.BytesToRead} bytes to read]";
            }
        }

        public static IEnumerable<string> GetPortNames() {
            yield return "(none)";
            var myComparer = new NaturalComparer();
            foreach (var name in SerialPort.GetPortNames().OrderBy(s => s, myComparer)) {
                yield return name;
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // dispose managed state (managed objects)
                    Port?.Close();
                    Port?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                Levers = null;
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~PaddleSerialPort()
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


    public class NaturalComparer : IComparer<string> {
        //via: https://stackoverflow.com/a/9989709/
        public int Compare(string x, string y) {
            var regex = new Regex(@"(\d+)");

            // run the regex on both strings
            var xRegexResult = regex.Match(x);
            var yRegexResult = regex.Match(y);

            // check if they are both numbers
            if (xRegexResult.Success && yRegexResult.Success) {
                return int.Parse(xRegexResult.Groups[1].Value).CompareTo(int.Parse(yRegexResult.Groups[1].Value));
            }

            // otherwise return as string comparison
            return x.CompareTo(y);
        }
    }
}
