#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using static Wanderer.WandererCoverSDK;
using System.Linq;

namespace NINA.Equipment.Equipment.MyFlatDevice {

    public partial class WandererCover : BaseINPC, IFlatDevice {
        private readonly IProfileService _profileService;
        private readonly int _id;

        public WandererCover(int id, IProfileService profileService) {
            _profileService = profileService;
            _id = id;

            // Grab model
            Name = $"Wanderer Cover ({_id})";
        }

        public bool HasSetupDialog => false;
        public string Id => $"{_id}";
        public string Name { get; private set; }
        public string DisplayName => Name;
        public string Category => "WandererCover";

        [ObservableProperty]
        private bool connected = false;

        public string Description => "Native driver for WandererCover FlatDevices";

        public string DriverInfo { get; private set; } = string.Empty;
        public string DriverVersion => GetSDKVersion();

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                // Verify, the flat panel id actually exists
                int[] ids = new int[WC_MAX_NUM];
                CoverScan(out var count, ids);
                if (!ids.Take(count).Contains(_id)) {
                    Notification.ShowError(Loc.Instance["LblWandererCoverNotAvailableError"]);
                    Logger.Error("Selected WandererCover FlatDevices not available (disconnected?)");
                    return false;
                }
                if (CoverOpen(_id) == WC_ERROR_TYPE.WC_SUCCESS) {
                    DriverInfo = $"SDK: {DriverVersion}";
                    Connected = true;
                    return true;
                } else {
                    Logger.Error("Failed to connect to WandererCover");
                    return false;
                }
            }, token);
        }

        public void Disconnect() {
            _ = CoverClose(_id);
            Connected = false;
        }

        private void DisconnectOnRemovedError() {
            try {
                Notification.ShowWarning(Loc.Instance["LblCoverConnectionLost"]);
                Logger.Error($"WandererCover device was removed");
                Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public void SetupDialog() {
        }

        public CoverState CoverState {
            get {
                if (!Connected) {
                    return CoverState.Unknown;
                }

                var err = CoverGetStatus(_id, out var status);
                if (err == WC_ERROR_TYPE.WC_SUCCESS) {
                    return status.coverState switch {
                        0 => CoverState.Closed,
                        1 => CoverState.Open,
                        2 => CoverState.NeitherOpenNorClosed,
                        3 => CoverState.NeitherOpenNorClosed,
                        _ => CoverState.Error
                    };
                } else {
                    if (err == WC_ERROR_TYPE.WC_ERROR_COMMUNICATION) {
                        Logger.Error($"WandererCover communication error to get brightness {err}");
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"WandererCover error to get brightness {err}");
                    }
                    return CoverState.Error;
                }
            }
        }

        public int MaxBrightness => 255;
        public int MinBrightness => 0;

        public async Task<bool> Open(CancellationToken ct, int delay = 300) {
            if (!Connected) return await Task.Run(() => false, ct);
            return await Task.Run(async () => {
                var err = CoverOpenCover(_id);
                if (err != WC_ERROR_TYPE.WC_SUCCESS) {
                    if (err == WC_ERROR_TYPE.WC_ERROR_COMMUNICATION) {
                        Logger.Error($"WandererCover communication error to open cover {err}");
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"WandererCover error to open cover {err}");
                    }
                    return false;
                }
                while (!ct.IsCancellationRequested) {
                    await CoreUtil.Delay(delay, ct);
                    _ = CoverGetStatus(_id, out var status);
                    if (status.coverState == 3) {
                        // Still moving
                        continue;
                    }
                    if (status.coverState == 0) {
                        // Return true on open state
                        return true;
                    }
                    return false;
                }
                return false;
            }, ct);
        }

        public async Task<bool> Close(CancellationToken ct, int delay = 300) {
            if (!Connected) return await Task.Run(() => false, ct);
            return await Task.Run(async () => {
                var err = CoverCloseCover(_id);
                if (err != WC_ERROR_TYPE.WC_SUCCESS) {
                    if (err == WC_ERROR_TYPE.WC_ERROR_COMMUNICATION) {
                        Logger.Error($"WandererCover communication error to close cover {err}");
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"WandererCover error to close cover {err}");
                    }
                    return false;
                }
                while (!ct.IsCancellationRequested) {
                    await CoreUtil.Delay(delay, ct);
                    _ = CoverGetStatus(_id, out var status);
                    if (status.coverState == 3) {
                        // Still moving
                        continue;
                    }
                    if (status.coverState == 1) {
                        // Return true on closed state
                        return true;
                    }
                    return false;
                }
                return false;
            }, ct);
        }

        public bool LightOn {
            get {
                if (!Connected) {
                    return false;
                }

                var err = CoverGetConfig(_id, out var config);
                if (err == WC_ERROR_TYPE.WC_SUCCESS) {
                    return config.brightness > 0;
                } else {
                    if (err == WC_ERROR_TYPE.WC_ERROR_COMMUNICATION) {
                        Logger.Error($"WandererCover communication error to get brightness {err}");
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"WandererCover error to get brightness {err}");
                    }
                    return false;
                }
            }
            set {
                WC_COVER_CONFIG config = new() {
                    mask = MASK_COVER_BRIGHTNESS,
                    brightness = value ? (lastBrightness != 0 ? lastBrightness : MaxBrightness) : 0
                };
                _ = CoverSetConfig(_id, config);
                RaisePropertyChanged(nameof(Brightness));
            }
        }

        private int lastBrightness = 0;

        public int Brightness {
            get {
                if (!Connected) {
                    return -1;
                }

                var err = CoverGetConfig(_id, out var config);
                if (err == WC_ERROR_TYPE.WC_SUCCESS) {
                    return config.brightness;
                } else {
                    if (err == WC_ERROR_TYPE.WC_ERROR_COMMUNICATION) {
                        Logger.Error($"WandererCover communication error to get brightness {err}");
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"WandererCover error to get brightness {err}");
                    }
                    return -1;
                }
            }
            set {
                WC_COVER_CONFIG config = new() {
                    mask = MASK_COVER_BRIGHTNESS,
                    brightness = value
                };
                Logger.Info($"Setting brightness to {value}");
                _ = CoverSetConfig(_id, config);
                lastBrightness = value;
                RaisePropertyChanged(nameof(Brightness));
            }
        }

        public bool SupportsOpenClose => true;

        public bool SupportsOnOff => true;

        public string PortName { get => string.Empty; set { } }

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