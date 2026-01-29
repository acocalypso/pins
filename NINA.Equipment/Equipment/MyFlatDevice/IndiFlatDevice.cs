#region "copyright"

/*
    Copyright © 2025-2026 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.INDI;
using NINA.INDI.Devices;
using NINA.INDI.Interfaces;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyFlatDevice {

    public class IndiFlatDevice : IndiDevice<IINDIFlatDevice>, IFlatDevice, IDisposable {

        public IndiFlatDevice(INDIDeviceInfo info) : base(info) {
        }

        private int lastBrightness = 0;

        public CoverState CoverState {
            get {
                var state = GetProperty(nameof(CoverState), INDI.Enums.CoverState.Unknown);
                switch (state) {
                    case INDI.Enums.CoverState.Unknown:
                        return CoverState.Unknown;

                    case INDI.Enums.CoverState.NotPresent:
                        return CoverState.NotPresent;

                    case INDI.Enums.CoverState.NeitherOpenNorClosed:
                        return CoverState.NeitherOpenNorClosed;

                    case INDI.Enums.CoverState.Closed:
                        return CoverState.Closed;

                    case INDI.Enums.CoverState.Open:
                        return CoverState.Open;

                    case INDI.Enums.CoverState.Error:
                        return CoverState.Error;

                    default:
                        return CoverState.Unknown;
                }
            }
        }

        public int MaxBrightness { get; private set; }

        public int MinBrightness { get; private set; }

        public bool LightOn {
            get => device.LightOn;
            set {
                try {
                    if (SupportsOnOff && (CoverState == CoverState.Closed || CoverState == CoverState.NotPresent)) {
                        if (value) {
                            Logger.Debug("Switching cover calibrator on");
                            // switch the light on with the last saved value, if any
                            device.Brightness = (lastBrightness != 0) ? lastBrightness : MaxBrightness;
                            device.LightOn = true;
                        } else {
                            Logger.Debug("Switching cover calibrator off");
                            device.LightOn = false;
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
            }
        }

        public int Brightness {
            get => device.Brightness;
            set {
                try {
                    if (SupportsOnOff) {
                        if (value < MinBrightness) {
                            value = MinBrightness;
                        }

                        if (value > MaxBrightness) {
                            value = MaxBrightness;
                        }
                        Logger.Debug($"Setting cover calibrator brightness to {value}");
                        device.Brightness = value;
                        lastBrightness = value; // save brightness for next time the user toggles the light on
                        device.LightOn = value != 0;
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
            }
        }

        public string PortName { get => string.Empty; set { } }

        public bool SupportsOpenClose => device.SupportsOpenClose;

        public bool SupportsOnOff => device.SupportsOnOff;

        protected override string ConnectionLostMessage => Loc.Instance["LblFlatDeviceConnectionLost"];

        private void Initialize() {
            if (device.SupportsOnOff == false) {
                MinBrightness = 0;
                MaxBrightness = 0;
            } else {
                try {
                    MinBrightness = 0;
                    MaxBrightness = device.MaxBrightness;
                } catch {
                    MinBrightness = 0;
                    MaxBrightness = 0;
                }
            }
        }

        public async Task<bool> Open(CancellationToken ct, int delay = 300) {
            if (SupportsOpenClose) {
                if (CoverState == CoverState.Error)
                {
                    throw new Exception();
                }

                // Turn off lights before opening
                LightOn = false;

                await device.Open(ct);
                if (CoverState == CoverState.Error) {
                    throw new Exception();
                }
            }
            return CoverState == CoverState.Open;
        }

        public async Task<bool> Close(CancellationToken ct, int delay = 300) {
            if (SupportsOpenClose) {
                if (CoverState == CoverState.Error) {
                    throw new Exception();
                }

                await device.Close(ct);
                if (CoverState == CoverState.Error) {
                    throw new Exception();
                }
            }
            return CoverState == CoverState.Closed;
        }

        protected override Task PreConnect() {
            lastBrightness = 0;
            return base.PreConnect();
        }

        protected override Task PostConnect() {
            Initialize();
            return Task.CompletedTask;
        }

        protected override IINDIFlatDevice GetInstance() {
            return device ??= new INDIFlatDevice(_device);
        }
    }
}