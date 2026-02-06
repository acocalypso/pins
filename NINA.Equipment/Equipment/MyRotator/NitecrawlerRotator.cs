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
using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using static Nitecrawler.NitecrawlerSDK;


namespace NINA.Equipment.Equipment.MyRotator {
    public partial class NitecrawlerRotator : BaseINPC, IRotator {
        private readonly IProfileService _profileService;
        private readonly int _uniqueId;

        public NitecrawlerRotator(int id, uint serial, IProfileService profileService) {
            Id = $"{serial}";
            _profileService = profileService;
            _uniqueId = id;

            Name = $"Nitecrawler Rotator.{_uniqueId} ({serial})";
        }

        public bool IsMoving {
            get {
                if (!Connected) {
                    return false;
                }
                var err = GetRotatorStatus(_uniqueId, out var status);
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

        public bool CanReverse => true;
        public bool Reverse {
            get {
                if (!Connected) {
                    return false;
                }
                var err = GetRotatorConfig(_uniqueId, out var config);
                if (err == NC_ERROR_TYPE.NC_SUCCESS) {
                    return config.reverseDirection != 0;
                } else {
                    Logger.Error($"Nitecrawler error to get reverse direction state {err}");
                    return false;
                }
            }
            set {
                NC_ROTATOR_CONFIG config = new() {
                    mask = (uint)MASK_ROTATOR_REVERSE_DIRECTION,
                    reverseDirection = value ? 1 : 0
                };
                _ = SetRotatorConfig(_uniqueId, config);
                RaisePropertyChanged(nameof(Reverse));
            }
        }

        private bool synced;

        public bool Synced {
            get => synced;
            private set {
                synced = value;
                RaisePropertyChanged();
            }
        }

        private float offset = 0;

        public float Position => AstroUtil.EuclidianModulus(MechanicalPosition + offset, 360);

        public float MechanicalPosition {
            get {
                if (!Connected) {
                    return -1;
                }
                var err = GetRotatorStatus(_uniqueId, out var status);
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

        public void Sync(float skyAngle) {
            offset = skyAngle - MechanicalPosition;
            RaisePropertyChanged(nameof(Position));
            Synced = true;
            Logger.Debug($"Mechanical Position is {MechanicalPosition}° - Sync Position to Sky Angle {skyAngle}° using offset {offset}");
        }

        private void DisconnectOnRemovedError() {
            try {
                Notification.ShowWarning(Loc.Instance["LblRotatorConnectionLost"]);
                Logger.Error($"Nitecrawler device was removed");
                Disconnect();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public float StepSize {
            get {
                if (!Connected) {
                    return float.NaN;
                }
                var err = GetRotatorStatus(_uniqueId, out var status);
                if (err == NC_ERROR_TYPE.NC_SUCCESS) {
                    return status.stepSize;
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
        public string Id { get; }
        public string Name { get; }
        public string DisplayName => Name;

        public string Category => "Nitecrawler";

        [ObservableProperty]
        private bool connected = false;

        [RelayCommand]
        public void ResetPosition() {
            if (MyMessageBox.Show(Loc.Instance["LblZwoResetZeroPositionPrompt"], "", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {
                if (Position > 0) {
                    RotatorSyncPosition(_uniqueId, 0);
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
                    RotatorSyncPosition(_uniqueId, SyncPosition);
                    RaisePropertyChanged(nameof(Position));
                }
            }
        }

        public string Description => "Native driver for Nitecrawler Rotators";

        public string DriverInfo { get; private set; } = string.Empty;

        public string DriverVersion => GetSDKVersion();

        public Task<bool> Connect(CancellationToken token) {
            return Task.Run(() => {
                // Verify, the Rotator _uniqueId actually exists
                int[] ids = new int[NC_MAX_NUM];
                ScanRotators(out var count, ids);
                if (!ids.Take(count).Contains(_uniqueId)) {
                    Notification.ShowError(Loc.Instance["LblNitecrawlerRotatorNotAvailableError"]);
                    Logger.Error("Selected Nitecrawler Rotator not available (disconnected?)");
                    return false;
                }
                if (RotatorOpen(_uniqueId) == NC_ERROR_TYPE.NC_SUCCESS) {
                    DriverInfo = $"SDK: {DriverVersion}; FW: {GetFwVersionString()}";

                    GetRotatorConfig(_uniqueId, out var config);

                    Connected = true;
                    return true;
                } else {
                    Logger.Error("Failed to connect to Nitecrawler Rotator");
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
            _ = RotatorClose(_uniqueId);
            Connected = false;
        }

        public void Halt() {
            RotatorStopMove(_uniqueId);
        }

        public async Task<bool> Move(float angle, CancellationToken ct) {
            if(!Connected) {
                return false;
            }

            if (angle >= 360) {
                angle = AstroUtil.EuclidianModulus(angle, 360);
            }
            if (angle <= -360) {
                angle = AstroUtil.EuclidianModulus(angle, -360);
            }

            Logger.Debug($"Move relative by {angle}° - Mechanical Position reported by rotator {MechanicalPosition}° and offset {offset}");

            var err = RotatorMove(_uniqueId, angle);
            if (err != NC_ERROR_TYPE.NC_SUCCESS) {
                Logger.Error($"Nitecrawler failed to issue move command {err}");
                throw new Exception($"Failed to move Rotator {err}");
            }

            await CoreUtil.Wait(TimeSpan.FromMilliseconds(100), ct);
            NC_ROTATOR_STATUS status;
            do {
                err = GetRotatorStatus(_uniqueId, out status);

                if (err != NC_ERROR_TYPE.NC_SUCCESS) {
                    if (err == NC_ERROR_TYPE.NC_ERROR_COMMUNICATION) {
                        DisconnectOnRemovedError();
                    } else {
                        Logger.Error($"Nitecrawler error to get moving state {err}");
                    }

                    throw new Exception($"Rotator error {err}");
                }

                await CoreUtil.Wait(TimeSpan.FromMilliseconds(100), ct);
            } while (status.moving == 1 && !ct.IsCancellationRequested);

            return true;
        }

        public async Task<bool> MoveAbsoluteMechanical(float targetPosition, CancellationToken ct) {
            if (!Connected) {
                return false;
            }

            var movement = targetPosition - MechanicalPosition;
            return await Move(movement, ct);
        }

        public async Task<bool> MoveAbsolute(float targetPosition, CancellationToken ct) {
            if (!Connected) {
                return false;
            }

            return await Move(targetPosition - Position, ct);
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
