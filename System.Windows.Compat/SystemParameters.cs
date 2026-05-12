#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace System.Windows {
    /// <summary>
    /// Stub for WPF SystemParameters
    /// </summary>
    public static class SystemParameters {
        /// <summary>
        /// Gets the work area of the primary display monitor
        /// </summary>
        public static Rect WorkArea {
            get {
                // Return a reasonable default work area
                // In a real WPF app, this would query the system
                return new Rect(0, 0, 1920, 1080);
            }
        }
    }

    /// <summary>
    /// Stub for System.Windows.Clipboard – clipboard operations are no-ops on Linux.
    /// </summary>
    public static class Clipboard {
        private static string _text = string.Empty;

        public static void SetText(string text) {
            _text = text ?? string.Empty;
        }

        public static string GetText() => _text;

        public static bool ContainsText() => !string.IsNullOrEmpty(_text);

        public static void Clear() => _text = string.Empty;
    }
}
