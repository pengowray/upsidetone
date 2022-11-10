using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Management;
using System.Runtime;
using System.Diagnostics;
using System.Xml.Linq;

using NAudio.Mixer;
using NAudio.CoreAudioApi;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
//using NWaves.Audio;

namespace upSidetone.Sound
{
    public class AudioOut : IDisposable {

        // creates and holds onto the audio device (IWavePlayer) and Mixer

        //static readonly int[] PREF_FREQS = { 44100, 48000, 88200, 96000, 176400, 192000, 352800, 384000, 32000, 22500, 16000, 11025, 8000};

        //test
        static readonly int[] PREF_FREQS = { 8000, 11025, 16000, 22500, 32000, 44100, 48000, 88200, 96000, 176400, 192000, 352800, 384000 };

        static readonly int[] NORMAL_FREQS = { 8000, 11025, 16000, 22500, 32000, 44100, 48000, 88200, 96000, 176400, 192000, 352800, 384000 };

        const string DEFAULT_AUDIO = "(default)";
        const string NONE_LABEL = "(none)";

        // MME (Multimedia Events, aka WinMM [Windows Multimedia APIs])
        // MME was released in 1991.
        // "the most compatible with all audio devices"
        // winmm.dll is used by NAudio = MME
        // We previously called this "WAVE". Renamed MME to match VoiceMeeter. (Also more descriptive)
        const string PRE_WAVE = "MME: ";

        // "DirectSound is basically just a DirectX-related Interface to the Windows Audio Session API (WASAPI) underneath."
        // Apparently called "Direct-X" in VoiceMeeter, maybe in older versions?
        const string PRE_DS = "DS: "; 

        const string PRE_WASAPI = "WASAPI: "; // WASAPI is called WDM in VoiceMeeter, but WDM and WASAPI are separate in JACK2 for Windows; related to KS (Kernel Streaming)
        const string PRE_ASIO = "ASIO: ";

        private int DesiredLatency = 50;
        private int ReportLatency = 0; // Latency used when creating OutDevice; For reporting back to the user.

        public IWavePlayer? OutDevice; // DirectSoundOut or AsioOut
        public WaveFormat? Format;
        public MixingSampleProvider? Mixer;

        public int PrefSampleRate { get; private set; }
        private string? DeviceName;

        private bool disposedValue;

        string ErrorMsg = null; // Error when loading driver

        public AudioOut() {
        }

        public void Enable(string? deviceName = null, int requestedLatency = -1, int prefSampleRate = 48000) {

            DeviceName = deviceName;

            if (requestedLatency <= 0) {
                //Latency = 90; // leave default
                DesiredLatency = -1;

            } else {
                DesiredLatency = requestedLatency;
            }

            PrefSampleRate = prefSampleRate;
            //int defaultRate = 48000;
            //int sampleRate = defaultRate;


            InitDeviceAndMixer();
        }

        public string GetReport() {
            if (ErrorMsg != null) {
                return ErrorMsg;
            }

            string waveFormat = OutDevice?.OutputWaveFormat?.ToString() ?? "";
            return waveFormat + "\n" + GetLatencyReport();
        }

        public string GetLatencyReport() {
            if (OutDevice == null) {
                return "";
            }

            if (OutDevice is DirectSoundOut directSoundOut) {
                // Official name is now "DirectX Audio" but that seems rare.
                if (ReportLatency <= 0) return "";
                return $"DirectSound latency: {ReportLatency}ms" + ApproxSamples();
            }
            if (OutDevice is WasapiOut wasapiOut) {
                if (ReportLatency <= 0) return "";
                return $"WASAPI latency: {ReportLatency}ms" + ApproxSamples();
            }
            if (OutDevice is WaveOut waveOut) {
                if (ReportLatency <= 0) return "";
                return $"MME latency: {waveOut.DesiredLatency}ms{ApproxSamples()}; Buffers: {waveOut.NumberOfBuffers}";
            }
            if (OutDevice is AsioOut asioOut) {
                int sampleRate = asioOut?.OutputWaveFormat?.SampleRate ?? 0;
                if (sampleRate <= 0) return "";
                float latencySeconds = 1.0f * asioOut.PlaybackLatency / sampleRate;
                return $"ASIO latency: {asioOut.PlaybackLatency} samples [{latencySeconds*1000.0f:N2}ms]";
            }

            return "";
        }

