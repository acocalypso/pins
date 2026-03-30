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
    public interface IINDIFlatDevice : IINDIDevice {

        CoverState CoverState { get; }

        int MaxBrightness { get; }

        int MinBrightness { get; }

        Task<bool> Open(CancellationToken ct, int delay = 300);

        Task<bool> Close(CancellationToken ct, int delay = 300);

        bool LightOn { get; set; }

        int Brightness { get; set; }

        bool SupportsOpenClose { get; }

        bool SupportsOnOff { get; }
    }
}