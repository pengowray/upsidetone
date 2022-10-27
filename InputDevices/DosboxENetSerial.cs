using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//using Arrowgene.ENet;
using ENet;

using upSidetone.Sound;

namespace upSidetone.InputDevices {

    // experimental support for direct connection to DosBOX comm ports 

    // dosbox config:
    // serial1=nullmodem server:127.0.0.1 port:5673 sock:1  // unneeded: transparent:0  nonlocal:1
    
    // for com0com: serial1=directserial realport:com5

    // attempt 1: https://github.com/nxrighthere/ENet-CSharp
    // [very vague error] System.InvalidOperationException: Host creation call failed -- so....?

    // alternative 1: https://github.com/StrangeLoopGames/ENet-CSharp -- fork with more descriptive errors
    // "Either the application has not called WSAStartup, or WSAStartup failed." (same code as above); WinSock thing. Fixing will mean no chance of crossplatform

    // alternative 2: https://github.com/sebastian-heinz/Arrowgene.ENet
    // actual rewrite in C#. Promising but feeatures incomplete. Probably good enough.

    // alternative 3: https://github.com/moonshadow565/LENet (1.0.1)
    // Direct port of C ENet networking library to .net/C#.
    // Supports base enet 1.2.x protocol together with modifications done by Riot Games for game League of Legends.
    // possibly just a clone of: https://github.com/LeagueSandbox/ENetSharpLeague (2019 archived, but newer version? 1.1.0?)

    // alternative 4: https://github.com/moien007/ENet.Managed


    public class DosboxENetSerial : IDisposable {

        //static readonly CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        bool cancel = false;

        public Levers Levers;
        Host server;
        public int maxClients = 10;
        public ushort port = 5673; // 7373;

        Task Task;
        Thread Thread;

        bool Xrts;
        bool Xdtr;

        private bool disposedValue;


        public void Cancel() {
            cancel = true;
        }
        public void StartAsThread() {
            try {
                Thread = new Thread(new ThreadStart(() => Init()));
                Thread.Start();

            } catch (Exception e) {
                Debug.WriteLine(e);
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }

        }
        public void StartAsTask() {

            try {
                //Task = Task.Run(() => { Init(); });
                Task = Task.Factory.StartNew(() => Init());
                
            } catch (Exception e) {
                Debug.WriteLine(e);
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
        }

        private void Init() {

            server = new Host();

            Address address = new Address();
            //address.SetIP("0.0.0.0"); // maybe?
            //address.SetIP("127.0.0.1"); // maybe?
            address.Port = port;

            // error
            server.Create();

            Event netEvent;

            while (true) { // while (!Console.KeyAvailable) {
                bool polled = false;

                while (!polled) {

                    if (cancel || (Task?.IsCanceled ?? false)) {
                        server?.Flush();
                        Dispose();
                        return;
                    }

                    if (server.CheckEvents(out netEvent) <= 0) {
                        if (server.Service(15, out netEvent) <= 0)
                            break;

                        polled = true;
                    }

                    switch (netEvent.Type) {
                        case EventType.None:
                            break;

                        case EventType.Connect:
                            Debug.WriteLine("Client connected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                            break;

                        case EventType.Disconnect:
                            Debug.WriteLine("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                            break;

                        case EventType.Timeout:
                            Debug.WriteLine("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                            break;

                        case EventType.Receive:
                            Debug.WriteLine("Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);

                            ProcessPacket(netEvent.Packet);

                            netEvent.Packet.Dispose();
                            break;
                    }
                }
            }


        }

        bool escaped = false; // last value was escape symbol
        private void ProcessPacket(Packet packet) {
            byte[] buffer = new byte[packet.Length];
            packet.CopyTo(buffer);

            foreach (byte b in buffer) {
                if (escaped) {

                    if (b == 0xFF) {
                        // literal 0xFF in stream
                        escaped = false;
                        continue;
                    }

                    bool xrts = (b & 0x01) > 0;
                    bool xdtr = (b & 0x02) > 0;
                    bool lcr = (b & 0x04) > 0; // Line Control Break (LCR_BREAK_MASK) // CTRL+C?

                    if (xrts != Xrts) {
                        Xrts = xrts;
                        if (xrts) {
                            Levers.PushLeverDown(VirtualLever.Left);
                        } else {
                            Levers.ReleaseLever(VirtualLever.Left);
                        }
                    }

                    if (xdtr != Xdtr) {
                        Xdtr = xdtr;
                        if (xdtr) {
                            Levers.PushLeverDown(VirtualLever.Right);
                        } else {
                            Levers.ReleaseLever(VirtualLever.Right);
                        }
                    }

                    escaped = false;
                } else {
                    if (b == 0xFF) {
                        escaped = true;

                    } else {
                        //ignore
                        //escaped = false; // redundant
                    }

                }

            }
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                    Cancel();
                    server?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ENetSerial()
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
