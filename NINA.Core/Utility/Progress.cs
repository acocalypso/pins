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
using System.Threading.Tasks;
using NINA.Core.SignalR;

namespace NINA.Core.Utility {
    /// <summary>
    /// Static broadcaster for progress messages, similar to Notification
    /// </summary>
    public static class Progress {
        public delegate Task ProgressBroadcaster(ProgressMessage message);
        public static ProgressBroadcaster Broadcaster { get; set; }

        private static void BroadcastProgress(ProgressMessage message) {
            try {
                var task = Broadcaster?.Invoke(message);
                if (task != null) {
                    _ = task; // fire and forget
                }
            } catch (Exception ex) {
                Logger.Error($"Failed to broadcast progress: {ex}");
            }
        }

        public static void Publish(ProgressMessage message) {
            BroadcastProgress(message);
        }
    }
}