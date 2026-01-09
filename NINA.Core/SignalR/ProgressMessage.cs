#region "copyright"

/*
    Copyright © 2025-2026 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;

namespace NINA.Core.SignalR {
    /// <summary>
    /// DTO representing progress updates for broadcasting via SignalR
    /// </summary>
    public class ProgressMessage {
        public string Source { get; set; }
        public string Status { get; set; }

        public double Progress { get; set; } = -1;
        public int MaxProgress { get; set; } = 1;

        public double Progress2 { get; set; } = -1;
        public int MaxProgress2 { get; set; } = 1;

        public double Progress3 { get; set; } = -1;
        public int MaxProgress3 { get; set; } = 1;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}