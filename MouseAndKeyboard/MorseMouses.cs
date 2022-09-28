using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using ManyMouseSharp;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using static ManyMouseSharp.ManyMouse;
//using static System.Console;
using static UpSidetone.MainWindow;

// https://github.com/Eideren/ManyMouseSharp/blob/master/ManyMouseTest/Program.cs

namespace UpSidetone.MouseAndKeyboard {
    public class MorseMouses : IDisposable {
        Task? polling;
        bool stopPolling = false;
        private bool disposedValue;

        public void StartPolling() {
            if (polling != null && !polling.IsCompleted) {
                WriteLine("Already polling mouse");
                return;
            }
            WriteLine("Starting mouse polling...");
            polling = Task.Run(() => EndlessPolling());
                //.ContinueWith(CleanUp);
            //polling.Start();
        }

        private void CleanUp(Task arg1, object? arg2) {
            polling = null;
            stopPolling = true;
        }


        public void StopPolling() {
            stopPolling = true;
            //polling = null;
        }

        void EndlessPolling() {
            //Console.CancelKeyPress += OnCancelKeyPress;
            string finalMessage;
            try {
                WriteLine("Starting up ManyMouse");
                int result = Init();
                WriteLine($"{AmountOfMiceDetected} mice on {DriverName}");
                List<string> mouses = new();
                mouses.Add("(none)"); // todo: rename to "(none watched)" or something
                for (uint i = 0; i < result; i++) {
                    string name = DeviceName(i);
                    mouses.Add(name);
                    WriteLine($"\tname");
                }
                MainWindow.Me.SetMouseNames(mouses.AsEnumerable());

                WriteLine("Starting to poll.");
                while (!stopPolling) {
                    while (PollEvent(out ManyMouseEvent mme) > 0) {
                        if (mme.type == ManyMouseEventType.MANYMOUSE_EVENT_BUTTON) {
                            //WriteLine(mme.ToString());
                            WriteLine($"[{mme.device + 1}] {DeviceName(mme.device)}: {mme.item}: {(mme.value == 1 ? "Down" : "Up")}");
                        }
                    }
                }

                Quit();
                finalMessage = "Done";
            } catch (Exception e) {
                //ForegroundColor = ConsoleColor.Red;
                WriteLine(e.ToString());
                //ResetColor();
                finalMessage = "Error";
            }
            WriteLine($"{finalMessage}");
            //Read();
        }

        void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs args) {
            StopPolling();
            args.Cancel = true;
            Console.CancelKeyPress -= OnCancelKeyPress;
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // dispose managed state (managed objects)
                    try {
                        Quit();
                    } catch {}
                    //TODO: interrupt Task?
                    stopPolling = true;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MorseMouses()
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
