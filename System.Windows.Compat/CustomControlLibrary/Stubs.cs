#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.CustomControlLibrary {
    // Dummy namespace for compatibility

    /// <summary>
    /// Dummy placeholder class to ensure namespace exists in compiled assembly
    /// </summary>
    internal static class CustomControlLibraryStub {
        // Empty placeholder
    }

    /// <summary>
    /// Step direction for IntStepperControl. Not a stub - the underlying values are copied from
    /// upstream because callers cast them to int and use the result as the step amount
    /// (FramingAssistantTimeContext.Adjust), so changing them changes behavior.
    /// </summary>
    public enum StepDirection {
        Decrement = -1,
        Increment = 1
    }

    public sealed class StepRequestedEventArgs : System.EventArgs {
        public StepRequestedEventArgs(StepDirection direction) {
            Direction = direction;
        }

        public StepDirection Direction { get; }
        public bool Handled { get; set; }
    }

    /// <summary>
    /// Headless stub for the WPF DetachingExpander control.
    /// </summary>
    public class DetachingExpander : System.Windows.FrameworkElement {
        public bool IsExpanded { get; set; }
        public object Header { get; set; }
        public object Content { get; set; }
    }
}
