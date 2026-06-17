#region "copyright"

/*
    Copyright © 2025-2026 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Generic;

namespace NINA.INDI.Protocol {

    /// <summary>
    /// Immutable, JSON-friendly snapshot of a single INDI device and all of its currently
    /// known properties. Built under lock by <see cref="INDIClient"/> so consumers (e.g. the
    /// Touch-N-Stars INDI control panel) can serialize it without racing the receive thread.
    /// </summary>
    public class INDIDeviceSnapshot {
        public string Device { get; set; } = string.Empty;
        public string DriverExec { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int Interface { get; set; }
        public bool Connected { get; set; }
        public List<INDIPropertySnapshot> Properties { get; set; } = new();
    }

    /// <summary>
    /// Snapshot of one INDI property vector (number/switch/text/light/blob) with its elements.
    /// </summary>
    public class INDIPropertySnapshot {
        public string Device { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        /// <summary>number, switch, text, light or blob.</summary>
        public string Type { get; set; } = string.Empty;
        /// <summary>Idle, Ok, Busy or Alert.</summary>
        public string State { get; set; } = string.Empty;
        /// <summary>ro, wo or rw.</summary>
        public string Permission { get; set; } = string.Empty;
        /// <summary>For switch vectors: OneOfMany, AtMostOne or AnyOfMany. Empty otherwise.</summary>
        public string Rule { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public List<INDIElementSnapshot> Elements { get; set; } = new();
    }

    /// <summary>
    /// Snapshot of one element inside a property vector. <see cref="Value"/> carries the
    /// type-appropriate boxed value (double for numbers, bool for switches, string for text,
    /// the light state string for lights). Min/Max/Step/Format only apply to numbers.
    /// </summary>
    public class INDIElementSnapshot {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public object Value { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public double? Step { get; set; }
        public string Format { get; set; }
    }
}
