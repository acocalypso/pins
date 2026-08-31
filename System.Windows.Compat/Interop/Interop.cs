#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace System.Windows.Interop {
    /// <summary>
    /// Minimal stub for PresentationSource interop operations
    /// </summary>
    internal static class Interop {
    }

    /// <summary>
    /// Specifies the render mode preference for the process.
    /// </summary>
    public enum RenderMode {
        Default = 0,
        SoftwareOnly = 1
    }
}
