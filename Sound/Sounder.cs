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
//using NWaves.Audio;

namespace MorseKeyer.Sound
{
    public class Sounder : IDisposable {

        private int Latency = 30;
        float Volume = 0.2f;

        IWavePlayer? OutDevice; // DirectSoundOut or AsioOut
        WaveFormat? Format;
        SignalGenerator? Sine;
        //List<AdsrSampleProvider> AdsrList = new();
        AdsrSampleProvider? Adsr;
        MixingSampleProvider? Mixer;

        private bool disposedValue;



        public Sounder(int latency = 50) {
            Latency = latency;
        }

        public void Enable() {
            // start running.
            try {

                int sampleRate = 44100;

                //DUO-CAPTURE EX
                //Voicemeeter AUX Virtual ASIO
                //Voicemeeter Insert Virtual ASIO
                //Voicemeeter Potato Insert Virtual ASIO
                //Voicemeeter VAIO3 Virtual ASIO  // ok?
                //Voicemeeter Virtual ASIO

                //var test = "DUO-CAPTURE EX"; // works for ASIO but fails to unload
                var test = "Voicemeeter AUX Virtual ASIO";
                if (AsioOut.GetDriverNames().Any(d => d == test)) {
                    OutDevice = new AsioOut(test);
                    MainWindow.Debug($"asio set ({test}; device:{OutDevice}; format:{OutDevice?.OutputWaveFormat?.ToString() ?? "null"}): " + OutDevice.ToString());
                } else {
                    OutDevice = new DirectSoundOut(Latency);
                }

                int defaultRate = 48000; //  44100;
                int defaultChannels = 2; // 1 works but will be left only

                //note: OutDevice.OutputWaveFormat is null until after Init(); but then it's too late to get its default (especially for asio)

                sampleRate = OutDevice?.OutputWaveFormat?.SampleRate ?? defaultRate;
                if (sampleRate == 0) sampleRate = defaultRate;
                int channels = OutDevice?.OutputWaveFormat?.Channels ?? defaultChannels;
                if (channels == 0) channels = defaultChannels;

                Format = OutDevice?.OutputWaveFormat 
                    ?? WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
                //Mixer = new MixingSampleProvider(44100, 2);
                //Mixer = new MixingSampleProvider(Format);

                MainWindow.Debug($"1. Format sampleRate/channels (Device: {OutDevice?.OutputWaveFormat?.ToString()} | {OutDevice?.OutputWaveFormat?.SampleRate}/{OutDevice?.OutputWaveFormat?.Channels}): {Format} => {Format.SampleRate}/{Format.Channels}");

                Sine = new SignalGenerator(Format.SampleRate, channel: 1) {
                    Gain = Volume,
                    Frequency = 650,
                    Type = SignalGeneratorType.Sin
                };

                Mixer = new MixingSampleProvider(Format);
                Mixer.ReadFully = true;
                OutDevice?.Init(Mixer);
                OutDevice?.Play();

                MainWindow.Debug($"2. Format sampleRate/channels (Device: {OutDevice?.OutputWaveFormat?.ToString()} | {OutDevice?.OutputWaveFormat?.SampleRate}/{OutDevice?.OutputWaveFormat?.Channels}): {Format} => {Format.SampleRate}/{Format.Channels}");

            } catch (Exception e) {
                // When "DUO-CAPTURE EX" is busy because VoiceMeeter is hogging it:
                // (System.InvalidOperationException)
                // Message == "Can not found a device. Please connect the device."

                // When SampleRate set to 44100 instead of 48000 on "Voicemeeter AUX Virtual ASIO"
                // (NAudio.Wave.Asio.AsioException)
                // Message == "Error code [ASE_NoClock] while calling ASIO method <setSampleRate>, "

                string err = $"driverCreateException ({e?.GetType()}): " + e?.Message?.ToString();
                Console.WriteLine(err);
                MainWindow.Debug(err);
                return;
            }

        }

        public IEnumerable<string> Devices() {
            // https://github.com/naudio/NAudio/blob/master/Docs/EnumerateOutputDevices.md
            for (int n = -1; n < WaveOut.DeviceCount; n++) {
                var caps = WaveOut.GetCapabilities(n);
                //Console.WriteLine($"{n}: {caps.ProductName}");
                yield return $"wave: {caps.ProductName}";
            }

            foreach (var dev in DirectSoundOut.Devices) {
                yield return $"DirectSound: {dev.Guid} {dev.ModuleName} {dev.Description}";
            }

            var enumerator = new MMDeviceEnumerator();
            foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)) {
                yield return $"wasapi: {wasapi.DataFlow} {wasapi.FriendlyName} {wasapi.DeviceFriendlyName} {wasapi.State}";
            }

            foreach (var asio in AsioOut.GetDriverNames()) {
                // includes disconnected devices, e.g. 
                // DUO-CAPTURE EX
                // Focusrite USB ASIO
                // Realtek ASIO
                MainWindow.Debug(asio);
                yield return $"asio: {asio}";
            }
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

        public void DitKeyDown() {
            //mainOutput
        }

        public void StraightKeyDown() {
            // https://github.com/naudio/NAudio/blob/master/Docs/PlaySineWave.md
            // https://csharp.hotexamples.com/examples/NAudio.Wave/DirectSoundOut/Play/php-directsoundout-play-method-examples.html


            if (Adsr == null) {

                //https://stackoverflow.com/a/23357560/443019

                if (Sine != null) {
                    Adsr = new AdsrSampleProvider(Sine.ToMono(1, 0)) {
                        AttackSeconds = 0.015f,
                        ReleaseSeconds = 0.015f
                    };
                }

                if (Mixer != null) {
                    if (Mixer.WaveFormat.Channels == 2) {
                        Mixer?.AddMixerInput(Adsr.ToStereo(1, 1));
                    } else {
                        Mixer?.AddMixerInput(Adsr);
                    }

                } else {
                    MainWindow.Debug("Mixer null");
                }
                
            }

            
        }

        public void StraightKeyUp() {
            Adsr?.Stop();
            Adsr = null;
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)

                    Adsr?.Stop();
                    Mixer?.RemoveAllMixerInputs(); // probably not needed here
                    OutDevice?.Stop();
                    OutDevice?.Dispose();

                    var asioOut = OutDevice as AsioOut;
                    if (asioOut != null) {
                        //asioOut?.Dispose();
                    }

                    var directSoundOut = OutDevice as DirectSoundOut;
                    if (directSoundOut != null) {
                        //directSoundOut?.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer

                // TODO: set large fields to null
                OutDevice = null;
                Mixer = null;
                Adsr = null;
                Sine = null;

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
