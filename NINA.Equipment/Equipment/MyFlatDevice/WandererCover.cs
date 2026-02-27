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
        private readonly int _uniqueId;

        public WandererCover(int id, string model, IProfileService profileService) {
            Id = $"{id}.{model}";
            _profileService = profileService;
            _uniqueId = id;

            // Grab model
            Name = $"Wanderer.Cover.{_uniqueId} ({model})";
        }

        public bool HasSetupDialog => false;
        public string Id { get; }
        public string Name { get; }
        public string DisplayName => Name;
        public string Category => "WandererCover";

        [ObservableProperty]
        private bool connected = false;

        public string Description => "Native driver for WandererCover FlatDevices";

        public string DriverInfo { get; private set; } = string.Empty;
        public string DriverVersion => GetSDKVersion();

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(() => {
                try {
                    // Verify, the flat panel id actually exists
                    int[] ids = new int[WC_MAX_NUM];
                    var scanErr = CoverScan(out var count, ids);
                    if (scanErr != WC_ERROR_TYPE.WC_SUCCESS) {
                        Notification.ShowError(Loc.Instance["LblWandererCoverNotAvailableError"]);
                        Logger.Error($"WandererCover scan failed: {scanErr}");
                        return false;
                    }
                    if (!ids.Take(count).Contains(_uniqueId)) {
                        Notification.ShowError(Loc.Instance["LblWandererCoverNotAvailableError"]);
                        Logger.Error("Selected WandererCover FlatDevices not available (disconnected?)");
                        return false;
                    }
                    var openErr = CoverOpen(_uniqueId);
                    if (openErr == WC_ERROR_TYPE.WC_SUCCESS) {
                        DriverInfo = $"SDK: {DriverVersion}";
                        Connected = true;
                        return true;
                    } else {
                        Logger.Error($"Failed to connect to WandererCover: {openErr}");
                        return false;
                    }
                } catch (Exception ex) {
                    Logger.Error($"WandererCover Connect error: {ex}");
                    Notification.ShowError($"WandererCover connection error: {ex.Message}");
                    return false;
                }
            }, token);
        }

        public void Disconnect() {
            try {
                _ = CoverClose(_uniqueId);
            } catch (Exception ex) {
                Logger.Error($"WandererCover Disconnect crashed: {ex}");
            } finally {
                Connected = false;
            }
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

                try {
                    var err = CoverGetStatus(_uniqueId, out var status);
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
                } catch (Exception ex) {
                    Logger.Error($"WandererCover CoverState crashed: {ex}");
                    DisconnectOnRemovedError();
                    return CoverState.Error;
                }
            }
        }

        public int MaxBrightness => 255;
        public int MinBrightness => 0;

        public async Task<bool> Open(CancellationToken ct, int delay = 300) {
            if (!Connected) return await Task.Run(() => false, ct);
            return await Task.Run(async () => {
                try {
                    var err = CoverOpenCover(_uniqueId);
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
                        _ = CoverGetStatus(_uniqueId, out var status);
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
                } catch (Exception ex) {
                    Logger.Error($"WandererCover Open crashed: {ex}");
                    DisconnectOnRemovedError();
                    return false;
                }
            }, ct);
        }

        public async Task<bool> Close(CancellationToken ct, int delay = 300) {
            if (!Connected) return await Task.Run(() => false, ct);
            return await Task.Run(async () => {
                try {
                    var err = CoverCloseCover(_uniqueId);
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
                        _ = CoverGetStatus(_uniqueId, out var status);
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
                } catch (Exception ex) {
                    Logger.Error($"WandererCover Close crashed: {ex}");
                    DisconnectOnRemovedError();
                    return false;
                }
            }, ct);
        }

        public bool LightOn {
            get {
                if (!Connected) {
                    return false;
                }

                try {
                    var err = CoverGetConfig(_uniqueId, out var config);
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
                } catch (Exception ex) {
                    Logger.Error($"WandererCover LightOn getter crashed: {ex}");
                    DisconnectOnRemovedError();
                    return false;
                }
            }
            set {
                try {
                    WC_COVER_CONFIG config = new() {
                        mask = MASK_COVER_BRIGHTNESS,
                        brightness = value ? (lastBrightness != 0 ? lastBrightness : MaxBrightness) : 0
                    };
                    _ = CoverSetConfig(_uniqueId, config);
                    RaisePropertyChanged(nameof(Brightness));
                } catch (Exception ex) {
                    Logger.Error($"WandererCover LightOn setter crashed: {ex}");
                    DisconnectOnRemovedError();
                }
            }
        }

        private int lastBrightness = 0;

        public int Brightness {
            get {
                if (!Connected) {
                    return -1;
                }

                try {
                    var err = CoverGetConfig(_uniqueId, out var config);
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
                } catch (Exception ex) {
                    Logger.Error($"WandererCover Brightness getter crashed: {ex}");
                    DisconnectOnRemovedError();
                    return -1;
                }
            }
            set {
                try {
                    WC_COVER_CONFIG config = new() {
                        mask = MASK_COVER_BRIGHTNESS,
                        brightness = value
                    };
                    Logger.Info($"Setting brightness to {value}");
                    _ = CoverSetConfig(_uniqueId, config);
                    lastBrightness = value;
                    RaisePropertyChanged(nameof(Brightness));
                } catch (Exception ex) {
                    Logger.Error($"WandererCover Brightness setter crashed: {ex}");
                    DisconnectOnRemovedError();
                }
            }
        }

        public int HeaterPower {
            get {
                if (!Connected) {
                    return 0;
                }

                try {
                    var err = CoverGetConfig(_uniqueId, out var config);
                    if (err == WC_ERROR_TYPE.WC_SUCCESS) {
                        return config.heaterPower;
                    } else {
                        if (err == WC_ERROR_TYPE.WC_ERROR_COMMUNICATION) {
                            Logger.Error($"WandererCover communication error to get heater power {err}");
                            DisconnectOnRemovedError();
                        } else {
                            Logger.Error($"WandererCover error to get heater power {err}");
                        }
                        return 0;
                    }
                } catch (Exception ex) {
                    Logger.Error($"WandererCover HeaterPower getter crashed: {ex}");
                    DisconnectOnRemovedError();
                    return 0;
                }
            }
            set {
                try {
                    WC_COVER_CONFIG config = new() {
                        mask = MASK_COVER_HEATER_POWER,
                        heaterPower = Math.Max(0, Math.Min(3, value))
                    };
                    Logger.Info($"Setting heater power to {value}");
                    _ = CoverSetConfig(_uniqueId, config);
                    RaisePropertyChanged(nameof(HeaterPower));
                } catch (Exception ex) {
                    Logger.Error($"WandererCover HeaterPower setter crashed: {ex}");
                    DisconnectOnRemovedError();
                }
            }
        }

        public float OpenPositionAngle {
            get {
                if (!Connected) {
                    return float.NaN;
                }

                try {
                    var err = CoverGetStatus(_uniqueId, out var status);
                    if (err == WC_ERROR_TYPE.WC_SUCCESS) {
                        return status.openPositionAngle;
                    } else {
                        if (err == WC_ERROR_TYPE.WC_ERROR_COMMUNICATION) {
                            Logger.Error($"WandererCover communication error to get open position angle {err}");
                            DisconnectOnRemovedError();
                        } else {
                            Logger.Error($"WandererCover error to get open position angle {err}");
                        }
                        return float.NaN;
                    }
                } catch (Exception ex) {
                    Logger.Error($"WandererCover OpenPositionAngle getter crashed: {ex}");
                    DisconnectOnRemovedError();
                    return float.NaN;
                }
            }
            set {
                try {
                    WC_COVER_CONFIG config = new() {
                        mask = MASK_COVER_OPEN_POSITION,
                        openPositionAngle = value
                    };
                    Logger.Info($"Setting open position angle to {value}");
                    _ = CoverSetConfig(_uniqueId, config);
                    RaisePropertyChanged(nameof(OpenPositionAngle));
                } catch (Exception ex) {
                    Logger.Error($"WandererCover OpenPositionAngle setter crashed: {ex}");
                    DisconnectOnRemovedError();
                }
            }
        }

        public float ClosePositionAngle {
            get {
                if (!Connected) {
                    return float.NaN;
                }

                try {
                    var err = CoverGetStatus(_uniqueId, out var status);
                    if (err == WC_ERROR_TYPE.WC_SUCCESS) {
                        return status.closePositionAngle;
                    } else {
                        if (err == WC_ERROR_TYPE.WC_ERROR_COMMUNICATION) {
                            Logger.Error($"WandererCover communication error to get close position angle {err}");
                            DisconnectOnRemovedError();
                        } else {
                            Logger.Error($"WandererCover error to get close position angle {err}");
                        }
                        return float.NaN;
                    }
                } catch (Exception ex) {
                    Logger.Error($"WandererCover ClosePositionAngle getter crashed: {ex}");
                    DisconnectOnRemovedError();
                    return float.NaN;
                }
            }
            set {
                try {
                    WC_COVER_CONFIG config = new() {
                        mask = MASK_COVER_CLOSE_POSITION,
                        closePositionAngle = value
                    };
                    Logger.Info($"Setting close position angle to {value}");
                    _ = CoverSetConfig(_uniqueId, config);
                    RaisePropertyChanged(nameof(ClosePositionAngle));
                } catch (Exception ex) {
                    Logger.Error($"WandererCover ClosePositionAngle setter crashed: {ex}");
                    DisconnectOnRemovedError();
                }
            }
        }

        public float CurrentPositionAngle {
            get {
                if (!Connected) {
                    return float.NaN;
                }

                try {
                    var err = CoverGetStatus(_uniqueId, out var status);
                    if (err == WC_ERROR_TYPE.WC_SUCCESS) {
                        return status.currentPositionAngle;
                    } else {
                        if (err == WC_ERROR_TYPE.WC_ERROR_COMMUNICATION) {
                            Logger.Error($"WandererCover communication error to get current position angle {err}");
                            DisconnectOnRemovedError();
                        } else {
                            Logger.Error($"WandererCover error to get current position angle {err}");
                        }
                        return float.NaN;
                    }
                } catch (Exception ex) {
                    Logger.Error($"WandererCover CurrentPositionAngle crashed: {ex}");
                    DisconnectOnRemovedError();
                    return float.NaN;
                }
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