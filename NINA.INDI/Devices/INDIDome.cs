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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.INDI.Devices {

    public class INDIDome : INDIDevice, IINDIDome {

        public INDIDome(INDIDeviceInfo device) : base(device) {
        }

        /// <summary>
        /// Wait for the azimuth position property to arrive before completing Connect().
        /// </summary>
        protected override string[] GetRequiredConnectionProperties() {
            return ["ABS_DOME_POSITION"];
        }

        public override void OnTextPropertyUpdated(INDITextProperty p) {
            base.OnTextPropertyUpdated(p);
        }

        public override void OnNumberPropertyUpdated(INDINumberProperty p) {
            base.OnNumberPropertyUpdated(p);
        }

        public override void OnSwitchPropertyUpdated(INDISwitchProperty p) {
            base.OnSwitchPropertyUpdated(p);

            if (p.Name == "DOME_SHUTTER") {
                UpdateShutterState(p);
            }
        }

        public override void OnBlobPropertyUpdated(INDIBlobProperty p) {
            base.OnBlobPropertyUpdated(p);
        }

        // ── Capabilities ──────────────────────────────────────────────────────────

        public bool CanSetShutter => GetSwitchProperty("DOME_SHUTTER") != null;
        public bool CanSetAzimuth => GetNumberProperty("ABS_DOME_POSITION") != null;
        public bool CanSetPark => GetSwitchProperty("DOME_PARK") != null;
        public bool CanSyncAzimuth => GetNumberProperty("DOME_SYNC") != null;
        public bool CanPark => GetSwitchProperty("DOME_PARK") != null;
        public bool CanFindHome {
            get {
                var sw = GetSwitchProperty("FIND_HOME") ?? GetSwitchProperty("HOME_PARK");
                return sw != null;
            }
        }
        public bool DriverCanFollow => GetSwitchProperty("DOME_AUTOSYNC") != null;

        // ── State ─────────────────────────────────────────────────────────────────

        private ShutterState _shutterState = ShutterState.ShutterNone;

        public ShutterState ShutterStatus => _shutterState;

        private void UpdateShutterState(INDISwitchProperty p) {
            if (p.State == PropertyState.Busy) {
                var openSwitch = p.Switches.FirstOrDefault(s => s.Name == "SHUTTER_OPEN");
                _shutterState = (openSwitch?.Value == true) ? ShutterState.ShutterOpening : ShutterState.ShutterClosing;
            } else if (p.State == PropertyState.Ok) {
                var openSwitch = p.Switches.FirstOrDefault(s => s.Name == "SHUTTER_OPEN");
                _shutterState = (openSwitch?.Value == true) ? ShutterState.ShutterOpen : ShutterState.ShutterClosed;
            } else if (p.State == PropertyState.Alert) {
                _shutterState = ShutterState.ShutterError;
            }
        }

        public double Azimuth => GetNumberPropertyValue("ABS_DOME_POSITION", "DOME_ABSOLUTE_POSITION") ?? double.NaN;

        public bool AtPark => GetSwitchPropertyValue("DOME_PARK", "PARK") ?? false;

        public bool AtHome {
            get {
                // Some drivers expose AT_HOME, others HOME_PARK
                var val = GetSwitchPropertyValue("AT_HOME", "AT_HOME")
                       ?? GetSwitchPropertyValue("HOME_PARK", "AT_HOME");
                return val ?? false;
            }
        }

        public bool Slewing {
            get {
                var absProp = GetNumberProperty("ABS_DOME_POSITION");
                if (absProp?.State == PropertyState.Busy) return true;
                var motionProp = GetSwitchProperty("DOME_MOTION");
                return motionProp?.State == PropertyState.Busy;
            }
        }

        public bool DriverFollowing {
            get => GetSwitchPropertyValue("DOME_AUTOSYNC", "DOME_AUTOSYNC_ENABLE") ?? false;
            set {
                try {
                    if (value) {
                        SetSwitchValue("DOME_AUTOSYNC", "DOME_AUTOSYNC_ENABLE", true);
                    } else {
                        SetSwitchValue("DOME_AUTOSYNC", "DOME_AUTOSYNC_DISABLE", true);
                    }
                } catch (ArgumentException ex) {
                    Logger.Warning($"INDIDome: Cannot set DriverFollowing — {ex.Message}");
                }
            }
        }

        // ── Operations ────────────────────────────────────────────────────────────

        public async Task SlewToAzimuth(double azimuth, CancellationToken ct) {
            if (!Connected || !CanSetAzimuth) return;
            await SetNumberValuesAsync("ABS_DOME_POSITION", TimeSpan.FromSeconds(10),
                ("DOME_ABSOLUTE_POSITION", azimuth));
            // Wait until motion stops
            while (Slewing && !ct.IsCancellationRequested) {
                await Task.Delay(500, ct);
            }
        }

        public async Task OpenShutter(CancellationToken ct) {
            if (!Connected || !CanSetShutter) return;
            await SetSwitchValueAsync("DOME_SHUTTER", "SHUTTER_OPEN", true, TimeSpan.FromSeconds(10));
            while (_shutterState == ShutterState.ShutterOpening && !ct.IsCancellationRequested) {
                await Task.Delay(500, ct);
            }
        }

        public async Task CloseShutter(CancellationToken ct) {
            if (!Connected || !CanSetShutter) return;
            await SetSwitchValueAsync("DOME_SHUTTER", "SHUTTER_CLOSE", true, TimeSpan.FromSeconds(10));
            while (_shutterState == ShutterState.ShutterClosing && !ct.IsCancellationRequested) {
                await Task.Delay(500, ct);
            }
        }

        public async Task Park(CancellationToken ct) {
            if (!Connected || !CanPark) return;
            await SetSwitchValueAsync("DOME_PARK", "PARK", true, TimeSpan.FromSeconds(10));
            while (!AtPark && !ct.IsCancellationRequested) {
                await Task.Delay(500, ct);
            }
        }

        public void FindHome() {
            if (!Connected || !CanFindHome) return;
            try {
                var prop = GetSwitchProperty("FIND_HOME") ?? GetSwitchProperty("HOME_PARK");
                if (prop != null) {
                    SetSwitchValue(prop.Name, prop.Switches.First().Name, true);
                }
            } catch (ArgumentException ex) {
                Logger.Warning($"INDIDome: Cannot find home — {ex.Message}");
            }
        }

        public void SetPark() {
            if (!Connected) return;
            try {
                SetSwitchValue("DOME_PARK", "DOME_PARK_WRITE", true);
            } catch (ArgumentException ex) {
                Logger.Warning($"INDIDome: Cannot set park position — {ex.Message}");
            }
        }

        public void SyncToAzimuth(double azimuth) {
            if (!Connected || !CanSyncAzimuth) return;
            try {
                SetNumberValue("DOME_SYNC", "DOME_SYNC_VALUE", azimuth);
            } catch (ArgumentException ex) {
                Logger.Warning($"INDIDome: Cannot sync azimuth — {ex.Message}");
            }
        }

        public void Abort() {
            if (!Connected) return;
            try {
                SetSwitchValue("DOME_ABORT_MOTION", "ABORT", true);
            } catch (ArgumentException ex) {
                Logger.Warning($"INDIDome: Cannot abort — {ex.Message}");
            }
        }

        #region Unsupported

        public IList<string> SupportedActions { get; } = new List<string>();

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public void CommandBlind(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        public bool CommandBool(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        public string CommandString(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        #endregion
    }
}
