#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.CustomControlLibrary {
    // Dummy namespace for compatibility

    /// <summary>
    /// Dummy placeholder class to ensure namespace exists in compiled assembly
    /// </summary>
    internal static class CustomControlLibraryStub {
        // Empty placeholder
    }

    /// <summary>
    /// Headless stub for the WPF DetachingExpander control.
    /// </summary>
    public class DetachingExpander : System.Windows.FrameworkElement {
        public bool IsExpanded { get; set; }
        public object Header { get; set; }
        public object Content { get; set; }
    }
}
