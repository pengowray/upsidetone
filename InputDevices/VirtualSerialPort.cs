using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Diagnostics;
using upSidetone.Sound;
using System.Net;

namespace upSidetone.InputDevices {

    public class VirtualSerialPort : IDisposable {
        public string PortName { get; private set; }
        private SerialPort? Port;
        private Levers Levers;

        public bool DtrNormallyHigh = false;
        public bool RtsNormallyHigh = true;

        private bool disposedValue;

        public VirtualSerialPort(string portname, Levers levers) {
            PortName = portname;
            Levers = levers;
        }

        public void Enable() {
            try {
                Port = new SerialPort(PortName);
                Port.Handshake = Handshake.None;
                Port.Open();
                //UpdatePiano("");
                Debug.WriteLine($"Virtual Port {Port.PortName} open: {Port.IsOpen}. handshake:{Port.Handshake}");

                Levers.LeverDown += Levers_LeverDown;
                Levers.LeverUp += Levers_LeverUp;

                Port.DtrEnable = false;
                Port.RtsEnable = false;

            } catch (Exception e) {
                // eg: "failed to open port: COM5 / System.UnauthorizedAccessException: 'Access to the path 'COM5' is denied.'"
                Debug.WriteLine($"failed to open port: {Port?.PortName} / {e.GetType()}: '{e.Message}'");
                // eg: "COM5: 'Access to the path 'COM5' is denied.'" (when in use by another application or instance)
                //UpdatePiano($"{Port?.PortName}: '{e.Message}'");
            }
        }

        private void Levers_LeverUp(Levers levers, LeverKind lever, LeverKind require, LeverKind[]? fill, bool bFill = false) {
            // pass through mode
            // TODO: for pass through mode, really should be subscribing to the plain lever events, not as filtered as this

            if (Port == null || !Port.IsOpen)
                return;

            // DSR (6) <-> DTR (4)
            // RTS (7) <-> CTS (8)
            if (lever == LeverKind.Dit || lever == LeverKind.PoliteStraight || lever == LeverKind.Straight) {
                Port.DtrEnable = DtrNormallyHigh;
            } else if (lever == LeverKind.Dah) {
                Port.RtsEnable = RtsNormallyHigh;
            }
        }

        private void Levers_LeverDown(Levers levers, LeverKind lever, LeverKind require, LeverKind[]? fill, bool bFill = false) {
            if (Port == null || !Port.IsOpen)
                return;

            if (lever == LeverKind.Dit || lever == LeverKind.PoliteStraight || lever == LeverKind.Straight) {
                Port.DtrEnable = !DtrNormallyHigh;
            } else if (lever == LeverKind.Dah) {
                Port.RtsEnable = !RtsNormallyHigh;
            }
        }

        public void ResetPins() {
            // after changing config settings, reset pins to normal state
            if (Port == null || !Port.IsOpen)
                return;

            Port.DtrEnable = DtrNormallyHigh;
            Port.RtsEnable = RtsNormallyHigh;
        }

        private void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e) {
            //Debug.WriteLine($"port error: {e.EventType}.");
            //UpdatePianoTwice(e.EventType.ToString());
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
                MainWindow.Me?.VPortPinsPianoUpdate(pianoText);
            } else {
                // "also" is likely an error
                MainWindow.Me?.VPortPinsPianoUpdate(also);
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
