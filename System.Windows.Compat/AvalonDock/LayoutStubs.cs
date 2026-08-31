#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AvalonDock {
    /// <summary>
    /// Main docking manager control for AvalonDock.
    /// </summary>
    public class DockingManager {
        public AvalonDock.Layout.LayoutRoot Layout { get; set; }
        public object LayoutItemTemplate { get; set; }
        public object LayoutItemTemplateSelector { get; set; }
        public object LayoutUpdateStrategy { get; set; }
    }
}

namespace AvalonDock.Layout {
    /// <summary>
    /// Interface for layout update strategies.
    /// </summary>
    public interface ILayoutUpdateStrategy {
        bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableToShow, ILayoutContainer destinationContainer);
        void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableShown);
        bool BeforeInsertDocument(LayoutRoot layout, LayoutDocument anchorableToShow, ILayoutContainer destinationContainer);
        void AfterInsertDocument(LayoutRoot layout, LayoutDocument anchorableShown);
    }

    /// <summary>
    /// Base class for layout elements in AvalonDock.
    /// </summary>
    public abstract class LayoutElement {
        public virtual T FindParent<T>() where T : class {
            return null;
        }
    }

    /// <summary>
    /// Container interface for layout elements.
    /// </summary>
    public interface ILayoutContainer {
        T FindParent<T>() where T : class;
    }

    /// <summary>
    /// Root layout element.
    /// </summary>
    public class LayoutRoot : LayoutElement, ILayoutContainer {
        public List<LayoutElement> Children { get; } = new List<LayoutElement>();

        public IEnumerable Descendents() {
            var result = new List<LayoutElement>();
            foreach (var child in Children) {
                result.Add(child);
                if (child is LayoutElement elem) {
                    result.AddRange(GetDescendentsNonGeneric(elem));
                }
            }
            return result;
        }

        public IEnumerable<T> Descendents<T>() where T : class {
            var result = new List<T>();
            foreach (var child in Children) {
                if (child is T t) {
                    result.Add(t);
                }
                if (child is LayoutElement elem) {
                    result.AddRange(GetDescendents<T>(elem));
                }
            }
            return result;
        }

        private IEnumerable<LayoutElement> GetDescendentsNonGeneric(LayoutElement element) {
            var result = new List<LayoutElement>();
            if (element is ILayoutContainer container) {
                // Stub implementation - would normally recurse through container children
            }
            return result;
        }

        private IEnumerable<T> GetDescendents<T>(LayoutElement element) where T : class {
            var result = new List<T>();
            if (element is ILayoutContainer container) {
                // Stub implementation - would normally recurse through container children
            }
            return result;
        }

        public T FindParent<T>() where T : class {
            return this as T;
        }
    }

    /// <summary>
    /// Represents an anchorable pane in the layout.
    /// </summary>
    public class LayoutAnchorablePane : LayoutElement, ILayoutContainer {
        public string Name { get; set; }
        public List<LayoutAnchorable> Children { get; } = new List<LayoutAnchorable>();

        public override T FindParent<T>() {
            return this as T;
        }
    }

    /// <summary>
    /// Represents an anchorable element in the layout.
    /// </summary>
    public class LayoutAnchorable : LayoutElement {
        public bool IsSelected { get; set; }
        public string Title { get; set; }
        public object Content { get; set; }

        public override T FindParent<T>() {
            return null;
        }
    }

    /// <summary>
    /// Represents a document in the layout.
    /// </summary>
    public class LayoutDocument : LayoutElement {
        public string Title { get; set; }
        public object Content { get; set; }
    }

    /// <summary>
    /// Represents a floating window in the layout.
    /// </summary>
    public class LayoutFloatingWindow : LayoutElement, ILayoutContainer {
        public List<LayoutElement> Children { get; } = new List<LayoutElement>();

        public override T FindParent<T>() {
            return this as T;
        }
    }

    namespace Serialization {
        /// <summary>
        /// Base class for layout serialization models.
        /// </summary>
        public class LayoutModel {
            public string ContentId { get; set; }
        }

        /// <summary>
        /// Arguments for layout serialization callbacks.
        /// </summary>
        public class LayoutSerializationCallbackEventArgs : EventArgs {
            public LayoutModel Model { get; set; }
            public object Content { get; set; }
            public string ContentId { get; set; }
            public bool Cancel { get; set; }
        }

        /// <summary>
        /// Serializes and deserializes AvalonDock layouts to XML.
        /// </summary>
        public class XmlLayoutSerializer {
            public object LayoutElement { get; set; }
            public event EventHandler<LayoutSerializationCallbackEventArgs> LayoutSerializationCallback;

            public XmlLayoutSerializer(object layoutElement) {
                LayoutElement = layoutElement;
            }

            public void Serialize(string path) {
                // Stub implementation - doesn't actually serialize in headless mode
            }

            public void Serialize(System.IO.Stream stream) {
                // Stub implementation - doesn't actually serialize in headless mode
            }

            public void Serialize(System.IO.TextWriter writer) {
                // Stub implementation - doesn't actually serialize in headless mode
            }

            public void Deserialize(string path) {
                // Stub implementation - doesn't actually deserialize in headless mode
            }

            public void Deserialize(System.IO.Stream stream) {
                // Stub implementation - doesn't actually deserialize in headless mode
            }

            public void Deserialize(System.IO.TextReader reader) {
                // Stub implementation - doesn't actually deserialize in headless mode
            }
        }
    }
}
