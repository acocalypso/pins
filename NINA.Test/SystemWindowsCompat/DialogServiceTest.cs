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
using System.Windows;

namespace NINA.Test.SystemWindowsCompat {

    // DialogService is a static singleton — tests share its state and must not run
    // in parallel with each other or with anything else touching dialogs.
    [TestFixture]
    [NonParallelizable]
    public class DialogServiceTest {

        [SetUp]
        public void SetUp() {
            DialogService.CloseAllDialogs();
        }

        [TearDown]
        public void TearDown() {
            DialogService.CloseAllDialogs();
        }

        [Test]
        public void RegisterDialog_ReturnsUniqueIncreasingIds() {
            int first = DialogService.RegisterDialog("First", "message");
            int second = DialogService.RegisterDialog("Second", "message");

            Assert.That(first, Is.GreaterThan(0));
            Assert.That(second, Is.GreaterThan(first));
            Assert.That(DialogService.GetDialogCount(), Is.EqualTo(2));
        }

        [Test]
        public void GetDialog_ReturnsDefensiveCopy() {
            int id = DialogService.RegisterDialog("Original", "message");

            var copy = DialogService.GetDialog(id);
            Assert.That(copy, Is.Not.Null);
            copy!.Title = "Mutated";

            Assert.That(DialogService.GetDialog(id)!.Title, Is.EqualTo("Original"));
        }

        [Test]
        public void GetDialog_UnknownId_ReturnsNull() {
            Assert.That(DialogService.GetDialog(999999), Is.Null);
        }

        [Test]
        public void CloseDialog_InvokesResultCallback() {
            bool? result = null;
            int id = DialogService.RegisterDialog("Test", "message", resultCallback: r => result = r);

            bool closed = DialogService.CloseDialog(id, true);

            Assert.That(closed, Is.True);
            Assert.That(result, Is.True);
            Assert.That(DialogService.GetDialogCount(), Is.EqualTo(0));
        }

        // The result callback used to be invoked while _lock was held: a callback waiting on
        // another thread that itself calls into DialogService deadlocked until the wait timed
        // out. The callback must run outside the lock, like ClickButton's OnClick already does.
        [Test]
        public void CloseDialog_ResultCallbackRunsOutsideLock() {
            bool otherThreadCompleted = false;
            int id = DialogService.RegisterDialog("Test", "message", resultCallback: _ => {
                var worker = Task.Run(() => DialogService.GetDialogCount());
                otherThreadCompleted = worker.Wait(TimeSpan.FromSeconds(2));
            });

            bool closed = DialogService.CloseDialog(id);

            Assert.That(closed, Is.True);
            Assert.That(otherThreadCompleted, Is.True,
                "a thread calling into DialogService while the result callback runs must not block on _lock");
        }

        [Test]
        public void CloseDialog_UnknownId_ReturnsFalse() {
            Assert.That(DialogService.CloseDialog(999999), Is.False);
        }

        [Test]
        public void ClickButton_InvokesOnClick_AndClosesWithTrue() {
            bool clicked = false;
            bool? result = null;
            int id = DialogService.RegisterDialog("Confirm", "message", resultCallback: r => result = r);
            DialogService.AddButton(id, "ok", "OK", isDefault: true, onClick: () => clicked = true);

            bool found = DialogService.ClickButton(id, "OK");

            Assert.That(found, Is.True);
            Assert.That(clicked, Is.True);
            Assert.That(result, Is.True);
            Assert.That(DialogService.GetDialogCount(), Is.EqualTo(0));
        }

        [Test]
        public void ClickButton_CancelButton_ClosesWithFalse() {
            bool? result = null;
            int id = DialogService.RegisterDialog("Confirm", "message", resultCallback: r => result = r);
            DialogService.AddButton(id, "cancel", "Cancel", isCancel: true);

            DialogService.ClickButton(id, "cancel");

            Assert.That(result, Is.False);
        }

