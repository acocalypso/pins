#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Drawing;

namespace System.Windows.Forms {
    /// <summary>
    /// Screen information stub for compatibility
    /// On Linux, we provide basic functionality
    /// </summary>
    public class Screen {
        private static Screen _primaryScreen;

        public static Screen PrimaryScreen {
            get {
                if (_primaryScreen == null) {
                    _primaryScreen = new Screen();
                }
                return _primaryScreen;
            }
        }

        public Rectangle Bounds { get; private set; }

        private Screen() {
            // Try to get screen resolution from environment or use defaults
            // On Linux, we'd typically use X11 or Wayland APIs, but for simplicity we'll use defaults
            int width = 1920;  // Default width
            int height = 1080; // Default height

            // Try to get from DISPLAY resolution if available
            try {
                // This is a simplified approach - actual implementation would query X11/Wayland
                var displayEnv = Environment.GetEnvironmentVariable("DISPLAY");
                if (!string.IsNullOrEmpty(displayEnv)) {
                    // Could parse xrandr output or use X11 libraries here
                    // For now, use defaults
                }
            } catch {
                // Fallback to defaults
            }

            Bounds = new Rectangle(0, 0, width, height);
        }

        public static Screen[] AllScreens {
            get {
                return new Screen[] { PrimaryScreen };
            }
        }
    }

    /// <summary>
    /// Represents a dialog result value.
    /// </summary>
    public enum DialogResult {
        None = 0,
        OK = 1,
        Cancel = 2,
        Abort = 3,
        Retry = 4,
        Ignore = 5,
        Yes = 6,
        No = 7
    }

    /// <summary>
    /// Stub for FolderBrowserDialog – on Linux uses a console fallback.
    /// </summary>
    public class FolderBrowserDialog : IDisposable {
        public string SelectedPath { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool ShowNewFolderButton { get; set; } = true;

        public DialogResult ShowDialog() {
            // On Linux there is no native WinForms dialog; return Cancel so callers degrade gracefully.
            return DialogResult.Cancel;
        }

        public void Dispose() { }
    }
}
