#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace System.Windows.Threading {
    // Minimal stub for DispatcherOperationStatus
    public enum DispatcherOperationStatus {
        Pending,
        Aborted,
        Completed,
        Executing
    }

    // Minimal stub for DispatcherOperation
    public class DispatcherOperation {
        public Dispatcher Dispatcher { get; set; } = Dispatcher.CurrentDispatcher;
        public DispatcherPriority Priority { get; set; } = DispatcherPriority.Normal;
        public System.Threading.Tasks.Task Task { get; set; } = System.Threading.Tasks.Task.CompletedTask;
        public object Result { get; set; }
        public event System.EventHandler Aborted;
        public event System.EventHandler Completed;

        // Reflects the underlying Task's actual state instead of a value fixed once at creation
        // time - the backing Task.Run may still be executing when the operation is created.
        public DispatcherOperationStatus Status =>
            Task.IsCompleted ? DispatcherOperationStatus.Completed : DispatcherOperationStatus.Executing;

        public bool Abort() { return true; }

        public DispatcherOperationStatus Wait() {
            try {
                Task.Wait();
            } catch (System.AggregateException) {
                // Match WPF: Wait() reports completion status, it doesn't rethrow the callback's exception.
            }
            return Status;
        }

        public DispatcherOperationStatus Wait(System.TimeSpan timeout) {
            try {
                Task.Wait(timeout);
            } catch (System.AggregateException) {
            }
            return Status;
        }

        public System.Runtime.CompilerServices.TaskAwaiter GetAwaiter() => Task.GetAwaiter();
    }

    /// <summary>
    /// Stub implementation of DispatcherFrame for headless execution
    /// </summary>
    public class DispatcherFrame {
        /// <summary>
        /// Gets or sets a value indicating whether this frame should continue processing
        /// </summary>
        public bool Continue { get; set; } = true;
    }
}