        [Test]
        public void ClickButton_MatchesCaseInsensitively_ByNameOrText() {
            int id = DialogService.RegisterDialog("Confirm", "message");
            DialogService.AddButton(id, "btnYes", "Yes");

            Assert.That(DialogService.ClickButton(id, "BTNYES"), Is.True);

            id = DialogService.RegisterDialog("Confirm", "message");
            DialogService.AddButton(id, "btnYes", "Yes");
            Assert.That(DialogService.ClickButton(id, "yes"), Is.True);
        }

        [Test]
        public void ClickButton_UnknownButton_ReturnsFalse_AndKeepsDialogOpen() {
            int id = DialogService.RegisterDialog("Confirm", "message");
            DialogService.AddButton(id, "ok", "OK");

            Assert.That(DialogService.ClickButton(id, "DoesNotExist"), Is.False);
            Assert.That(DialogService.GetDialogCount(), Is.EqualTo(1));
        }

        // REVIEW.md F18: ClickButton used instance .Equals(), which NREs if a caller (e.g. a
        // malformed SignalR request from the frontend) passes a null buttonName.
        [Test]
        public void ClickButton_NullButtonName_ReturnsFalse_DoesNotThrow() {
            int id = DialogService.RegisterDialog("Confirm", "message");
            DialogService.AddButton(id, "ok", "OK");

            Assert.That(() => DialogService.ClickButton(id, null), Throws.Nothing);
            Assert.That(DialogService.ClickButton(id, null), Is.False);
        }

        [Test]
        public void UpdateDialogMessage_UpdatesMessageAndContent() {
            int id = DialogService.RegisterDialog("Test", "before");

            DialogService.UpdateDialogMessage(id, "after");

            var dialog = DialogService.GetDialog(id);
            Assert.That(dialog!.Message, Is.EqualTo("after"));
            Assert.That(dialog.Content["Message"], Is.EqualTo("after"));
        }

        [Test]
        public void GetDialogsByType_FiltersBySubstring() {
            DialogService.RegisterDialog("A", "m", contentType: "SlewCenterDialog");
            DialogService.RegisterDialog("B", "m", contentType: "AutofocusDialog");

            var matches = DialogService.GetDialogsByType("slewcenter");

            Assert.That(matches, Has.Count.EqualTo(1));
            Assert.That(matches[0].Title, Is.EqualTo("A"));
        }

        [Test]
        public void CloseAllDialogs_ReturnsCountAndEmptiesRegistry() {
            DialogService.RegisterDialog("A", "m");
            DialogService.RegisterDialog("B", "m");
            DialogService.RegisterDialog("C", "m");

            int closed = DialogService.CloseAllDialogs();

            Assert.That(closed, Is.EqualTo(3));
            Assert.That(DialogService.GetDialogCount(), Is.EqualTo(0));
        }

        [Test]
        public void GetAllDialogs_StripsCallbacks_ButKeepsButtons() {
            int id = DialogService.RegisterDialog("Test", "m", resultCallback: _ => { });
            DialogService.AddButton(id, "ok", "OK", onClick: () => { });

            var all = DialogService.GetAllDialogs();

            Assert.That(all, Has.Count.EqualTo(1));
            Assert.That(all[0].ResultCallback, Is.Null);
            Assert.That(all[0].Buttons, Has.Count.EqualTo(1));
            Assert.That(all[0].Buttons[0].OnClick, Is.Null);
        }

        [Test]
        public void ConcurrentRegistrations_ProduceUniqueIds() {
            var ids = new System.Collections.Concurrent.ConcurrentBag<int>();

            Parallel.For(0, 100, i => {
                ids.Add(DialogService.RegisterDialog($"Dialog{i}", "m"));
            });

            Assert.That(ids.Distinct().Count(), Is.EqualTo(100));
            Assert.That(DialogService.GetDialogCount(), Is.EqualTo(100));
        }

        [Test]
        public void IsHeadless_IsTrueOnLinux() {
            if (!OperatingSystem.IsLinux()) {
                Assert.Ignore("Linux-only assertion");
            }
            Assert.That(DialogService.IsHeadless(), Is.True);
        }
    }
}
