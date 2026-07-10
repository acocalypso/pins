#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Generic;
using System.Reflection;

namespace System.Windows.Input
{
    public static class CommandManager
    {
        // WPF holds RequerySuggested subscribers weakly (RelayCommand.CanExecuteChanged forwards every
        // add/remove straight through) so a subscriber that never unsubscribes - the common case for
        // transient VMs/commands - doesn't get pinned forever. Static-method handlers have no target to
        // leak and are stored directly (targetRef == null is the marker for that).
        private static readonly object _lock = new object();
        private static readonly List<(WeakReference targetRef, MethodInfo method)> _handlers = new();

        public static event EventHandler RequerySuggested
        {
            add
            {
                if (value == null) return;
                lock (_lock)
                {
                    _handlers.Add((value.Target == null ? null : new WeakReference(value.Target), value.Method));
                }
            }
            remove
            {
                if (value == null) return;
                lock (_lock)
                {
                    for (int i = _handlers.Count - 1; i >= 0; i--)
                    {
                        var (targetRef, method) = _handlers[i];
                        var target = targetRef?.Target;
                        if (targetRef != null && target == null)
                        {
                            _handlers.RemoveAt(i); // prune collected entry
                            continue;
                        }
                        if (target == value.Target && method == value.Method)
                        {
                            _handlers.RemoveAt(i); // remove only the one matching subscription
                            return;
                        }
                    }
                }
            }
        }

        public static void InvalidateRequerySuggested()
        {
            // Snapshot outside the invoke loop: resolving each WeakReference once roots it in this list
            // for the duration of the invocation, and prunes any entry collected since it was added.
            var snapshot = new List<(object target, MethodInfo method)>();
            lock (_lock)
            {
                for (int i = _handlers.Count - 1; i >= 0; i--)
                {
                    var (targetRef, method) = _handlers[i];
                    if (targetRef == null)
                    {
                        snapshot.Add((null, method));
                        continue;
                    }

                    var target = targetRef.Target;
                    if (target == null)
                    {
                        _handlers.RemoveAt(i);
                        continue;
                    }
                    snapshot.Add((target, method));
                }
            }

            foreach (var (target, method) in snapshot)
            {
                method.Invoke(target, new object[] { null, EventArgs.Empty });
            }
        }
    }
}
