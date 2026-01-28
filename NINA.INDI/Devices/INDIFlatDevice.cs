#region "copyright"

/*
    Copyright © 2025-2026 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.INDI.Enums;
using NINA.INDI.Protocol;
using NINA.INDI.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;
using System.Linq;

namespace NINA.INDI.Devices
{

    public class INDIFlatDevice : INDIDevice, IINDIFlatDevice
    {
        public override void OnTextPropertyUpdated(INDITextProperty p)
        {
            base.OnTextPropertyUpdated(p);
        }

        public override void OnNumberPropertyUpdated(INDINumberProperty p)
        {
            base.OnNumberPropertyUpdated(p);
        }

        public override void OnSwitchPropertyUpdated(INDISwitchProperty p)
        {
            base.OnSwitchPropertyUpdated(p);

            if (p.Name == "CAP_PARK" && p.State != PropertyState.Busy)
            {
                var sw = GetSwitchProperty("CAP_PARK");
                coverState = (sw?.Switches.FirstOrDefault(s => s.Name == "PARK")?.Value ?? true) ? CoverState.Closed : CoverState.Open;
            }
        }

        public override void OnBlobPropertyUpdated(INDIBlobProperty p)
        {
            base.OnBlobPropertyUpdated(p);
        }





        public INDIFlatDevice(INDIDeviceInfo device) : base(device)
        {
        }

        /// <summary>
        /// Specify critical properties that must arrive before Connect() completes
        /// </summary>
        protected override string[] GetRequiredConnectionProperties()
        {
            return ["FLAT_LIGHT_INTENSITY"];
        }

        private CoverState coverState = CoverState.Unknown;
        public CoverState CoverState => SupportsOpenClose ? coverState : CoverState.NotPresent;

        public int MaxBrightness
        {
            get
            {
                var prop = GetNumberProperty("FLAT_LIGHT_INTENSITY");
                return (int)(prop?.Numbers.FirstOrDefault(n => n.Name == "FLAT_LIGHT_INTENSITY_VALUE")?.Max ?? 0);
            }
        }

        public int MinBrightness
        {
            get
            {
                var prop = GetNumberProperty("FLAT_LIGHT_INTENSITY");
                return (int)(prop?.Numbers.FirstOrDefault(n => n.Name == "FLAT_LIGHT_INTENSITY_VALUE")?.Min ?? 0);
            }
        }

        public bool SupportsOpenClose
        {
            get
            {
                var prop = GetSwitchProperty("CAP_PARK");
                return prop != null;
            }
        }

        public bool IsParked => GetSwitchPropertyValue("CAP_PARK", "PARK") ?? false;

        public async Task<bool> Open(CancellationToken ct, int delay = 300)
        {
            if (!Connected || !SupportsOpenClose)
            {
                return false;
            }

            // Initiate the move
            coverState = CoverState.NeitherOpenNorClosed;
            SetSwitchValue("CAP_PARK", "UNPARK", true);
            Logger.Info("Commanded flat device to unpark");

            while (coverState == CoverState.NeitherOpenNorClosed && !ct.IsCancellationRequested)
            {
                await Task.Delay(delay, ct);
            }

            Logger.Debug($"FlatDevice reached unpark position");

            return coverState == CoverState.Open;
        }

        public async Task<bool> Close(CancellationToken ct, int delay = 300)
        {
            if (!Connected || !SupportsOpenClose)
            {
                return false;
            }

            // Initiate the move
            coverState = CoverState.NeitherOpenNorClosed;
            SetSwitchValue("CAP_PARK", "PARK", true);
            Logger.Info("Commanded flat device to park");

            while (coverState == CoverState.NeitherOpenNorClosed && !ct.IsCancellationRequested)
            {
                await Task.Delay(delay, ct);
            }

            Logger.Debug($"FlatDevice reached park position");

            return coverState == CoverState.Closed;
        }

        public bool SupportsOnOff
        {
            get
            {
                var prop = GetSwitchProperty("FLAT_LIGHT_CONTROL");
                return prop != null;
            }
        }

        public bool LightOn
        {
            get => GetSwitchPropertyValue("FLAT_LIGHT_CONTROL", "FLAT_LIGHT_ON") ?? false;
            set
            {
                if (SupportsOnOff && Connected)
                {
                    try
                    {
                        if (value)
                        {
                            SetSwitchValue("FLAT_LIGHT_CONTROL", "FLAT_LIGHT_ON", true);
                        }
                        else
                        {
                            SetSwitchValue("FLAT_LIGHT_CONTROL", "FLAT_LIGHT_OFF", true);
                        }
                    }
                    catch (ArgumentException)
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        public bool CanSetBrightness
        {
            get
            {
                var prop = GetNumberPropertyValue("FLAT_LIGHT_INTENSITY", "FLAT_LIGHT_INTENSITY_VALUE");
                return prop != null;
            }
        }
        
        public int Brightness
        {
            get => (int)GetNumberPropertyValue("FLAT_LIGHT_INTENSITY", "FLAT_LIGHT_INTENSITY_VALUE");
            set
            {
                if (CanSetBrightness && Connected)
                {
                    try
                    {
                        SetNumberValue("FLAT_LIGHT_INTENSITY", "FLAT_LIGHT_INTENSITY_VALUE", value);
                        Logger.Info($"Set brightness to {value}");
                    }
                    catch (ArgumentException)
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        #region Unsupported

        public IList<string> SupportedActions { get; }

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public void CommandBlind(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        public bool CommandBool(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        public string CommandString(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        #endregion
    }
}