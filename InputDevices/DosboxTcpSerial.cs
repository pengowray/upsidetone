using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using upSidetone.Sound;

namespace upSidetone.InputDevices {

    // experimental support for direct connection to DosBOX comm ports 

    // dosbox config:
    // #serial1=directserial realport:com5
    // serial1=nullmodem server:127.0.0.1 port:5673  // unneeded: sock:0 transparent:0 nonlocal:1 
    // or in dosbox:
    // serial1 nullmodem server:localhost port:5673

    public class DosboxTcpSerial : IDisposable {

        //static readonly CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        bool cancel = false;

        TcpListener Server;
        TcpClient client;
        NetworkStream ns;

        public Levers Levers;
        public int maxClients = 10;
        public ushort Port = 5673; 

        Task Task;
        Thread Thread;

        bool Xrts;
        bool Xdtr;

        private bool disposedValue;


        public void Stop() {
            cancel = true;

            try {
                ns?.Close();
                client?.Close();
                Server?.Stop();
            } catch(Exception) { }
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

            Server = new TcpListener(IPAddress.Any, Port);

            Server.Start();  // this will start the server

            while (true && !cancel)   //we wait for a connection
            {
                try {
                    client = Server.AcceptTcpClient();  //if a connection exists, the server will accept it

                    ns = client.GetStream();

                    //byte[] hello = new byte[100];
                    //hello = Encoding.Default.GetBytes("hello world");
                    //ns.Write(hello, 0, hello.Length);
                    while (client.Connected && !cancel) {
                        byte[] msg = new byte[1024];
                        int count = ns.Read(msg, 0, msg.Length);

                        ProcessPacket(msg, count);
                    }
                } catch (Exception e) {
                    if (cancel) {
                        return;
                    }
                    Debug.WriteLine(e.Message);
                    Thread.Sleep(2000);
                }
            }


        }


        private bool escaped = false; // last value was escape symbol
        private void ProcessPacket(byte[] buffer, int count) {

            foreach (byte b in buffer.Take(count)) {
                if (escaped) {

                    if (b == 0xFF) {
                        // literal 0xFF in stream
                        escaped = false;
                        continue; 
                    }

                    bool xrts = (b & 0x01) > 0; 
                    bool xdtr = (b & 0x02) > 0;
                    bool lcr  = (b & 0x04) > 0; // Line Control Break (LCR_BREAK_MASK) // CTRL+C?

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
                    Stop();
                    Server?.Stop();
                    client?.Dispose();
                    ns?.Dispose();
                    Task?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
                Server = null;
                Task = null;
                Thread = null;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~TcpSerial()
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
