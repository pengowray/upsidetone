using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Windows;

namespace upSidetone.InputDevices {
    public class SerialPortInfo {
        string[] SerialPortNames;

        // serial port names, but not fully populated
        // "COM5" -> "com0com - serial port emulator (COM5)" -- but remove " (COM5)"
        Dictionary<string, string> Captions;

        // more serial port names
        // less reliable names as Win32_PnPEntity does not include exact serial port name
        // so, e.g. "(COM3)" is taken from the caption
        // in some cases it might include info about virtual serial ports
        // e.g. Caption:"Electronic Team Virtual Serial Port (COM10->COM11)"
        Dictionary<string, string> PNPCaptions; 

        public IEnumerable<string> GetPortNames() {
            yield return "(none)";
            if (SerialPortNames == null) {
                var myComparer = new NaturalComparer();
                SerialPortNames = SerialPort.GetPortNames().OrderBy(s => s, myComparer).ToArray();
            }

            foreach (var name in SerialPortNames) {
                yield return name;
            }
        }

        /// <summary>
        /// If ScanForCaptions() has been called, gives names with captions, including "(none)"
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetPortNamesWithCaptions() {
            //yield return "(none)";

            foreach (var device in GetPortNames()) {
                if (device == "(none)") {
                    yield return device;
                    continue;
                }
                    
                string cap = GetCaption(device);
                string display = device;
                if (!string.IsNullOrEmpty(cap)) {
                    display = device + " — " + (cap ?? "");
                }
                yield return display;
            }
        }

        public string? GetPortNameFromCaptioned(string? caption) {
            if (caption == null) {
                return null;
            }

            if (caption.Contains(' ')) {
                return caption.Substring(0, caption.IndexOf(' ')).Trim();
            }

            if (caption == "(none)") {
                return null;
            }
            
            return caption.Trim();
        }


        public string GetCaption(string portname) {
            var name = portname.ToUpperInvariant();
            if (PNPCaptions != null) {
                if (PNPCaptions.TryGetValue(name, out string caption)) {
                    return caption;
                }
            }

            if (Captions != null) {
                if (Captions.TryGetValue(name, out string caption)) {
                    return caption;
                }
            }

            return null;
        }

        public void ScanForCaptions() {
            PopulateCaptions();
            PopulatePNPCaptions();
        }

        public void PopulateCaptions() {

            var objSearcher = new ManagementObjectSearcher(
                "SELECT DeviceID, Caption FROM Win32_SerialPort");

            Captions = new();

            var objCollection = objSearcher.Get();
            foreach (var d in objCollection) {
                string? portName = d.GetPropertyValue("DeviceID")?.ToString();
                string? caption = d.GetPropertyValue("Caption")?.ToString();

                if (portName == null || caption == null) continue;

                //caption = RemoveFinalBrackets(caption) ?? caption; // "com0com - serial port emulator (COM6)" =>  "com0com - serial port emulator"
                caption = TrimFinalPortName(caption, $"({portName})"); // "com0com - serial port emulator (COM6)" =>  "com0com - serial port emulator"
                caption = TrimFinalPortName(caption, portName); // "com0com - serial port emulator CNCB0" -> "com0com - serial port emulator"

                //TODO: check removed bracket text == device ID?

                Captions[portName.ToUpperInvariant()] = caption;
            }
        }

