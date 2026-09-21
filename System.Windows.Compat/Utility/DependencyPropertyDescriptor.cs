#region "copyright"

/*
    Copyright © 2026 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace System.ComponentModel {
    /// <summary>
    /// WPF builds these from the registered dependency property table, which the compat
    /// DependencyProperty stub does not maintain. Every descriptor therefore reports
    /// "not a dependency property", which is what a headless run without a binding engine is.
    /// </summary>
    public sealed class DependencyPropertyDescriptor {
        private DependencyPropertyDescriptor(System.Windows.DependencyProperty dependencyProperty) {
            DependencyProperty = dependencyProperty;
        }

        public System.Windows.DependencyProperty DependencyProperty { get; }

        public static DependencyPropertyDescriptor FromProperty(PropertyDescriptor property) => null;

        public static DependencyPropertyDescriptor FromProperty(System.Windows.DependencyProperty dependencyProperty, Type targetType) => null;

        public static DependencyPropertyDescriptor FromName(string name, Type ownerType, Type targetType) => null;
    }
}
