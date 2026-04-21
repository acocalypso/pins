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
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MySafetyMonitor {

    internal class IndiSafetyMonitor : IndiDevice<IINDISafetyMonitor>, ISafetyMonitor {

        public IndiSafetyMonitor(INDIDeviceInfo info, IProfileService profileService = null) : base(info) {
            this.profileService = profileService;
        }

        private IProfileService profileService;

        public bool IsSafe => GetProperty(nameof(IINDISafetyMonitor.IsSafe), false);

        protected override string ConnectionLostMessage => Loc.Instance["LblSafetyMonitorConnectionLost"];

        protected override Task PreConnect() {
            if (profileService != null) {
                var settings = profileService.ActiveProfile.SafetyMonitorSettings;
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

        protected override IINDISafetyMonitor GetInstance() {
            return device ??= new INDISafetyMonitor(_device);
        }
    }
}
