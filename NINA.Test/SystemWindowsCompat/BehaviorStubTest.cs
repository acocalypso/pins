#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Microsoft.Xaml.Behaviors;
using NUnit.Framework;
using System.Windows;

namespace NINA.Test.SystemWindowsCompat {

    [TestFixture]
    public class BehaviorStubTest {

        // Mirrors the shape of every production behavior (DragDropBehavior, BubbleScrollEvent,
        // ...): override OnAttached/OnDetaching and read the typed AssociatedObject.
        private sealed class TestBehavior : Behavior<FrameworkElement> {
            public int AttachedCalls;
            public int DetachingCalls;
            public FrameworkElement SeenOnAttach;

            protected override void OnAttached() {
                AttachedCalls++;
                SeenOnAttach = AssociatedObject;
            }

            protected override void OnDetaching() {
                DetachingCalls++;
            }
        }

        // Behavior<T> used to redeclare OnAttached/OnDetaching and AssociatedObject, hiding the
        // base members: Attach() never invoked derived overrides and the typed AssociatedObject
        // stayed null forever.
        [Test]
        public void Attach_InvokesDerivedOnAttached_AndExposesTypedAssociatedObject() {
            var element = new FrameworkElement();
            var behavior = new TestBehavior();

            behavior.Attach(element);

            Assert.That(behavior.AttachedCalls, Is.EqualTo(1));
            Assert.That(behavior.SeenOnAttach, Is.SameAs(element), "AssociatedObject must be set before OnAttached runs");
            Assert.That(behavior.AssociatedObject, Is.SameAs(element));
        }

        [Test]
        public void Detach_InvokesDerivedOnDetaching_AndClearsAssociatedObject() {
            var element = new FrameworkElement();
            var behavior = new TestBehavior();
            behavior.Attach(element);

            behavior.Detach();

            Assert.That(behavior.DetachingCalls, Is.EqualTo(1));
            Assert.That(behavior.AssociatedObject, Is.Null);
        }
    }
}
