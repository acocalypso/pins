#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Profile.Interfaces;
using System;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    internal class RotatorSettings : Settings, IRotatorSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        protected override void SetDefaultValues() {
            id = "No_Device";
            lastDeviceName = string.Empty;
            reverse2 = false;
            rangeType = RotatorRangeTypeEnum.FULL;
            rangeStartMechanicalPosition = 0.0f;
            overshoot = false;
            overshootDirection = false;
            overshootAngle = 0.0f;
            indiConnectionMode = "CONNECTION_SERIAL";
            indiPort = "/dev/ttyUSB0";
            indiBaudRate = 9600;
            indiAutoSearch = true;
            indiAddress = "localhost";
            indiDriver = "None";
        }

        private string id;

        [DataMember]
        public string Id {
            get => id;
            set {
                if (id != value) {
                    id = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string lastDeviceName;

        [DataMember]
        public string LastDeviceName {
            get => lastDeviceName;
            set {
                if (lastDeviceName != value) {
                    lastDeviceName = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool reverse2;
        [DataMember]
        /// <summary>
        /// Historically N.I.N.A. was expressing rotation in clockwise orientation
        /// As this was changed to follow the standard of counter clockwise orientation, the reverse setting is flipped for migration purposes
        /// </summary>
        public bool Reverse2 {
            get => reverse2;
            set {
                if (reverse2 != value) {
                    reverse2 = value;
                    RaisePropertyChanged();
                }
            }
        }

        private RotatorRangeTypeEnum rangeType;

        [DataMember]
        public RotatorRangeTypeEnum RangeType {
            get => rangeType;
            set {
                if (rangeType != value) {
                    rangeType = value;
                    RaisePropertyChanged();
                }
            }
        }

        private float rangeStartMechanicalPosition;

        [DataMember]
        public float RangeStartMechanicalPosition {
            get => rangeStartMechanicalPosition;
            set {
                if (rangeStartMechanicalPosition != value) {
                    rangeStartMechanicalPosition = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool overshoot;

        [DataMember]
        public bool Overshoot {
            get => overshoot;
            set {
                if (overshoot != value) {
                    overshoot = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool overshootDirection;

        [DataMember]
        public bool OvershootDirection {
            get => overshootDirection;
            set {
                if (overshootDirection != value) {
                    overshootDirection = value;
                    RaisePropertyChanged();
                }
            }
        }

        private float overshootAngle;

        [DataMember]
        public float OvershootAngle {
            get => overshootAngle;
            set {
                if (overshootAngle != value) {
                    overshootAngle = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string indiConnectionMode;
        [DataMember]
        public string IndiConnectionMode {
            get => indiConnectionMode;
            set {
                if (indiConnectionMode != value) {
                    indiConnectionMode = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string indiPort;
        [DataMember]
        public string IndiPort {
            get => indiPort;
            set {
                if (indiPort != value) {
                    indiPort = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int indiBaudRate;
        [DataMember]
        public int IndiBaudRate {
            get => indiBaudRate;
            set {
                if (indiBaudRate != value) {
                    indiBaudRate = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string indiDriver;
        [DataMember]
        public string IndiDriver {
            get => indiDriver;
            set {
                if (indiDriver != value) {
                    indiDriver = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool indiAutoSearch;
        [DataMember]
        public bool IndiAutoSearch {
            get => indiAutoSearch;
            set {
                if (indiAutoSearch != value) {
                    indiAutoSearch = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string indiAddress;
        [DataMember]
        public string IndiAddress {
            get => indiAddress;
            set {
                if (indiAddress != value) {
                    indiAddress = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}
