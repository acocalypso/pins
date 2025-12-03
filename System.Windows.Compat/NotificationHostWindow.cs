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
    /// Notification host window for displaying notifications (stub for headless mode)
    /// </summary>
    public class NotificationHostWindow : Window {
        public void Reposition() {
            // No-op in headless mode
        }
    }
}
