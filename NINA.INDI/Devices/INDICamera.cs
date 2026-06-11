#region "copyright"

/*
    Copyright © 2025-2026 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.INDI.Enums;
using NINA.INDI.Interfaces;
using NINA.INDI.Protocol;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.INDI.Devices {

    public class INDICamera : INDIDevice, IINDICamera {

        // Completed when the BLOB for the exposure in flight arrives. Replaced on every
        // StartExposure and dropped on abort, so a late BLOB from a previous (e.g. aborted)
        // exposure can neither signal the new exposure nor throw on a double release.
        private volatile TaskCompletionSource<bool> _exposureReady;
        private byte[] _lastBlobData;
        private string _lastBlobFormat;
        private double _temperatureSetPoint = double.NaN;
        private bool _coolerOn;

        public INDICamera(INDIDeviceInfo device) : base(device) {
        }

        protected override string[] GetRequiredConnectionProperties() {
            return ["CCD_INFO"];
        }

        public override void OnBlobPropertyUpdated(INDIBlobProperty p) {
            base.OnBlobPropertyUpdated(p);
            if (p.Name == "CCD1") {
                var blob = p.Blobs.FirstOrDefault();
                Logger.Info($"[{DeviceName}] BLOB received: name={p.Name} format={blob?.Format} size={blob?.Data?.Length}");
                if (blob != null && blob.Data?.Length > 0) {
                    _lastBlobData = blob.Data;
                    _lastBlobFormat = blob.Format;
                    // TrySetResult is idempotent (duplicate BLOB updates can arrive from
                    // parallel dispatch) and the TCS is null when no exposure is in flight.
                    _exposureReady?.TrySetResult(true);
                }
            }
        }

        public override void OnSwitchPropertyUpdated(INDISwitchProperty p) {
            base.OnSwitchPropertyUpdated(p);
            // Sync local cooler state from CCD_COOLER updates — but only when the vector
            // is readable. On write-only drivers (e.g. toupbase) the switch values are
            // definition defaults (COOLER_ON=On), not actual TEC state.
            if (p.Name == "CCD_COOLER" && p.Permission != PropertyPermission.WriteOnly) {
                var on = p.Switches.FirstOrDefault(s => s.Name == "COOLER_ON");
                if (on != null) _coolerOn = on.Value;
            }
        }

        private INDINumber GetNumberElement(string propertyName, string elementName) {
            return GetNumberProperty(propertyName)?.Numbers.FirstOrDefault(n => n.Name == elementName);
        }

        private void TrySetNumber(string propertyName, string elementName, double value) {
            try {
                SetNumberValue(propertyName, elementName, value);
            } catch (ArgumentException ex) {
                Logger.Debug($"[{DeviceName}] Cannot set {propertyName}.{elementName}: {ex.Message}");
            }
        }

        private void TrySetSwitch(string propertyName, string elementName, bool value) {
            try {
                SetSwitchValue(propertyName, elementName, value);
            } catch (ArgumentException ex) {
                Logger.Debug($"[{DeviceName}] Cannot set {propertyName}.{elementName}: {ex.Message}");
            }
        }

        public void SetBinning(short x, short y) {
            try {
                SetNumberValues("CCD_BINNING", ("HOR_BIN", (double)x), ("VER_BIN", (double)y));
            } catch (ArgumentException ex) {
                Logger.Debug($"[{DeviceName}] Cannot set CCD_BINNING: {ex.Message}");
            }
        }

        public void StartExposure(double exposureTime, short binX, short binY, bool enableSubSample, int subSampleX, int subSampleY, int subSampleWidth, int subSampleHeight, int gain, int offset) {
            Logger.Info($"[{DeviceName}] StartExposure called: {exposureTime}s");
            // Invalidate any stale BLOB and arm a fresh completion for this exposure.
            // Order matters: clear the data before swapping the TCS so a late BLOB
            // arriving in between resolves the abandoned TCS, not the new one.
            _lastBlobData = null;
            _exposureReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Apply the requested binning if it differs from the current driver state.
            // Both axes go out in a single CCD_BINNING vector (see SetBinning).
            if (binX > 0 && binY > 0 && (BinX != binX || BinY != binY)) {
                SetBinning(binX, binY);
            }

            // Set gain if supported and explicitly requested
            if (gain >= 0 && GainElement is { } gainElement) {
                TrySetNumber(gainElement.Property, gainElement.Element, gain);
            }

            // Set offset if supported and explicitly requested
            if (offset >= 0 && CanSetOffset) {
                TrySetNumber("CCD_OFFSET", "OFFSET", offset);
            }

            // Set frame region — only send CCD_FRAME when actually sub-sampling AND the
            // region changed. Drivers like toupbase restart the camera engine on any
            // CCD_FRAME update, which can make the following exposure command fail, and
            // a redundant full-frame reset even crashes indi_toupcam_ccd.
            if (enableSubSample && subSampleWidth > 0 && subSampleHeight > 0) {
                var frameChanged =
                    (int)(GetNumberPropertyValue("CCD_FRAME", "X") ?? -1) != subSampleX ||
                    (int)(GetNumberPropertyValue("CCD_FRAME", "Y") ?? -1) != subSampleY ||
                    (int)(GetNumberPropertyValue("CCD_FRAME", "WIDTH") ?? -1) != subSampleWidth ||
                    (int)(GetNumberPropertyValue("CCD_FRAME", "HEIGHT") ?? -1) != subSampleHeight;
                if (frameChanged) {
                    try {
                        SetNumberValues("CCD_FRAME",
                            ("X", (double)subSampleX),
                            ("Y", (double)subSampleY),
                            ("WIDTH", (double)subSampleWidth),
                            ("HEIGHT", (double)subSampleHeight));
                    } catch (ArgumentException ex) {
                        Logger.Debug($"[{DeviceName}] Cannot set CCD_FRAME: {ex.Message}");
                    }
                }
            }

            // Send exposure duration — this triggers the capture
            try {
                SetNumberValue("CCD_EXPOSURE", "CCD_EXPOSURE_VALUE", exposureTime);
                Logger.Info($"[{DeviceName}] Started INDI exposure: {exposureTime}s bin={binX}x{binY}");
            } catch (Exception ex) {
                Logger.Error($"[{DeviceName}] Failed to set CCD_EXPOSURE: {ex.Message}");
            }
        }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            var ready = _exposureReady;
            if (ready == null) { return; }
            await ready.Task.WaitAsync(token);
        }

        public byte[] GetLastBlobData() => _lastBlobData;
        public string GetLastBlobFormat() => _lastBlobFormat ?? ".fits";

        public void AbortExposure() {
            // Drop the in-flight completion first so a BLOB that still trickles in from
            // the aborted exposure is ignored instead of being mistaken for the next frame.
            var pending = _exposureReady;
            _exposureReady = null;
            try {
                SetSwitchValue("CCD_ABORT_EXPOSURE", "ABORT", true);
                Logger.Info($"[{DeviceName}] Sent CCD_ABORT_EXPOSURE");
            } catch (ArgumentException ex) {
                Logger.Debug($"[{DeviceName}] Cannot abort exposure: {ex.Message}");
            }
            // Wake any waiter immediately — no BLOB will follow an abort. Callers that
            // aborted without cancelling their capture token (e.g. the ninaAPI
            // abort-exposure endpoint) would otherwise sit in WaitUntilExposureIsReady
            // until the camera timeout expires. The waiter then proceeds to download,
            // finds no BLOB data and reports the aborted frame as a failed download.
            pending?.TrySetResult(true);
        }

        public void StopExposure() => AbortExposure();

        public int CameraXSize => (int)(GetNumberPropertyValue("CCD_INFO", "CCD_MAX_X") ?? 0.0);
        public int CameraYSize => (int)(GetNumberPropertyValue("CCD_INFO", "CCD_MAX_Y") ?? 0.0);
        public double PixelSizeX => GetNumberPropertyValue("CCD_INFO", "CCD_PIXEL_SIZE_X") ?? double.NaN;
        public double PixelSizeY => GetNumberPropertyValue("CCD_INFO", "CCD_PIXEL_SIZE_Y") ?? double.NaN;
        public int BitDepth => (int)(GetNumberPropertyValue("CCD_INFO", "CCD_BITSPERPIXEL") ?? 16.0);

        public double ExposureMin {
            get {
                var min = GetNumberElement("CCD_EXPOSURE", "CCD_EXPOSURE_VALUE")?.Min ?? 0.0;
                return min > 0 ? min : 0.001;
            }
        }

        public double ExposureMax {
            get {
                var max = GetNumberElement("CCD_EXPOSURE", "CCD_EXPOSURE_VALUE")?.Max ?? 0.0;
                return max > 0 ? max : 3600.0;
            }
        }

        public short MaxBinX {
            get {
                var max = GetNumberElement("CCD_BINNING", "HOR_BIN")?.Max ?? 0.0;
                return (short)(max > 0 ? max : 1);
            }
        }

        public short MaxBinY {
            get {
                var max = GetNumberElement("CCD_BINNING", "VER_BIN")?.Max ?? 0.0;
                return (short)(max > 0 ? max : 1);
            }
        }

        public short BinX {
            get => (short)(GetNumberPropertyValue("CCD_BINNING", "HOR_BIN") ?? 1.0);
            set {
                if (Connected) {
                    TrySetNumber("CCD_BINNING", "HOR_BIN", value);
                }
            }
        }

        public short BinY {
            get => (short)(GetNumberPropertyValue("CCD_BINNING", "VER_BIN") ?? 1.0);
            set {
                if (Connected) {
                    TrySetNumber("CCD_BINNING", "VER_BIN", value);
                }
            }
        }

        // CCD_CFA is a text vector announcing the color filter array of one-shot-color
        // sensors (e.g. CFA_TYPE=RGGB). Monochrome cameras do not expose it.
        public string CfaType => GetTextPropertyValue("CCD_CFA", "CFA_TYPE");
        public int CfaOffsetX => int.TryParse(GetTextPropertyValue("CCD_CFA", "CFA_OFFSET_X"), out var x) ? x : 0;
        public int CfaOffsetY => int.TryParse(GetTextPropertyValue("CCD_CFA", "CFA_OFFSET_Y"), out var y) ? y : 0;

        public double Temperature => GetNumberPropertyValue("CCD_TEMPERATURE", "CCD_TEMPERATURE_VALUE") ?? double.NaN;

        public double TemperatureSetPoint {
            // Return the last value we commanded, not the current sensor reading.
            // In INDI, CCD_TEMPERATURE is both read (current temp) and write (target temp),
            // so we must track the target ourselves.
            get => _temperatureSetPoint;
            set {
                if (Connected) {
                    try {
                        SetNumberValue("CCD_TEMPERATURE", "CCD_TEMPERATURE_VALUE", value);
                        _temperatureSetPoint = value;
                    } catch (ArgumentException ex) {
                        Logger.Debug($"[{DeviceName}] Cannot set CCD_TEMPERATURE: {ex.Message}");
                    }
                }
            }
        }

        public bool CanSetTemperature => GetSwitchProperty("CCD_COOLER") != null;

        public bool CoolerOn {
            // CCD_COOLER is perm="wo" on many drivers (its switch values are definition
            // defaults, not state). Prefer a real readback: readable CCD_COOLER vector
            // first, then the reported cooler power, then our locally tracked commands.
            get {
                var prop = GetSwitchProperty("CCD_COOLER");
                if (prop != null && prop.Permission != PropertyPermission.WriteOnly) {
                    return prop.Switches.FirstOrDefault(s => s.Name == "COOLER_ON")?.Value ?? _coolerOn;
                }
                var power = GetNumberPropertyValue("CCD_COOLER_POWER", "COOLER_POWER");
                if (power.HasValue) {
                    return power.Value > 0;
                }
                return _coolerOn;
            }
            set {
                if (Connected) {
                    try {
                        if (value) {
                            SetSwitchValue("CCD_COOLER", "COOLER_ON", true);
                        } else {
                            SetSwitchValue("CCD_COOLER", "COOLER_OFF", true);
                        }
                        _coolerOn = value;
                    } catch (ArgumentException ex) {
                        Logger.Debug($"[{DeviceName}] Cannot set CCD_COOLER: {ex.Message}");
                    }
                }
            }
        }

        public double CoolerPower => GetNumberPropertyValue("CCD_COOLER_POWER", "COOLER_POWER") ?? double.NaN;

        // Most drivers expose gain via the standard CCD_GAIN/GAIN vector; the toupbase
        // family (indi_toupcam_ccd etc.) publishes it as the "Gain" element of the
        // CCD_CONTROLS vector instead.
        private (string Property, string Element)? GainElement {
            get {
                if (GetNumberElement("CCD_GAIN", "GAIN") != null) return ("CCD_GAIN", "GAIN");
                if (GetNumberElement("CCD_CONTROLS", "Gain") != null) return ("CCD_CONTROLS", "Gain");
                return null;
            }
        }

        public bool CanGetGain => GainElement != null;
        public bool CanSetGain => CanGetGain;

        public int Gain {
            get => GainElement is { } e ? (int)(GetNumberPropertyValue(e.Property, e.Element) ?? 0.0) : 0;
            set {
                if (Connected && GainElement is { } e) {
                    TrySetNumber(e.Property, e.Element, value);
                }
            }
        }

        public int GainMin => GainElement is { } e ? (int)(GetNumberElement(e.Property, e.Element)?.Min ?? 0.0) : 0;
        public int GainMax => GainElement is { } e ? (int)(GetNumberElement(e.Property, e.Element)?.Max ?? 0.0) : 0;

        // Generic INDI has no standard readout-mode vector. The toupbase family models
        // conversion gain (LCG/HCG/HDR) as the TC_CONVERSION_GAIN switch, which maps
        // naturally onto NINA's readout modes. Extend this list for other drivers.
        private static readonly string[] ReadoutModeSwitchProperties = ["TC_CONVERSION_GAIN"];

        private INDISwitchProperty GetReadoutModeProperty() {
            foreach (var name in ReadoutModeSwitchProperties) {
                var prop = GetSwitchProperty(name);
                if (prop != null) return prop;
            }
            return null;
        }

        public string[] ReadoutModes {
            get {
                var prop = GetReadoutModeProperty();
                if (prop == null) return [];
                return prop.Switches.Select(s => string.IsNullOrEmpty(s.Label) ? s.Name : s.Label).ToArray();
            }
        }

        public short ReadoutMode {
            get {
                var index = GetReadoutModeProperty()?.Switches.FindIndex(s => s.Value) ?? -1;
                return (short)Math.Max(index, 0);
            }
            set {
                var prop = GetReadoutModeProperty();
                if (prop != null && value >= 0 && value < prop.Switches.Count) {
                    TrySetSwitch(prop.Name, prop.Switches[value].Name, true);
                }
            }
        }

        public bool CanSetOffset => GetNumberProperty("CCD_OFFSET") != null;

        public int Offset {
            get => (int)(GetNumberPropertyValue("CCD_OFFSET", "OFFSET") ?? 0.0);
            set {
                if (Connected) {
                    TrySetNumber("CCD_OFFSET", "OFFSET", value);
                }
            }
        }

        public int OffsetMin => (int)(GetNumberElement("CCD_OFFSET", "OFFSET")?.Min ?? 0.0);
        public int OffsetMax => (int)(GetNumberElement("CCD_OFFSET", "OFFSET")?.Max ?? 0.0);

        // USB bandwidth/speed control. The toupbase family exposes it as the "Speed"
        // element of CCD_CONTROLS (0..model->maxspeed). Extend for other drivers as needed.
        private (string Property, string Element)? UsbLimitElement {
            get {
                if (GetNumberElement("CCD_CONTROLS", "Speed") != null) return ("CCD_CONTROLS", "Speed");
                return null;
            }
        }

        public bool CanSetUSBLimit => UsbLimitElement != null;

        public int USBLimit {
            get => UsbLimitElement is { } e ? (int)(GetNumberPropertyValue(e.Property, e.Element) ?? 0.0) : 0;
            set {
                if (Connected && UsbLimitElement is { } e) {
                    TrySetNumber(e.Property, e.Element, value);
                }
            }
        }

        public int USBLimitMin => UsbLimitElement is { } e ? (int)(GetNumberElement(e.Property, e.Element)?.Min ?? 0.0) : 0;
        public int USBLimitMax => UsbLimitElement is { } e ? (int)(GetNumberElement(e.Property, e.Element)?.Max ?? 0.0) : 0;
        public int USBLimitStep => UsbLimitElement is { } e ? Math.Max((int)(GetNumberElement(e.Property, e.Element)?.Step ?? 1.0), 1) : 1;

        // Simple INDI_ENABLED/INDI_DISABLED switch helper used by the toupbase feature
        // properties below (low noise, high fullwell, tail light).
        private bool? GetIndiEnabled(string propertyName) {
            var prop = GetSwitchProperty(propertyName);
            if (prop == null) return null;
            var on = prop.Switches.FirstOrDefault(s => s.Name == "INDI_ENABLED");
            return on?.Value;
        }

        private void SetIndiEnabled(string propertyName, bool value) {
            TrySetSwitch(propertyName, value ? "INDI_ENABLED" : "INDI_DISABLED", true);
        }

        // TC_LOW_NOISE — toupbase "ultra mode" / low-noise mode
        public bool HasLowNoiseMode => GetSwitchProperty("TC_LOW_NOISE") != null;

        public bool LowNoiseMode {
            get => GetIndiEnabled("TC_LOW_NOISE") ?? false;
            set {
                if (Connected) SetIndiEnabled("TC_LOW_NOISE", value);
            }
        }

        // TC_HIGHFULLWELL (no underscore in the driver!) — toupbase high fullwell capacity mode
        public bool HasHighFullwell => GetSwitchProperty("TC_HIGHFULLWELL") != null;

        public bool HighFullwellMode {
            get => GetIndiEnabled("TC_HIGHFULLWELL") ?? false;
            set {
                if (Connected) SetIndiEnabled("TC_HIGHFULLWELL", value);
            }
        }

        // TC_TAILLIGHT (no underscore in the driver!) — toupbase LED indicator / tail light
        public bool HasTailLight => GetSwitchProperty("TC_TAILLIGHT") != null;

        public bool TailLight {
            get => GetIndiEnabled("TC_TAILLIGHT") ?? false;
            set {
                if (Connected) SetIndiEnabled("TC_TAILLIGHT", value);
            }
        }

        // The toupbase heat control is a one-of-many switch: INDI_DISABLED plus either a
        // single INDI_ENABLED level or HEAT1..HEATn strength levels.
        public bool HasDewHeater => GetSwitchProperty("TC_HEAT_CONTROL") != null;

        public bool DewHeaterOn {
            get {
                var active = GetSwitchProperty("TC_HEAT_CONTROL")?.Switches.FirstOrDefault(s => s.Value);
                return active != null && active.Name != "INDI_DISABLED";
            }
        }

        public int DewHeaterMaxStrength => GetSwitchProperty("TC_HEAT_CONTROL")?.Switches.Count(s => s.Name != "INDI_DISABLED") ?? 0;

        public void SetDewHeater(bool on, int strength) {
            var prop = GetSwitchProperty("TC_HEAT_CONTROL");
            if (prop == null) return;

            if (!on) {
                TrySetSwitch("TC_HEAT_CONTROL", "INDI_DISABLED", true);
                return;
            }

            var levels = prop.Switches.Where(s => s.Name != "INDI_DISABLED").ToList();
            if (levels.Count == 0) return;

            // strength <= 0 selects the highest available level; otherwise clamp to 1..n
            var index = strength <= 0 ? levels.Count - 1 : Math.Min(strength, levels.Count) - 1;
            TrySetSwitch("TC_HEAT_CONTROL", levels[index].Name, true);
        }

        public bool CanSubSample => GetNumberProperty("CCD_FRAME") != null;
    }
}
