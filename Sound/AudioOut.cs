using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Mixer;
using NAudio.CoreAudioApi;
using System.Windows.Forms;
using System.Management;
using System.Runtime;
using System.Diagnostics;
using System.Xml.Linq;
//using NWaves.Audio;

namespace MorseKeyer.Sound
{
    public class AudioOut : IDisposable {

        // creates and holds onto the audio device (IWavePlayer) and Mixer

        static readonly int[] FREQS = { 44100, 48000, 88200, 96000, 176400, 192000 };
        const string DEFAULT_AUDIO = "(default)";
        const string PRE_WAVE = "WAVE: ";
        const string PRE_DS = "DS: ";
        const string PRE_WASAPI = "WASAPI: ";
        const string PRE_ASIO = "ASIO: ";

        private int Latency = 30;

        public IWavePlayer? OutDevice; // DirectSoundOut or AsioOut
        public WaveFormat? Format;
        public MixingSampleProvider? Mixer;

        public int PrefSampleRate { get; private set; }
        private string DeviceName;

        private bool disposedValue;

        public AudioOut() {
        }

        public void Enable(string deviceName = null, int latency = -1, int prefSampleRate = 48000) {

            DeviceName = deviceName;

            if (latency <= 0) {
                Latency = 90;
            } else {
                Latency = latency;
            }

            PrefSampleRate = prefSampleRate;
            //int defaultRate = 48000;
            //int sampleRate = defaultRate;


            CreateDevice();
            InitDeviceAndMixer();
        }
        private void CreateDevice() {
            //int defaultRate = 44100; 

            //examples:
            //DUO-CAPTURE EX
            //Voicemeeter AUX Virtual ASIO
            //Voicemeeter Insert Virtual ASIO
            //Voicemeeter Potato Insert Virtual ASIO
            //Voicemeeter VAIO3 Virtual ASIO  // ok?
            //Voicemeeter Virtual ASIO

            string deviceName = DeviceName;

            if (string.IsNullOrWhiteSpace(deviceName) || deviceName == DEFAULT_AUDIO) {
                try {
                    OutDevice = new DirectSoundOut(Latency);
                    MainWindow.Debug($"{DEFAULT_AUDIO} Found.");

                    return;
                } catch (Exception e) {
                    MainWindow.Debug("Failed to set audio to default");
                    return;
                }
            } else if (deviceName.StartsWith(PRE_WAVE)) {
                string name = deviceName.Substring(PRE_WAVE.Length).Trim();
                for (int n = -1; n < WaveOut.DeviceCount; n++) {
                    var caps = WaveOut.GetCapabilities(n);
                    //TODO: check caps.ProductGuid too
                    if (caps.ProductName == name) { // case 
                        //TODO: not sure if this is how to initiate WaveOut
                        var outDev = new WaveOut();
                        outDev.DeviceNumber = n;
                        outDev.DesiredLatency = Latency;
                        OutDevice = outDev;
                        MainWindow.Debug($"{PRE_WAVE}Found: {name}");
                        return;
                    }
                }

            } else if (deviceName.StartsWith(PRE_DS)) {
                string name = deviceName.Substring(PRE_DS.Length);
                var parts = name.Split(" | ");
                if (parts == null || parts.Length <= 0) {
                    MainWindow.Debug($"{PRE_DS}Not found (name or guid missing)");
                    return;
                }
                // ModuleName | Description | Guid
                string moduleName = parts[0];
                string guid = (parts.Length >= 2) ? parts[2].Trim().ToLowerInvariant() : moduleName; // if missing "|" separators, try the whole device name as a guid

                foreach (var item in DirectSoundOut.Devices) {
                    if (item.Guid.ToString().Trim().ToLowerInvariant() == guid) {
                        OutDevice = new DirectSoundOut(item.Guid);
                        MainWindow.Debug($"{PRE_DS}Found: {name}");
                        return;
                    }
                }

                // guid not found, now check moduleName
                foreach (var item in DirectSoundOut.Devices) {
                    if (item.ModuleName == moduleName) {
                        OutDevice = new DirectSoundOut(item.Guid);
                        return;
                    }
                }
            } else if (deviceName.StartsWith(PRE_WASAPI)) {
                string name = deviceName.Substring(PRE_WASAPI.Length);
                MainWindow.Debug($"{PRE_ASIO} not yet implemented NYI: {name}");
                return;

            } else if (deviceName.StartsWith(PRE_ASIO)) {
                //var name = "Voicemeeter AUX Virtual ASIO"; // test
                string name = deviceName.Substring(PRE_ASIO.Length);
                if (AsioOut.GetDriverNames().Any(d => d == name)) {
                    try {
                        OutDevice = new AsioOut(name);
                        MainWindow.Debug($"{PRE_ASIO} found: {name}");
                        MainWindow.Debug($"asio set ({name}; device:{OutDevice}; format:{OutDevice?.OutputWaveFormat?.ToString() ?? "null"}): " + OutDevice?.ToString());
                        return;
                    } catch (Exception e) {
                        MainWindow.Debug($"{PRE_ASIO} error ({e?.GetType()}): {e?.Message}");
                    }
                }
            }

            MainWindow.Debug($"Error. Unrecognized: {deviceName}");

        }
        private void InitDeviceAndMixer() {

            //var test = "DUO-CAPTURE EX"; // works for ASIO, and now can unload
            //OutDevice = new DirectSoundOut(Latency);

            int defaultChannels = 2; // 1 works but will be left only

            //note: OutDevice.OutputWaveFormat is null until after Init(); but then it's too late to get its default (especially for asio)

            //int sampleRate = OutDevice?.OutputWaveFormat?.SampleRate ?? PrefSampleRate; // null reference even with all the "?" ??? Doesn't work any way.
            int sampleRate = PrefSampleRate;
            //if (sampleRate == 0) sampleRate = defaultRate;

            //int channels = OutDevice?.OutputWaveFormat?.Channels ?? defaultChannels; // gets a null reference even with the "?"'s. Doesn't work any way.
            //if (channels == 0) channels = defaultChannels;
            int channels = defaultChannels;

            //TODO: try preferred sampleRate first
            foreach (var freq in FREQS) {
                try {
                    //Format = OutDevice?.OutputWaveFormat ?? WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels); // still gets an uncaught null reference exception
                    Format = WaveFormat.CreateIeeeFloatWaveFormat(freq, channels);
                    Mixer = new MixingSampleProvider(Format);
                    Mixer.ReadFully = true;
                    OutDevice?.Init(Mixer);
                    OutDevice?.Play();
                    MainWindow.Debug($"Format found ({freq}Hz): {Format?.ToString()}");
                    return;

                } catch (Exception e) {
                    if (e is System.InvalidOperationException && e.Message.StartsWith("Can not found a device")) {
                        // TODO: does this get localized?
                        // When "DUO-CAPTURE EX" is busy because VoiceMeeter is hogging it:
                        // (System.InvalidOperationException): "Can not found a device. Please connect the device."
                        string err1 = $"driverCreateException ({e?.GetType()}): " + e?.Message?.ToString();
                        MainWindow.Debug(err1);
                        return;
                    }

                    Mixer = null;
                    OutDevice?.Dispose();
                    CreateDevice(); // recreate so we can try again

                    // (System.InvalidOperationException): "Already initialised this instance of AsioOut - dispose and create a new one"
                    // uh oh

                    // When SampleRate set to 44100 instead of 48000 on "Voicemeeter AUX Virtual ASIO"
                    // (NAudio.Wave.Asio.AsioException)
                    // Message == "Error code [ASE_NoClock] while calling ASIO method <setSampleRate>, "

                    // When device not connected
                    // (System.InvalidOperationException): Cannot open Focusrite USB ASIO. (Error code: 0x54f)

                    // (System.ArgumentException): "SampleRate is not supported"
                    // when DUO-CAPTURE EX set to 44100 instead of 44000

                    string err = $"driverCreateException ({e?.GetType()}): " + e?.Message?.ToString();
                    //Console.WriteLine(err);
                    MainWindow.Debug(err);
                }

                // No info in OutDevice until it's Init'd
                //MainWindow.Debug($"1. Format sampleRate/channels (Device: {OutDevice?.OutputWaveFormat?.ToString()} | {OutDevice?.OutputWaveFormat?.SampleRate}/{OutDevice?.OutputWaveFormat?.Channels}): {Format} => {Format.SampleRate}/{Format.Channels}");
            }
        }



