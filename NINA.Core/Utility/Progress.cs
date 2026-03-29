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
using System.Collections.Generic;
using System.Threading.Tasks;
using NINA.Core.Model;
using NINA.Core.SignalR;

namespace NINA.Core.Utility {
    /// <summary>
    /// Static broadcaster for progress messages, similar to Notification
    /// </summary>
    public static class Progress {
        public delegate Task ProgressBroadcaster(ProgressMessage message);
        public static ProgressBroadcaster Broadcaster { get; set; }

        // Track last send time AND last status text per source to throttle updates
        private static readonly Dictionary<string, DateTime> lastSendTime = new Dictionary<string, DateTime>();
        private static readonly Dictionary<string, string> lastStatusText = new Dictionary<string, string>();
        private static readonly int throttleIntervalMs = 500;

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

        private static bool ShouldThrottle(string source, string status) {
            if (string.IsNullOrEmpty(source)) {
                return false;
            }

            lock (lastSendTime) {
                // Always send when the status text itself has changed
                if (!lastStatusText.TryGetValue(source, out var prevStatus) || prevStatus != status) {
                    lastSendTime[source] = DateTime.UtcNow;
                    lastStatusText[source] = status;
                    return false;
                }

                // Same status text — apply time-based throttle to suppress rapid progress ticks
                if (!lastSendTime.ContainsKey(source)) {
                    lastSendTime[source] = DateTime.UtcNow;
                    return false;
                }

                var timeSinceLastSend = DateTime.UtcNow - lastSendTime[source];
                if (timeSinceLastSend.TotalMilliseconds >= throttleIntervalMs) {
                    lastSendTime[source] = DateTime.UtcNow;
                    return false;
                }

                return true; // Same status, not enough time has passed — throttle this message
            }
        }

        private static void ClearThrottleForSource(string source) {
            if (string.IsNullOrEmpty(source)) {
                return;
            }

            lock (lastSendTime) {
                lastSendTime.Remove(source);
                lastStatusText.Remove(source);
            }
        }

        public static void PublishNewStatus(ApplicationStatus status) {
            ProgressMessage message = new ProgressMessage {
                Source = status.Source,
                Status = status.Status,
                ProgressType = status.ProgressType,
                Progress = status.Progress,
                MaxProgress = status.MaxProgress,
                Status2 = status.Status2,
                ProgressType2 = status.ProgressType2,
                Progress2 = status.Progress2,
                MaxProgress2 = status.MaxProgress2,
                Status3 = status.Status3,
                ProgressType3 = status.ProgressType3,
                Progress3 = status.Progress3,
                MaxProgress3 = status.MaxProgress3,
                State = "create",
                Timestamp = DateTime.UtcNow,
            };
            BroadcastProgress(message);
        }

        public static void PublishUpdateStatus(ApplicationStatus status) {
            // Throttle only when the same status text repeats rapidly (e.g. progress ticks)
            if (ShouldThrottle(status.Source, status.Status)) {
                return;
            }

            ProgressMessage message = new ProgressMessage {
                Source = status.Source,
                Status = status.Status,
                ProgressType = status.ProgressType,
                Progress = status.Progress,
                MaxProgress = status.MaxProgress,
                Status2 = status.Status2,
                ProgressType2 = status.ProgressType2,
                Progress2 = status.Progress2,
                MaxProgress2 = status.MaxProgress2,
                Status3 = status.Status3,
                ProgressType3 = status.ProgressType3,
                Progress3 = status.Progress3,
                MaxProgress3 = status.MaxProgress3,
                State = "update",
                Timestamp = DateTime.UtcNow,
            };
            BroadcastProgress(message);
        }
        
        public static void PublishRemoveStatus(ApplicationStatus status) {
            // Clear throttle tracking for this source when removing
            ClearThrottleForSource(status.Source);

            ProgressMessage message = new ProgressMessage {
                Source = status.Source,
                Status = string.Empty,
                State = "delete",
                Timestamp = DateTime.UtcNow,
            };
            BroadcastProgress(message);
        }
    }
}