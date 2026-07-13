#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NUnit.Framework;
using System.Reflection;

namespace NINA.Test.SystemWindowsCompat {

    /// <summary>
    /// System.Windows.Compat reaches upward into NINA.Core via stringly-typed reflection
    /// (see REVIEW.md F19). These tests pin those contracts so a rename in NINA.Core
    /// fails a test instead of silently breaking dialog broadcasts at runtime.
    /// </summary>
    [TestFixture]
    public class ReflectionContractsTest {

        [Test]
        public void DialogBroadcaster_TypeAndMembers_Resolve() {
            // Exactly the lookup DialogService.CloseDialog performs
            var type = Type.GetType("NINA.Core.SignalR.DialogBroadcaster, NINA.Core");

            Assert.That(type, Is.Not.Null,
                "DialogService.CloseDialog reflects on this type; renaming it breaks dialog-clear broadcasts");

            var instanceProperty = type!.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            Assert.That(instanceProperty, Is.Not.Null,
                "DialogService.CloseDialog requires a public static 'Instance' property");

            var clearMethod = type.GetMethod("ClearDialogAsync");
            Assert.That(clearMethod, Is.Not.Null,
                "DialogService.CloseDialog requires a public 'ClearDialogAsync' method");
            Assert.That(clearMethod!.ReturnType.IsAssignableTo(typeof(Task)), Is.True,
                "DialogService awaits the result as a Task");
        }

        [Test]
        public void WindowService_CleanupEventHandlersForContent_Resolves() {
            // Exactly the lookup DialogService.CleanupEventHandlers performs
            var type = Type.GetType("NINA.Core.Utility.WindowService.WindowService, NINA.Core");

            Assert.That(type, Is.Not.Null,
                "DialogService.CleanupEventHandlers reflects on this type");

            var method = type!.GetMethod("CleanupEventHandlersForContent",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null,
                "DialogService requires a public static 'CleanupEventHandlersForContent' method");

            var parameters = method!.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(1),
                "DialogService invokes it with a single content argument");
        }

        [Test]
        public void CompatAssembly_IsNamedSystemWindows() {
            // The whole layer relies on masquerading as System.Windows. The version must stay
            // ABOVE the Microsoft.NETCore.App type-forwarder facade (4.0.0.0), or assembly
            // resolution prefers the facade and no compat type loads (framework-dependent hosts).
            var assembly = typeof(System.Windows.DialogService).Assembly;
            var name = assembly.GetName();

            Assert.That(name.Name, Is.EqualTo("System.Windows"));
            Assert.That(name.Version, Is.GreaterThan(new Version(4, 0, 0, 0)));
        }
    }
}
