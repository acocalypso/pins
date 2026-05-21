#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace System.Windows.Navigation {
    /// <summary>
    /// Event args for the RequestNavigate routed event raised by Hyperlink (stub).
    /// </summary>
    public class RequestNavigateEventArgs : System.Windows.RoutedEventArgs {
        public Uri Uri { get; }
        public string Target { get; }
        public RequestNavigateEventArgs(Uri uri, string target) {
            Uri = uri;
            Target = target;
        }
    }

    /// <summary>Delegate for RequestNavigate events.</summary>
    public delegate void RequestNavigateEventHandler(object sender, RequestNavigateEventArgs e);
}
