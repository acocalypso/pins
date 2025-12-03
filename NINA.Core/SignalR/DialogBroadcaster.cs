#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Microsoft.AspNetCore.SignalR;
using NINA.Core.Model;
using NINA.Core.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NINA.Core.SignalR {
    /// <summary>
    /// Service to broadcast dialog data via SignalR
    /// </summary>
    public class DialogBroadcaster : IDialogBroadcaster {
        private readonly IHubContext<DialogHub> _hubContext;
        private static IDialogBroadcaster _instance;
        private static readonly ConcurrentDictionary<string, DialogData> _activeDialogs = new();

        public DialogBroadcaster(IHubContext<DialogHub> hubContext) {
            _hubContext = hubContext;
            _instance = this; // Store singleton instance for static access
        }

        /// <summary>
        /// Get the singleton instance (for use by plugins that don't have DI access)
        /// </summary>
        public static IDialogBroadcaster Instance => _instance;
        
        /// <summary>
        /// Get all currently active dialogs (for sending to newly connected clients)
        /// </summary>
        public static IList<DialogData> GetActiveDialogs() {
            return new List<DialogData>(_activeDialogs.Values);
        }

        public async Task BroadcastDialogAsync(DialogData data) {
            try {
                if (data != null && _hubContext != null) {
                    // Track active dialog
                    if (data.Active) {
                        _activeDialogs[data.ContentType] = data;
                    }
                    
                    await _hubContext.Clients.All.SendAsync("ReceiveDialog", data);
                } else {
                    Logger.Warning($"Cannot broadcast dialog: data={data != null}, hubContext={_hubContext != null}");
                }
            } catch (Exception ex) {
                Logger.Error($"Failed to broadcast dialog via SignalR: {ex.Message}");
            }
        }

        public async Task BroadcastMeasurementAsync(DialogMeasurement measurement) {
            try {
                if (measurement != null && _hubContext != null) {
                    await _hubContext.Clients.All.SendAsync("ReceiveMeasurement", measurement);
                }
            } catch (Exception ex) {
                Logger.Error($"Failed to broadcast measurement via SignalR: {ex.Message}");
            }
        }

        public async Task BroadcastStatusAsync(string status) {
            try {
                if (!string.IsNullOrEmpty(status) && _hubContext != null) {
                    await _hubContext.Clients.All.SendAsync("ReceiveDialogStatus", status);
                }
            } catch (Exception ex) {
                Logger.Error($"Failed to broadcast dialog status via SignalR: {ex.Message}");
            }
        }

        public async Task ClearDialogAsync(string contentType) {
            try {
                if (_hubContext != null) {
                    // Remove from active dialogs
                    _activeDialogs.TryRemove(contentType, out _);
                    
                    await _hubContext.Clients.All.SendAsync("ClearDialog", contentType);
                }
            } catch (Exception ex) {
                Logger.Error($"Failed to clear dialog via SignalR: {ex.Message}");
            }
        }
    }
}
