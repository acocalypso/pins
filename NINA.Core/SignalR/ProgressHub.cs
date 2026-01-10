#region "copyright"

/*
    Copyright © 2025-2026 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace NINA.Core.SignalR {
    /// <summary>
    /// SignalR hub for broadcasting progress messages
    /// </summary>
    public class ProgressHub : Hub {
        public override async Task OnConnectedAsync() {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception exception) {
            await base.OnDisconnectedAsync(exception);
        }
    }
}