        private string ApproxSamples() {
            if (ReportLatency <= 0) {
                return "";
            }

            int sampleRate = OutDevice?.OutputWaveFormat?.SampleRate ?? 0;
            if (sampleRate <= 0) {
                return "";
            }

            int samples = (sampleRate * ReportLatency) / 1000;
            return $" [approx {samples} samples]";
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

            string? deviceName = DeviceName;

            if (string.IsNullOrWhiteSpace(deviceName) || deviceName == DEFAULT_AUDIO) {
                try {
                    if (DesiredLatency > 0) {
                        ReportLatency = DesiredLatency;
                        OutDevice = new DirectSoundOut(DesiredLatency);
                    } else {
                        ReportLatency = 40;
                        OutDevice = new DirectSoundOut();
                    }
                    MainWindow.DebugOut($"{DEFAULT_AUDIO} Found.");
                    return;

                } catch (Exception e) {
                    MainWindow.DebugOut("Failed to set audio to default");
                    return;
                }

            } else if (deviceName == NONE_LABEL) {
                OutDevice?.Dispose();
                OutDevice = null;
                ReportLatency = 0;
                return;

            } else if (deviceName.StartsWith(PRE_WAVE)) {
                string name = deviceName.Substring(PRE_WAVE.Length).Trim();
                for (int n = -1; n < WaveOut.DeviceCount; n++) {
                    var caps = WaveOut.GetCapabilities(n);
                    //TODO: check caps.ProductGuid too
                    //caps.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_2M08) // TODO: check supported formats
                    //caps.SupportsPlaybackRateControl // TODO

                    if (caps.ProductName == name) { // case 
                        //TODO: not sure if this is how to initiate WaveOut
                        var outDev = new WaveOut();
                        outDev.DeviceNumber = n;
                        if (DesiredLatency > 0) {
                            ReportLatency = DesiredLatency;
                            outDev.DesiredLatency = DesiredLatency;
                        } else {
                            ReportLatency = 300;
                        }
                        OutDevice = outDev;
                        MainWindow.DebugOut($"{PRE_WAVE}Found: {name}");
                        return;
                    }
                }

            } else if (deviceName.StartsWith(PRE_DS)) {
                string name = deviceName.Substring(PRE_DS.Length);
                var parts = name.Split(" | ");
                if (parts == null || parts.Length <= 0) {
                    MainWindow.DebugOut($"{PRE_DS}Not found (name or guid missing)");
                    ReportLatency = 0;
                    return;
                }
                // ModuleName | Description | Guid
                string moduleName = parts[0];
                string guid = (parts.Length >= 2) ? parts[1].Trim().ToLowerInvariant() : moduleName; // if missing "|" separators, try the whole device name as a guid

                if (!string.IsNullOrWhiteSpace(guid)) {
                    foreach (var item in DirectSoundOut.Devices) {
                        var itemGuid = item.Guid.ToString().Trim().ToLowerInvariant();
                        if (itemGuid == guid) {
                            if (DesiredLatency <= 0) {
                                ReportLatency = 40; // default for DirectSoundOut
                                OutDevice = new DirectSoundOut(item.Guid);
                            } else {
                                ReportLatency = DesiredLatency;
                                OutDevice = new DirectSoundOut(item.Guid, DesiredLatency);
                            }
                            //MainWindow.Debug($"{PRE_DS}Found: {name}");
                            return;
                        }
                    }
                }

                // guid not found, now check moduleName
                if (!string.IsNullOrWhiteSpace(moduleName)) {
                    foreach (var item in DirectSoundOut.Devices) {
                        if (item.ModuleName == moduleName) {
                            if (DesiredLatency <= 0) {
                                ReportLatency = 40; // default for DirectSoundOut
                                OutDevice = new DirectSoundOut(item.Guid);
                            } else {
                                ReportLatency = DesiredLatency;
                                OutDevice = new DirectSoundOut(item.Guid, DesiredLatency);
                            }
                            return;
                        }
                    }
                }

            } else if (deviceName.StartsWith(PRE_WASAPI) || deviceName.StartsWith("WASAPI: ") || deviceName.StartsWith("WAS: ")) { // previously also accepted "WDM: "
                // Allow "WDM: " (voicemeeter style) or "WASAPI: " (technically more correct)
                //string name = deviceName.Substring(PRE_WASAPI.Length);
                string name = deviceName.Substring(deviceName.IndexOf(':') + 2); // +2 to get past ": ".

                var parts = name.Split(" | ");
                if (parts == null || parts.Length <= 0) {
                    MainWindow.DebugOut($"{PRE_WASAPI}Not found (name missing)");
                    return;
                }
                //FriendlyName} | {wasapi.DeviceFriendlyName
                string? friendlyName = parts[0];
                string? deviceFriendlyName = (parts.Length >= 2) ? parts[1]?.Trim() : friendlyName;

                var enumerator = new MMDeviceEnumerator();
                var wasapi = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                    .FirstOrDefault(d => d.FriendlyName == friendlyName || d.DeviceFriendlyName == deviceFriendlyName);
                if (wasapi != null) {
                    // note: useEventSync: true is the default for "new WasapiOut()"
                    // 200ms is the default latency for "new WasapiOut()"
                    if (DesiredLatency > 0) {
                        ReportLatency = DesiredLatency;
                        OutDevice = new WasapiOut(wasapi, shareMode: AudioClientShareMode.Shared, useEventSync: true, DesiredLatency);
                    } else {
                        int defaultLatency = 150; // actual default is 200 for wasapi
                        ReportLatency = defaultLatency;
                        OutDevice = new WasapiOut(wasapi, shareMode: AudioClientShareMode.Shared, useEventSync: true, defaultLatency);
                    }
                    return;
                }

                MainWindow.DebugOut($"{PRE_WASAPI}Not found: {name}");
                return;

            } else if (deviceName.StartsWith(PRE_ASIO)) {
                //var name = "Voicemeeter AUX Virtual ASIO"; // test
                string name = deviceName.Substring(PRE_ASIO.Length);
                if (AsioOut.GetDriverNames().Any(d => d == name)) {
                    try {
                        // no latency setting
                        var asioOut = new AsioOut(name);
                        ReportLatency = 0; // reported separately for ASIO
                        OutDevice = asioOut;
                        MainWindow.DebugOut($"{PRE_ASIO} found: {name}");
                        //MainWindow.Debug($"asio set ({name}; device:{OutDevice}; format:{OutDevice?.OutputWaveFormat?.ToString() ?? "null"}): " + OutDevice?.ToString());
                        return;
                    } catch (Exception e) {
                        MainWindow.DebugOut($"{PRE_ASIO} error ({e?.GetType()}): {e?.Message}");
                    }
                }
            }

            ReportLatency = 0;
            MainWindow.DebugOut($"Error. Unrecognized: {deviceName}");

        }
        private void InitDeviceAndMixer() {

            //var test = "DUO-CAPTURE EX"; // works for ASIO, and now can unload
            //OutDevice = new DirectSoundOut(Latency);

            int defaultChannels = 2; // 1 works but will be left only

            //note: OutDevice.OutputWaveFormat is null until after Init(); but then it's too late to get its default (especially for asio)

            //int sampleRate = OutDevice?.OutputWaveFormat?.SampleRate ?? PrefSampleRate; // null reference even with all the "?" ??? Doesn't work any way.
            //int sampleRate = PrefSampleRate;
            //if (sampleRate == 0) sampleRate = defaultRate;

            //int channels = OutDevice?.OutputWaveFormat?.Channels ?? defaultChannels; // gets a null reference even with the "?"'s. Doesn't work any way.
            //if (channels == 0) channels = defaultChannels;
            int channels = defaultChannels;

            //TODO: try preferred sampleRate first
            AsioOut? asioOut = OutDevice as AsioOut;

            foreach (var freq in PREF_FREQS) {
                try {
                    if (asioOut != null && !asioOut.IsSampleRateSupported(freq)) {
                        continue;
                    }

                    CreateDevice();
                    //Format = OutDevice?.OutputWaveFormat ?? WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels); // still gets an uncaught null reference exception
                    Format = WaveFormat.CreateIeeeFloatWaveFormat(freq, channels);

                    //TODO: when would you use ALaw or MuLaw?
                    //Format = WaveFormat.CreateALawFormat(freq, channels); 

                    Mixer = new MixingSampleProvider(Format);
                    Mixer.ReadFully = true;
                    OutDevice?.Init(Mixer);
                    OutDevice?.Play();
                    MainWindow.DebugOut($"Format found ({freq}Hz): {Format?.ToString()}");
                    ErrorMsg = null;
                    return;

                } catch (Exception e) {

                    //'AsioException' is inaccessible due to its protection level
                    //if ((e is System.InvalidOperationException || e.GetType().ToString() == "NAudio.Wave.Asio.AsioException") &&
                    // screw this, just use the message
                    if (e.Message.StartsWith("Can not found a device") || e.Message.Contains("ASE_NotPresent") || e.Message.Contains("0x54f)")) {
                        // error isn't with bitrate, so bail out without trying every bitrate

                        // When "DUO-CAPTURE EX" is busy because VoiceMeeter is hogging it:
                        // (System.InvalidOperationException): "Can not found a device. Please connect the device."
                        // TODO: does this get localized?

                        // trying to use Realtek ASIO (because needs headphones/speaker plugged in and set up)
                        // (NAudio.Wave.Asio.AsioException): "Error code [ASE_NotPresent] while calling ASIO method <setSampleRate>, "

                        // error but it happens later or in a different thread
                        // "Focusrite USB ASIO" not connected to PC:
                        // ASIO:  error (System.InvalidOperationException): Cannot open Focusrite USB ASIO. (Error code: 0x54f)
                        // Error. Unrecognized: ASIO: Focusrite USB ASIO

                        string err1 = $"Device busy or not found: ({e?.GetType()}): " + e?.Message?.ToString();
                        MainWindow.DebugOut(err1);
                        ErrorMsg = "Device busy or not found";
                        return;
                    }

                    if (e.Message.Contains("ASE_HWMalfunction")) {
                        // never seen so far
                        string err1 = $"Could not init: ({e?.GetType()}): " + e?.Message?.ToString();
                        MainWindow.DebugOut(err1);
                        ErrorMsg = $"ASIO driver is reporting hardware malfunction (ASE_HWMalfunction): {e?.Message}";
                        return;
                    }

                    Mixer = null;
                    OutDevice?.Dispose();
                    OutDevice = null;

                    // (System.InvalidOperationException): "Already initialised this instance of AsioOut - dispose and create a new one"
                    // uh oh

                    // When SampleRate set to 44100 instead of 48000 on "Voicemeeter AUX Virtual ASIO"
                    // (NAudio.Wave.Asio.AsioException)
                    // Message == "Error code [ASE_NoClock] while calling ASIO method <setSampleRate>, "

                    // When device not connected
                    // (System.InvalidOperationException): Cannot open Focusrite USB ASIO. (Error code: 0x54f)

                    // (System.ArgumentException): "SampleRate is not supported"
                    // when DUO-CAPTURE EX set to 44100 instead of 44000

                    //asio codes
                    //ASE_OK = 0,             // This value will be returned whenever the call succeeded
                    //ASE_SUCCESS = 0x3f4847a0,	// unique success return value for ASIOFuture calls
                    //ASE_NotPresent = -1000, // hardware input or output is not present or available
                    //ASE_HWMalfunction,      // hardware is malfunctioning (can be returned by any ASIO function)
                    //ASE_InvalidParameter,   // input parameter invalid
                    //ASE_InvalidMode,        // hardware is in a bad mode or used in a bad mode
                    //ASE_SPNotAdvancing,     // hardware is not running when sample position is inquired
                    //ASE_NoClock,            // sample clock or rate cannot be determined or is not present
                    //ASE_NoMemory            // not enough memory for completing the request

                    string err2 = $"Could not init: ({e?.GetType()}): " + e?.Message?.ToString();
                    //note: e.Message may contain random text?
                    ErrorMsg = $"Could not init device. ({e?.GetType()})"; // may be nulled again if good parameters found
                    MainWindow.DebugOut(err2);
                }

                // No info in OutDevice until it's Init'd
                //MainWindow.Debug($"1. Format sampleRate/channels (Device: {OutDevice?.OutputWaveFormat?.ToString()} | {OutDevice?.OutputWaveFormat?.SampleRate}/{OutDevice?.OutputWaveFormat?.Channels}): {Format} => {Format.SampleRate}/{Format.Channels}");
            }
        }



