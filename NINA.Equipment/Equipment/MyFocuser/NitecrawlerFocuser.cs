#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NINA.Core.Locale;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using static Nitecrawler.NitecrawlerSDK;


namespace NINA.Equipment.Equipment.MyFocuser {
    public partial class NitecrawlerFocuser : BaseINPC, IFocuser {
        private readonly IProfileService _profileService;
        private readonly int _uniqueId;
        private readonly static TimeSpan _sameFocuserPositionTimeout = TimeSpan.FromMinutes(1);

        public NitecrawlerFocuser(int id, uint serial, IProfileService profileService) {
            Id = $"{serial}";
            _profileService = profileService;
            _uniqueId = id;

            Name = $"Nitecrawler.Focuser.{_uniqueId} ({serial})";
        }

        public bool IsMoving {
            get {
                if (!Connected) {
                    return false;
                }
                var err = GetFocuserStatus(_uniqueId, out var status);
                if (err == NC_ERROR_TYPE.NC_SUCCESS) {
                    return status.moving == 1;
                } else {
                    if (err == NC_ERROR_TYPE.NC_ERROR_COMMUNICATION) {
                        Logger.Error($"Nitecrawler communication error to get moving state {err}");
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"Nitecrawler error to get moving state {err}");
                    }
                    return false;
                }
            }
        }

        public int MaxIncrement => MaxStep;

        public bool CanSetMaxStep => true;
        public int MaxStep {
            get {
                if (!Connected) {
                    return -1;
                }
                var err = GetFocuserConfig(_uniqueId, out var config);
                if (err == NC_ERROR_TYPE.NC_SUCCESS) {
                    return config.maxStep;
                } else {
                    Logger.Error($"Nitecrawler error to get MaxStep {err}");
                    return -1;
                }
            }
            set {
                NC_FOCUSER_CONFIG config = new() {
                    mask = (uint)MASK_FOCUSER_MAX_STEP,
                    maxStep = value
                };
                _ = SetFocuserConfig(_uniqueId, config);
                RaisePropertyChanged(nameof(MaxStep));
                RaisePropertyChanged(nameof(MaxIncrement));
            }
        }

        public bool CanReverse => false;
        public bool Reverse { get => false; set { } }

