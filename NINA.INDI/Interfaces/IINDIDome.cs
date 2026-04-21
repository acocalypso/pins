#region "copyright"

/*
    Copyright © 2025-2026 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.INDI.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.INDI.Interfaces {
    public interface IINDIDome : IINDIDevice {
        bool CanSetShutter { get; }
        bool CanSetAzimuth { get; }
        bool CanSetPark { get; }
        bool CanSyncAzimuth { get; }
        bool CanPark { get; }
        bool CanFindHome { get; }
        bool DriverCanFollow { get; }

        ShutterState ShutterStatus { get; }
        double Azimuth { get; }
        bool AtPark { get; }
        bool AtHome { get; }
        bool Slewing { get; }
        bool DriverFollowing { get; set; }

        Task SlewToAzimuth(double azimuth, CancellationToken ct);
        Task OpenShutter(CancellationToken ct);
        Task CloseShutter(CancellationToken ct);
        Task Park(CancellationToken ct);
        void FindHome();
        void SetPark();
        void SyncToAzimuth(double azimuth);
        void Abort();
    }
}
