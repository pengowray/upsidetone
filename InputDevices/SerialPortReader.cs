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

        //TODO: merge with VirtualSerialPort: allow same port for read and write
        //TODO: reduce duplicated code with VirtualSerialPort 

        public string PortName { get; private set; }
        private SerialPort? Port;
        private Levers Levers;


        //default setup:
        //send → receive
        //RTS → CTS (7→8) — Dits
        //DTR → DSR (4→6) — Dashes


        public bool IsSecondPort;

        // play as input or listen ("receive")
        public bool PlayAsInputOn = true;
        public bool ListenOn = false; // play on a separate oscillator
        public bool CtsNormallyHigh = false; // set automatically during Enable()
        public bool DsrNormallyHigh = true;  // set automatically during Enable()


        // send (pass thru) -- DTR / RTS
        public bool PassThruOn = false; // pass on lever presses
        public bool DtrNormallyHigh = false; 
        public bool RtsNormallyHigh = true;

        private bool disposedValue;

        public SerialPortReader(string name, Levers levers) {
            PortName = name;
            Levers = levers;
        }

        public void Enable() {
            try {
                UpdatePiano("");

                //Port = new SerialPort(PortName, 300, Parity.None, 8); // commented out: don't need to pretend we're setting it up for data
                Port = new SerialPort(PortName);
                Port.Handshake = Handshake.None;

                Port.PinChanged += Port_PinChanged;
                Port.ErrorReceived += Port_ErrorReceived;
                Levers.LeverDown += Levers_LeverDown;
                Levers.LeverUp += Levers_LeverUp;

                Port.Open();

                // automatic detection of normal state (this could be better
                // TODO: ought to have manual settings option too
                // TODO: pick different pins, or just one pin

                // receive
                CtsNormallyHigh = Port?.CtsHolding ?? CtsNormallyHigh;
                DsrNormallyHigh = Port?.DsrHolding ?? DsrNormallyHigh;

                //send
                Port.DtrEnable = DtrNormallyHigh;
                Port.RtsEnable = RtsNormallyHigh;


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


        private void Levers_LeverUp(Levers levers, LeverKind lever, LeverKind require, LeverKind[]? fill, bool bFill = false) {
            // pass through mode
            // TODO: for pass through mode, really should be subscribing to the plain lever events, not as filtered as this

            if (!PassThruOn)
                return;

            if (Port == null || !Port.IsOpen)
                return;

            // DSR (6) <-> DTR (4)
            // RTS (7) <-> CTS (8)
            if (lever == LeverKind.Dit || lever == LeverKind.Straight || lever == LeverKind.Oscillate) {
                Port.RtsEnable = RtsNormallyHigh;
            } else if (lever == LeverKind.Dah) {
                Port.DtrEnable = DtrNormallyHigh;
            }
        }

        private void Levers_LeverDown(Levers levers, LeverKind lever, LeverKind require, LeverKind[]? fill, bool bFill = false) {
            if (!PassThruOn)
                return;

            if (Port == null || !Port.IsOpen)
                return;

            if (lever == LeverKind.Dit || lever == LeverKind.Straight || lever == LeverKind.Oscillate) {
                Port.RtsEnable = !RtsNormallyHigh;
            } else if (lever == LeverKind.Dah) {
                Port.DtrEnable = !DtrNormallyHigh;
            }

            UpdatePianoTwice($"");
        }


        private void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e) {
            Debug.WriteLine($"port error: {e.EventType}.");
            UpdatePianoTwice(e.EventType.ToString());
        }

        private void Port_PinChanged(object sender, SerialPinChangedEventArgs e) {
            if (ListenOn) {
                // TODO: play on separate oscillator
            }

            if (PlayAsInputOn) {
                if (e.EventType.HasFlag(SerialPinChange.CtsChanged)) {
                    var down = (Port?.CtsHolding ?? CtsNormallyHigh) != CtsNormallyHigh;
                    if (down) {
                        Levers?.PushLeverDown(VirtualLever.Left);
                    } else {
                        Levers?.ReleaseLever(VirtualLever.Left);
                    }
                }

                if (e.EventType.HasFlag(SerialPinChange.DsrChanged)) { // if (e.EventType.HasFlag(SerialPinChange.DsrChanged)) {
                    var down = (Port?.DsrHolding ?? DsrNormallyHigh) != DsrNormallyHigh;
                    if (down) {
                        Levers?.PushLeverDown(VirtualLever.Right);
                    } else {
                        Levers?.ReleaseLever(VirtualLever.Right);
                    }
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
                if (IsSecondPort) {
                    MainWindow.Me?.VPortPinsPianoUpdate(also);
                } else {
                    MainWindow.Me?.PortPinsPianoUpdate(also);
                }
            } else {
                // "also" is likely an error
                if (IsSecondPort) {
                    MainWindow.Me?.VPortPinsPianoUpdate(also);
                } else {
                    MainWindow.Me?.PortPinsPianoUpdate(also);
                }
            }
        }


        public void ResetPins() {
            // after changing config settings, reset pins to normal state
            if (Port == null || !Port.IsOpen)
                return;

            Port.RtsEnable = RtsNormallyHigh;
            Port.DtrEnable = DtrNormallyHigh;

            UpdatePianoTwice($"");
        }


        IEnumerable<string> ActivePins() {
            if (Port != null) {
                //if (Port.IsOpen) yield return "Open"; // when open, show the port name (no need to say "Open")
                if (Port.BreakState) yield return "Break";
                if (Port.CDHolding) yield return "CD"; // pin 1 (aka DCD)
                // pin 2: RxD 
                // pin 3: TxD
                if (Port.DtrEnable) yield return "DTR"; // pin 4 (tx to DSR)
                // pin 5: GND
                if (Port.DsrHolding) yield return "DSR"; // pin 6 (rx from DTR)
                if (Port.RtsEnable) yield return "RTS"; // pin 7 (tx to cts)
                if (Port.CtsHolding) yield return "CTS"; // pin 8
                // pin 9: RI ← Ring Indicator
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