        public static IEnumerable<string> Devices() {
            // https://github.com/naudio/NAudio/blob/master/Docs/EnumerateOutputDevices.md

            yield return DEFAULT_AUDIO;

            for (int n = -1; n < WaveOut.DeviceCount; n++) {
                var caps = WaveOut.GetCapabilities(n);
                //Console.WriteLine($"{n}: {caps.ProductName}");
                yield return $"{PRE_WAVE}{caps.ProductName}";
            }

            foreach (var dev in DirectSoundOut.Devices) {
                yield return $"{PRE_DS}{dev.ModuleName} | {dev.Description} | {dev.Guid}";
            }

            var enumerator = new MMDeviceEnumerator();
            foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)) {
                yield return $"{PRE_WASAPI}{wasapi.DataFlow} {wasapi.FriendlyName} {wasapi.DeviceFriendlyName} {wasapi.State}";
            }

            foreach (var asio in AsioOut.GetDriverNames()) {
                // includes disconnected devices, e.g. 
                // DUO-CAPTURE EX
                // Focusrite USB ASIO
                // Realtek ASIO
                MainWindow.Debug(asio);
                yield return $"{PRE_ASIO}{asio}";
            }

            //TODO: VBAN (voicemeeter UDP)
        }

        public void DeviceInfoDebug() {
            // Using Windows Management Objects to get hold of details of the sound devices installed.
            // This doesn't map specifically to any of the NAudio output device types, but can be a source of useful information

            var objSearcher = new ManagementObjectSearcher(
                   "SELECT * FROM Win32_SoundDevice");

            var objCollection = objSearcher.Get();
            foreach (var d in objCollection) {
                MainWindow.Debug($"=====DEVICE {d}====");
                foreach (var p in d.Properties) {
                    MainWindow.Debug($"{p.Name}:{p.Value}");
                }
            }
            MainWindow.Debug("=========");

        }

        public void SetDevice(string deviceName) {
            //TODO
            if (deviceName.StartsWith("asio") && deviceName.Contains("DUO-CAPTURE")) {
                // testing DUO-CAPTURE EX

            }
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)


                    //if (OutDevice is AsioOut asioOut) { }
                    //if (OutDevice is DirectSoundOut directSoundOut) { }

                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                OutDevice?.Dispose();

                // set large fields to null
                OutDevice = null;
                Mixer = null;

                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Sounder()
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