        public static IEnumerable<string> Devices() {
            // https://github.com/naudio/NAudio/blob/master/Docs/EnumerateOutputDevices.md

            yield return DEFAULT_AUDIO;
            yield return NONE_LABEL;

            var enumerator = new MMDeviceEnumerator();
            foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)) {
                //TODO: maybe use ID, or cut out GUID part from it
                yield return $"{PRE_WASAPI}{wasapi.FriendlyName} | {wasapi.DeviceFriendlyName}";
            }


            foreach (var dev in DirectSoundOut.Devices) {
                // Example:
                // ModuleName: "{0.0.0.00000000}.{95bc3cac-a45e-4773-b8ce-9dd0bc0eaa40}"
                // Description: "PHL 271S7Q (NVIDIA High Definition Audio)
                // Guid: "95bc3cac-a45e-4773-b8ce-9dd0bc0eaa40"
                yield return $"{PRE_DS}{dev.Description} | {dev.Guid}";
            }

            for (int n = -1; n < WaveOut.DeviceCount; n++) {
                var caps = WaveOut.GetCapabilities(n);
                //Console.WriteLine($"{n}: {caps.ProductName}");
                yield return $"{PRE_WAVE}{caps.ProductName}";
            }

            foreach (var asio in AsioOut.GetDriverNames()) {
                // includes disconnected devices, e.g. 
                // DUO-CAPTURE EX
                // Focusrite USB ASIO
                // Realtek ASIO
                MainWindow.DebugOut(asio);
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
                MainWindow.DebugOut($"=====DEVICE {d}====");
                foreach (var p in d.Properties) {
                    MainWindow.DebugOut($"{p.Name}:{p.Value}");
                }
            }
            MainWindow.DebugOut("=========");

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

        internal bool IsAsio() {
            return OutDevice != null && OutDevice is AsioOut;
        }

        public bool LaunchAsioControlPanel() {
            // returns true on success
            if (OutDevice is AsioOut asioOut) {
                try {
                    //asioOut.DriverResetRequest();
                    asioOut.ShowControlPanel();
                    return true;
                } catch (Exception e) {
                    MainWindow.DebugOut($"Error launching ASIO control panel ({e.GetType()}): '{e.Message}'");
                    //NAudio.Wave.Asio.AsioException: 'Error code [ASE_NotPresent] while calling ASIO method <controlPanel>, '
                    return false;
                }
            }
            return false;
        }
    }
}
