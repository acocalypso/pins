#region "copyright"

/*
    Copyright © 2025-2026 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using NINA.INDI;
using NINA.INDI.Devices;
using NINA.INDI.Interfaces;
using NINA.Profile.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyDome {

    internal class IndiDome : IndiDevice<IINDIDome>, IDome {

        public IndiDome(INDIDeviceInfo info, IProfileService profileService = null) : base(info) {
            this.profileService = profileService;
        }

        private IProfileService profileService;

        public ShutterState ShutterStatus {
            get {
                var state = GetProperty(nameof(IINDIDome.ShutterStatus), NINA.INDI.Enums.ShutterState.ShutterNone);
                return state switch {
                    NINA.INDI.Enums.ShutterState.ShutterOpen => ShutterState.ShutterOpen,
                    NINA.INDI.Enums.ShutterState.ShutterClosed => ShutterState.ShutterClosed,
                    NINA.INDI.Enums.ShutterState.ShutterOpening => ShutterState.ShutterOpening,
                    NINA.INDI.Enums.ShutterState.ShutterClosing => ShutterState.ShutterClosing,
                    NINA.INDI.Enums.ShutterState.ShutterError => ShutterState.ShutterError,
                    _ => ShutterState.ShutterNone,
                };
            }
        }
        public bool DriverCanFollow => GetProperty(nameof(IINDIDome.DriverCanFollow), false);
        public bool CanSetShutter => GetProperty(nameof(IINDIDome.CanSetShutter), false);
        public bool CanSetPark => GetProperty(nameof(IINDIDome.CanSetPark), false);
        public bool CanSetAzimuth => GetProperty(nameof(IINDIDome.CanSetAzimuth), false);
        public bool CanSyncAzimuth => GetProperty(nameof(IINDIDome.CanSyncAzimuth), false);
        public bool CanPark => GetProperty(nameof(IINDIDome.CanPark), false);
        public bool CanFindHome => GetProperty(nameof(IINDIDome.CanFindHome), false);
        public double Azimuth => GetProperty(nameof(IINDIDome.Azimuth), double.NaN);
        public double Altitude => double.NaN;
        public bool AtPark => GetProperty(nameof(IINDIDome.AtPark), false);
        public bool AtHome => GetProperty(nameof(IINDIDome.AtHome), false);
        public bool Slewing => GetProperty(nameof(IINDIDome.Slewing), false);

        public bool DriverFollowing {
            get => GetProperty(nameof(IINDIDome.DriverFollowing), false);
            set {
                if (device != null) {
                    device.DriverFollowing = value;
                }
            }
        }

        protected override string ConnectionLostMessage => Loc.Instance["LblDomeConnectionLost"];

        protected override Task PreConnect() {
            if (profileService != null) {
                var settings = profileService.ActiveProfile.DomeSettings;
                var instance = GetInstance();
                instance.ConfigureConnectionProperties(
                    settings.IndiConnectionMode,
                    settings.IndiAutoSearch,
                    settings.IndiAddress,
                    settings.IndiPort,
                    settings.IndiBaudRate
                );
            }
            return Task.CompletedTask;
        }

        protected override IINDIDome GetInstance() {
            return device ??= new INDIDome(_device);
        }

        public Task SlewToAzimuth(double azimuth, CancellationToken ct) {
            return device?.SlewToAzimuth(azimuth, ct) ?? Task.CompletedTask;
        }

        public Task StopSlewing() {
            device?.Abort();
            return Task.CompletedTask;
        }

        public Task StopShutter() {
            device?.Abort();
            return Task.CompletedTask;
        }

        public Task StopAll() {
            device?.Abort();
            return Task.CompletedTask;
        }

        public Task OpenShutter(CancellationToken ct) {
            return device?.OpenShutter(ct) ?? Task.CompletedTask;
        }

        public Task CloseShutter(CancellationToken ct) {
            return device?.CloseShutter(ct) ?? Task.CompletedTask;
        }

        public Task FindHome(CancellationToken ct) {
            device?.FindHome();
            return Task.CompletedTask;
        }

        public Task Park(CancellationToken ct) {
            return device?.Park(ct) ?? Task.CompletedTask;
        }

        public void SetPark() {
            device?.SetPark();
        }

        public void SyncToAzimuth(double azimuth) {
            device?.SyncToAzimuth(azimuth);
        }
    }
}
