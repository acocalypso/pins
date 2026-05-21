#region "copyright"

/*
    Copyright © 2026 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyFlatDevice {

    public class ManualFlatDevice : BaseINPC, IFlatDevice {

        public bool HasSetupDialog => false;

        public string Id => "manual_flat_device";
        public string Name => "Manual Flat Panel";
        public string DisplayName => Name;
        public string Category => "PI.N.S.";

        public bool Connected { get; private set; }

        public string Description => "A manually controlled flat panel";
        public string DriverInfo => "Built-in manual flat panel driver";
        public string DriverVersion => "1.0";

        public Task<bool> Connect(CancellationToken token) {
            Connected = true;
            RaiseAllPropertiesChanged();
            return Task.FromResult(true);
        }

        public void Disconnect() {
            _lightOn = false;
            Connected = false;
        }

        // The panel has no motorized cover — the user places it manually.
        public CoverState CoverState => CoverState.NotPresent;

        public int MaxBrightness => 0;
        public int MinBrightness => 0;

        // Open/Close are not supported; callers should check SupportsOpenClose first.
        public Task<bool> Open(CancellationToken ct, int delay = 300) => Task.FromResult(false);
        public Task<bool> Close(CancellationToken ct, int delay = 300) => Task.FromResult(false);

        private bool _lightOn;

        public bool LightOn {
            get => Connected && _lightOn;
            set {
                if (!Connected) return;
                MyMessageBox.Show(
                    $"Turn {(value ? "on" : "off")} flat panel",
                    "Manual flat panel",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxResult.OK);
                _lightOn = value;
                RaisePropertyChanged();
            }
        }

        public int Brightness {
            get => 0;
            set {
                // Do nothing
            }
        }

        public string PortName {
            get => string.Empty;
            set { }
        }

        public bool SupportsOpenClose => false;
        public bool SupportsOnOff => true;

        public void SetupDialog() {
        }

        public IList<string> SupportedActions => new List<string>();

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void SendCommandBlind(string command, bool raw) {
            throw new NotImplementedException();
        }
    }
}
