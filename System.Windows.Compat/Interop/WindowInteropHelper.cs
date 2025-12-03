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
    /// Helper for interop operations with Window objects
    /// </summary>
    public class WindowInteropHelper {
        private Window _window;

        public WindowInteropHelper(Window window) {
            _window = window;
        }

        /// <summary>
        /// Gets the window handle (HWND) - returns IntPtr.Zero in headless mode
        /// </summary>
        public IntPtr Handle => IntPtr.Zero;
    }
}
