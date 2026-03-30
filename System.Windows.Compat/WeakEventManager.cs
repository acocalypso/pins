namespace System.Windows {
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    /// <summary>
    /// Provides weak event subscriptions so that listeners can be garbage-collected
    /// without first unsubscribing from the event source.
    /// </summary>
    /// <typeparam name="TEventSource">The type of the event source.</typeparam>
    /// <typeparam name="TEventArgs">The type of the event args.</typeparam>
    public static class WeakEventManager<TEventSource, TEventArgs> where TEventArgs : EventArgs {
        private static readonly object _lock = new object();
        // Map (source, eventName) → list of (weak target, method, wrapper delegate)
        private static readonly Dictionary<(int sourceHash, string eventName),
            List<(WeakReference targetRef, MethodInfo method, Delegate wrapper)>> _subscriptions = new();

        /// <summary>
        /// Adds a weak event handler for the specified event.
        /// The handler's target is held via a WeakReference so it can be collected.
        /// </summary>
        public static void AddHandler(TEventSource source, string eventName, EventHandler<TEventArgs> handler) {
            if (source == null || string.IsNullOrEmpty(eventName) || handler == null) {
                return;
            }

            try {
                var eventInfo = typeof(TEventSource).GetEvent(eventName);
                if (eventInfo == null) {
                    return;
                }

                if (handler.Target == null) {
                    // Static method — nothing to prevent from leaking; subscribe directly
                    eventInfo.AddEventHandler(source, handler);
                    return;
                }

                var weakTarget = new WeakReference(handler.Target);
                var method = handler.Method;

                EventHandler<TEventArgs> wrapper = (sender, args) => {
                    var target = weakTarget.Target;
                    if (target != null) {
                        method.Invoke(target, new object[] { sender, args });
                    }
                };

                lock (_lock) {
                    var key = (System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(source), eventName);
                    if (!_subscriptions.TryGetValue(key, out var list)) {
                        list = new List<(WeakReference, MethodInfo, Delegate)>();
                        _subscriptions[key] = list;
                    }
                    list.Add((weakTarget, method, wrapper));
                }

                eventInfo.AddEventHandler(source, wrapper);
            } catch {
                // Silently fail if we can't add the event handler
            }
        }

        /// <summary>
        /// Removes a weak event handler.
        /// </summary>
        public static void RemoveHandler(TEventSource source, string eventName, EventHandler<TEventArgs> handler) {
            if (source == null || string.IsNullOrEmpty(eventName) || handler == null) {
                return;
            }

            try {
                var eventInfo = typeof(TEventSource).GetEvent(eventName);
                if (eventInfo == null) return;

                lock (_lock) {
                    var key = (System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(source), eventName);
                    if (_subscriptions.TryGetValue(key, out var list)) {
                        for (int i = list.Count - 1; i >= 0; i--) {
                            var (targetRef, method, wrapper) = list[i];
                            var target = targetRef.Target;
                            // Remove if target was collected, or if it matches the handler being removed
                            if (target == null || (target == handler.Target && method == handler.Method)) {
                                eventInfo.RemoveEventHandler(source, wrapper);
                                list.RemoveAt(i);
                            }
                        }
                        if (list.Count == 0) _subscriptions.Remove(key);
                    }
                }
            } catch {
                // Silently fail if we can't remove the event handler
            }
        }
    }
}