        public int Position {
            get {
                if (!Connected) {
                    return -1;
                }
                var err = GetFocuserStatus(_uniqueId, out var status);
                if (err == NC_ERROR_TYPE.NC_SUCCESS) {
                    return status.position;
                } else {
                    if (err == NC_ERROR_TYPE.NC_ERROR_COMMUNICATION) {
                        Logger.Error($"Nitecrawler communication error to get Position state {err}");
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"Nitecrawler error to get Position {err}");
                    }
                    return -1;
                }
            }
        }

        private void DisconnectOnRemovedError() {
            try {
                Notification.ShowWarning(Loc.Instance["LblFocuserConnectionLost"]);
                Logger.Error($"Nitecrawler device was removed");
                Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public double StepSize {
            get {
                if (!Connected) {
                    return float.NaN;
                }
                var err = GetFocuserStatus(_uniqueId, out var status);
                if (err == NC_ERROR_TYPE.NC_SUCCESS) {
                    return status.micronsPerStep;
                } else {
                    if (err == NC_ERROR_TYPE.NC_ERROR_COMMUNICATION) {
                        Logger.Error($"Nitecrawler communication error to get step size state {err}");
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"Nitecrawler error to get Position {err}");
                    }
                    return -1;
                }
            }
        }

        public double Temperature {
            get {
                if (!Connected) {
                    return double.NaN;
                }
                var err = GetFocuserStatus(_uniqueId, out var status);
                if (err == NC_ERROR_TYPE.NC_SUCCESS) {
                    if (status.temperatureDetection == 0 || (uint)status.temperatureExt == TEMPERATURE_INVALID) {
                        // Ambient probe not connected
                        return double.NaN;
                    } else {
                        return status.temperatureExt / 100.0;
                    }
                } else {
                    if (err == NC_ERROR_TYPE.NC_ERROR_COMMUNICATION) {
                        Logger.Error($"Nitecrawler communication error to get Temperature {err}");
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"Nitecrawler error to get Temperature {err}");
                    }
                    return double.NaN;
                }
            }
        }

        public string Id { get; }
        public string Name { get; }
        public string DisplayName => Name;

        public string Category => "Nitecrawler";

        [ObservableProperty]
        private bool connected = false;

        [ObservableProperty]
        private int targetMaxStep;

        partial void OnTargetMaxStepChanged(int value) {
            NC_FOCUSER_CONFIG config = new NC_FOCUSER_CONFIG();
            config.mask = (uint)MASK_FOCUSER_MAX_STEP;
            config.maxStep = value;
            _ = SetFocuserConfig(_uniqueId, config);
            RaisePropertyChanged(nameof(MaxStep));
            RaisePropertyChanged(nameof(MaxIncrement));
        }

        [RelayCommand]
        public void ResetPosition() {
            if (MyMessageBox.Show(Loc.Instance["LblZwoResetZeroPositionPrompt"], "", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {
                if (Position > 0) {
                    FocuserSyncPosition(_uniqueId, 0);
                    RaisePropertyChanged(nameof(Position));
                }
            }
        }

        private int syncPosition;
        public int SyncPosition {
            get => syncPosition;
            set {
                if (syncPosition != value) {
                    syncPosition = value;
                    RaisePropertyChanged();
                }
            }
        }

        [RelayCommand]
        public void SyncToPosition() {
            if (MyMessageBox.Show(Loc.Instance["LblNitecrawlerSyncPositionPrompt"], "", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {
                if (Position != SyncPosition) {
                    FocuserSyncPosition(_uniqueId, SyncPosition);
                    RaisePropertyChanged(nameof(Position));
                }
            }
        }

        public string Description => "Native driver for Nitecrawler focusers";

        public string DriverInfo { get; private set; } = string.Empty;

        public string DriverVersion => GetSDKVersion();

        public Task<bool> Connect(CancellationToken token) {
            return Task.Run(() => {
                // Verify, the focuser _uniqueId actually exists
                int[] ids = new int[NC_MAX_NUM];
                ScanFocusers(out var count, ids);
                if (!ids.Take(count).Contains(_uniqueId)) {
                    Notification.ShowError(Loc.Instance["LblNitecrawlerFocuserNotAvailableError"]);
                    Logger.Error("Selected Nitecrawler focuser not available (disconnected?)");
                    return false;
                }
                if (FocuserOpen(_uniqueId) == NC_ERROR_TYPE.NC_SUCCESS) {
                    DriverInfo = $"SDK: {DriverVersion}; FW: {GetFwVersionString()}";

                    GetFocuserConfig(_uniqueId, out var config);
                    TargetMaxStep = config.maxStep;

                    RaisePropertyChanged(nameof(MaxStep));

                    Connected = true;
                    return true;
                } else {
                    Logger.Error("Failed to connect to Nitecrawler focuser");
                    return false;
                }
            }, token);
        }

        private string GetFwVersionString() {
            NC_VERSION version = new();
            _ = GetVersion(_uniqueId, out version);

            uint major = (version.firmware >> 24) & 0xFF;
            uint minor = (version.firmware >> 16) & 0xFF;
            uint patch = (version.firmware >> 8) & 0xFF;

            return $"{major}.{minor}.{patch}";
        }

        public void Disconnect() {
            _ = FocuserClose(_uniqueId);
            Connected = false;
        }

        public void Halt() {
            FocuserStopMove(_uniqueId);
        }

        public async Task Move(int position, CancellationToken ct, int waitInMs = 1000) {
            var lastPosition = int.MinValue;
            int samePositionCount = 0;
            var lastMovementTime = DateTime.Now;
            while (position != Position && !ct.IsCancellationRequested) {
                // Issue move command
                var err = FocuserMoveTo(_uniqueId, position);
                if (err != NC_ERROR_TYPE.NC_SUCCESS) {
                    Logger.Error($"Nitecrawler failed to issue move command {err}");
                    throw new Exception($"Failed to move focuser {err}");
                }

                await CoreUtil.Wait(TimeSpan.FromMilliseconds(100), ct);
                NC_FOCUSER_STATUS status;
                do {
                    err = GetFocuserStatus(_uniqueId, out status);

                    if (err != NC_ERROR_TYPE.NC_SUCCESS) {
                        if (err == NC_ERROR_TYPE.NC_ERROR_COMMUNICATION) {
                            DisconnectOnRemovedError();
                        } else {
                            Logger.Error($"Nitecrawler error to get moving state {err}");
                        }

                        throw new Exception($"Focuser error {err}");
                    }

                    await CoreUtil.Wait(TimeSpan.FromMilliseconds(100), ct);
                } while (status.moving == 1 && !ct.IsCancellationRequested);

                if (lastPosition == Position) {
                    ++samePositionCount;
                    var samePositionTime = DateTime.Now - lastMovementTime;
                    if (samePositionTime >= _sameFocuserPositionTimeout) {
                        throw new Exception($"Focuser stuck at position {lastPosition} beyond {_sameFocuserPositionTimeout} timeout");
                    }

                    // Make sure we wait in between Move requests when no progress is being made
                    // to avoid spamming the driver and spiking the CPU
                    await CoreUtil.Wait(TimeSpan.FromSeconds(1), ct);
                } else {
                    lastMovementTime = DateTime.Now;
                }

                lastPosition = status.position;
            }
        }

        #region Unsupported
        public bool TempCompAvailable => false;
        public bool TempComp { get; set; }
        public bool HasSetupDialog => false;

        public IList<string> SupportedActions => new List<string>();
        public void SetupDialog() {
        }

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }
        public void SendCommandBlind(string command, bool raw = true) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw = true) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw = true) {
            throw new NotImplementedException();
        }
        #endregion
    }
}
