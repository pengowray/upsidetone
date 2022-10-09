using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using ManyMouseSharp;
using upSidetone.Sound;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using static ManyMouseSharp.ManyMouse;
//using static System.Console;
using static upSidetone.MainWindow;

// https://github.com/Eideren/ManyMouseSharp/blob/master/ManyMouseTest/Program.cs

namespace upSidetone.MouseAndKeyboard {
    public class MorseMouses : IDisposable {
        const int NON_DEVICE_LABELS = 2; // "none" and "all"
        const string NONE_LABEL = "(none)"; // todo: rename to "(none watched)" or none in background or something
        const int NONE_VALUE = 0;
        const string ALL_LABEL = "(all devices)";
        const int ALL_VALUE = 1;

        Task? polling;
        bool stopPolling = false;
        private bool disposedValue;
        public Sounder? Sounder;

        List<string>? Mouses = null; // created on DoInit()

        public int ChosenMouse = NONE_VALUE;
        public bool ListenToAll = false; // override chosen and use all (e.g. while mousing over test area)

        public void SetSounder(Sounder sounder) {
            //Note: doesn't manage Sounder and doesn't dispose of it
            Sounder = sounder;
        }

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

        private void CalcNames() {
            int mice = AmountOfMiceDetected;
            WriteLine($"{mice} mice on {DriverName}");
            Mouses = new();
            Mouses.Add(NONE_LABEL);
            Mouses.Add(ALL_LABEL);
            for (uint i = 0; i < mice; i++) {
                //TODO: only include a number if there's more than one with the same name
                string name = $"({i}) {DeviceName(i)}"; 
                Mouses.Add(name);
                WriteLine(name);
            }
            var index = ChosenMouse + NON_DEVICE_LABELS;
            MainWindow.Me.SetMouseNames(Mouses.AsEnumerable());
        }

        void EndlessPolling() {
            //Console.CancelKeyPress += OnCancelKeyPress;
            string finalMessage;
            try {

                WriteLine("Starting up ManyMouse");
                int result = Init(); // result = AmountOfMiceDetected
                CalcNames();
                WriteLine($"Starting to poll.");

                while (!stopPolling) {
                    while (PollEvent(out ManyMouseEvent mme) > 0) {
                        if (mme.type == ManyMouseEventType.MANYMOUSE_EVENT_BUTTON) {
                            //WriteLine(mme.ToString());
                            if (ListenToAll || ChosenMouse == ALL_VALUE || ChosenMouse == mme.device + NON_DEVICE_LABELS ) {
                                if (mme.item == 0) {
                                    if (mme.value == 1) {
                                        Sounder?.DitKeyDown();
                                    } else {
                                        Sounder?.DitsKeyUp();
                                    }
                                } else {
                                    if (mme.value == 1) {
                                        Sounder?.StraightKeyDown();
                                    } else {
                                        Sounder?.StraightKeyUp();
                                    }
                                }
                                WriteLine($"({mme.device}) {DeviceName(mme.device)} Button {mme.item}: {(mme.value == 1 ? "Down" : "Up")}");
                                //TODO: paste the text and flag another thread get around to sending it to the UI
                                //Task message = Task.Run(() => 
                                //    Piano = $"({mme.device}) {DeviceName(mme.device)} Button {mme.item}: {(mme.value == 1 ? "Down" : "Up")}";
                                //);

                            }

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
                        ListenToAll = false;
                        ChosenMouse = NONE_VALUE;
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
