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

    // attempt to read morse on pins of a serial port.

    public class SerialPortReader : IDisposable {

        //TODO: reduce duplicated code with VirtualSerialPort 

        public string PortName { get; private set; }
        private SerialPort? Port;
        private Levers Levers;

        // set automatically during Enable()
        public bool CtsNormallyHigh = false;
        public bool DsrNormallyHigh = true;

        private bool disposedValue;
        

        public SerialPortReader(string name, Levers levers) {
            PortName = name;
            Levers = levers;
        }

        public void Enable() {
            try {
                UpdatePiano("");

                Port = new SerialPort(PortName);
                Port.Handshake = Handshake.None;
                //Port = new SerialPort(Name, 300, Parity.None, 8); // commented out: don't need to pretend to set it up for data
                Port.PinChanged += Port_PinChanged;
                Port.ErrorReceived += Port_ErrorReceived;
                Port.Open();

                // automatic detection of normal state (this could be better
                // TODO: ought to have manual settings option too
                // TODO: pick different pins, or just one pin
                CtsNormallyHigh = Port?.CtsHolding ?? CtsNormallyHigh;
                DsrNormallyHigh = Port?.DsrHolding ?? DsrNormallyHigh;

                if (Port != null && Port.IsOpen) {
                    UpdatePiano("Connected");
                    string txt = $"{Port?.PortName}: Connected";
                    Debug.WriteLine(txt);
                }


            } catch (Exception e) {

                Debug.WriteLine($"Failed to open port: {Port?.PortName} / {e.GetType()}: '{e.Message}'");
                if (PortName != null && !PortName.StartsWith("COM", StringComparison.OrdinalIgnoreCase)) {
                    // eg: "CNCA0: 'The given port name (CNCA0) does not resolve to a valid serial port. (Parameter 'portName')'"
                    // discussion: https://web.archive.org/web/20221026061825/https://www.pcreview.co.uk/threads/problem-with-system-io-ports-serialport-open.2907727/
                    // see also (note fixed code also does not allow non-COM ports): https://stackoverflow.com/questions/14885288/i-o-exception-error-when-using-serialport-open
                    UpdatePiano($"{Port?.PortName} Error: Microsoft's System.IO.Ports does not support port names which do not start with 'COM'. '{e.Message}'");
                } else {
                    // eg: "COM5: 'Access to the path 'COM5' is denied.'" (when in use by another application or instance)
                    UpdatePiano($"{Port?.PortName} Error: '{e.Message}'");
                }
            }
        }

        private void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e) {
            Debug.WriteLine($"port error: {e.EventType}.");
            UpdatePianoTwice(e.EventType.ToString());
        }

        private void Port_PinChanged(object sender, SerialPinChangedEventArgs e) {
            if (e.EventType.HasFlag(SerialPinChange.CtsChanged)) {
                var down = (Port?.CtsHolding ?? CtsNormallyHigh) != CtsNormallyHigh;
                if (down) {
                    Levers?.PushLeverDown(VirtualLever.Left);
                } else {
                    Levers?.ReleaseLever(VirtualLever.Left);
                }
            }

            if (e.EventType.HasFlag(SerialPinChange.DsrChanged)) {
                var down = (Port?.DsrHolding ?? DsrNormallyHigh) != DsrNormallyHigh;
                if (down) {
                    Levers?.PushLeverDown(VirtualLever.Right);
                } else {
                    Levers?.ReleaseLever(VirtualLever.Right);
                }
            }

            Debug.WriteLine($"Pin changed: {e.EventType}. [{string.Join(" ", ActivePins())}]");
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


}