        public void PopulatePNPCaptions() {

            // In Win32_PnPEntity, the captions don't actually match up with the ports, because there's no port names.
            // You just have to guess/hope that "Electronic Team Virtual Serial Port (COM10->COM11)" means it's on COM10:
            // On Microsoft's focums they pass the blame onto the device manufacturers for it being like this,
            // https://web.archive.org/web/20221026043306/https://social.technet.microsoft.com/Forums/windows/en-US/82a8a4bd-6db3-4fe7-9e5e-915498eb6ba0/how-to-getwmiobject-win32pnpentity-where-name-like-8220usb-serial-port8221-by-model
            // "Those properties are only available to WMI if the vendor supplies the correct software.  There is a utility called DEVCON which can ready vendor tags on a device."
            // "Most things are available and PS is adding more all of the time.  The hardware vendors are still not up to date on the Common Information Model"
            // I don't think they understood the question.

            var objSearcher = new ManagementObjectSearcher(
                   "SELECT DeviceID, Caption, Name, Manufacturer FROM Win32_PnPEntity WHERE ClassGuid=\"{4d36e978-e325-11ce-bfc1-08002be10318}\"");

            PNPCaptions = new();

            var objCollection = objSearcher.Get();
            foreach (var d in objCollection) {
                string? deviceId = d.GetPropertyValue("DeviceID")?.ToString();
                string? caption = d.GetPropertyValue("Caption")?.ToString();
                string? name = d.GetPropertyValue("Name")?.ToString();
                string? manu = d.GetPropertyValue("Manufacturer")?.ToString();

                // example deviceId: "DeviceID:FTDIBUS\VID_0403+PID_6001+6&35844985&0&3\0000"
                // exmaple iddeviceId "DeviceID:COM0COM\PORT\CNCA1"

                // Name and Caption are usually the same so use caption, and fallback to using name
                var cap = caption;
                if (string.IsNullOrWhiteSpace(cap)) cap = name;
                if (string.IsNullOrWhiteSpace(cap)) continue;

                string? bracketText = GetBracketText(cap);
                if (string.IsNullOrWhiteSpace(bracketText)) continue;

                string portName = bracketText;
                if (bracketText.Contains("->")) {
                    // e.g. "(COM10->COM11)" 
                    // don't remove bracketed text from name, and use first part as port name

                    int pos = bracketText.IndexOf('-');
                    if (pos <= 1) continue; // too short and no chance we have "COMx"; something's wrong; abort
                    portName = bracketText[..pos]; // == bracketText.Substring(0, pos);
                    if (string.IsNullOrWhiteSpace(portName)) continue;
                    cap = cap.Replace("->", "→"); // make_pretty
                } else {
                    cap = RemoveFinalBrackets(cap);
                }

                cap = TrimFinalPortName(cap, portName);

                if (!string.IsNullOrWhiteSpace(manu) && !cap.Contains(manu) && !cap.StartsWith("com0com")) {
                    // include manufacturer if not already in name
                    // also skip superfluous manufacturer name for "com0com" (sorry, Vyacheslav)
                    if (manu.StartsWith('(')) {
                        // e.g. "Communications Port (Standard port types)"
                        cap = $"{cap} {manu}";
                    } else {
                        // e.g. "USB Serial Port — FTDI"
                        cap = $"{cap} — {manu}";
                    }
                }

                if (string.IsNullOrWhiteSpace(cap)) continue; // failed
                if (string.IsNullOrWhiteSpace(portName)) continue; // failed

                //TODO: check removed bracket text == device ID?

                PNPCaptions[portName.ToUpperInvariant()] = cap;
            }
        }

        private string TrimFinalPortName(string cap, string portName) {
            if (cap == null || portName == null || portName.Length <= 0)
                return cap;

            if (cap.EndsWith(portName, StringComparison.InvariantCultureIgnoreCase)) {
                return cap.Substring(0, cap.Length - portName.Length).TrimEnd();
            }

            return cap;
        }

        static string RemoveFinalBrackets(string text) {
            // https://stackoverflow.com/a/28556384
            // "Communications Port (COM1)" -> "Communications Port"
            // "Electronic Team Virtual Serial Port (COM10->COM11)" -> "Electronic Team Virtual Serial Port"
            return Regex.Replace(text, @"\([^()]*\)(?!.*\([^()]*\))", "").TrimEnd();
        }

        static string? GetBracketText(string text) {
            //todo: could just use above regex capture group

            if (text == null) return null;

            int ket = text.LastIndexOf(')');
            if (ket == -1) return null;
            int bra = text.LastIndexOf('(', ket);
            if (bra == -1) return null;
            var ret = text.Substring(bra + 1, ket - bra - 1);
            //Debug.WriteLine($"test '{text}' -> '{ret}'");
            return ret;
        }


        public void DeviceInfoDebug() {
            
            var objSearcher = new ManagementObjectSearcher(
                   "SELECT * FROM Win32_SerialPort");

            var objCollection = objSearcher.Get();
            foreach (var d in objCollection) {
                Debug.WriteLine($"=====SerialPort {d}===="); // example: \\PCNAME\root\cimv2:Win32_SerialPort.DeviceID="COM1"
                foreach (var p in d.Properties) {
                    Debug.WriteLine($"{p.Name}:{p.Value}");
                }
            }
        }

        public static void PnPEntityDebugInfo() {

            var objSearcher = new ManagementObjectSearcher(
                   "SELECT * FROM Win32_PnPEntity WHERE ClassGuid=\"{4d36e978-e325-11ce-bfc1-08002be10318}\"");

            var objCollection = objSearcher.Get();
            foreach (var d in objCollection) {
                Debug.WriteLine($"=====PnPEntity {d}====");
                foreach (var p in d.Properties) {
                    Debug.WriteLine($"{p.Name}:{p.Value}");
                }
            }
        }

    }


    public class NaturalComparer : IComparer<string> {
        //via: https://stackoverflow.com/a/9989709/
        public int Compare(string x, string y) {
            var regex = new Regex(@"(\d+)");

            // run the regex on both strings
            var xRegexResult = regex.Match(x);
            var yRegexResult = regex.Match(y);

            // check if they are both numbers
            if (xRegexResult.Success && yRegexResult.Success) {
                return int.Parse(xRegexResult.Groups[1].Value).CompareTo(int.Parse(yRegexResult.Groups[1].Value));
            }

            // otherwise return as string comparison
            return x.CompareTo(y);
        }
    }

}
