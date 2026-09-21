#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace System.Windows {
    // Minimal stub for DependencyProperty
    public class DependencyProperty {
        private PropertyMetadata metadata;

        /// <summary>
        /// The registered name of this dependency property.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Represents an unset value for a dependency property.
        /// </summary>
        public static readonly object UnsetValue = new object();

        public static DependencyProperty Register(string name, System.Type propertyType, System.Type ownerType) 
            => new DependencyProperty { Name = name, metadata = new PropertyMetadata() };

        public static DependencyProperty Register(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata metadata) 
            => new DependencyProperty { Name = name, metadata = metadata };

        public static DependencyProperty Register(string name, System.Type propertyType, System.Type ownerType, object metadata) 
            => new DependencyProperty { Name = name, metadata = metadata as PropertyMetadata ?? new PropertyMetadata() };

        public static DependencyProperty RegisterAttached(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata metadata) 
            => new DependencyProperty { Name = name, metadata = metadata };

        public static DependencyProperty RegisterAttached(string name, System.Type propertyType, System.Type ownerType, object metadata) 
            => new DependencyProperty { Name = name, metadata = metadata as PropertyMetadata ?? new PropertyMetadata() };

        public PropertyMetadata GetMetadata(System.Type forType) {
            return metadata ?? new PropertyMetadata();
        }

        public PropertyMetadata GetMetadata(DependencyObject targetObject) {
            return metadata ?? new PropertyMetadata();
        }

        public override string ToString() => Name ?? base.ToString();
    }

    /// <summary>
    /// Provides metadata for dependency properties.
    /// </summary>
    public class PropertyMetadata {
        public PropertyMetadata() { }

        public PropertyMetadata(object defaultValue) {
            DefaultValue = defaultValue;
        }

        public PropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback) {
            DefaultValue = defaultValue;
            PropertyChangedCallback = propertyChangedCallback;
        }

        public object DefaultValue { get; set; }
        public PropertyChangedCallback PropertyChangedCallback { get; set; }
    }

    /// <summary>
    /// Framework-level options for a registered dependency property. Only the flags a headless
    /// run can answer are acted on - layout, rendering and inheritance have no effect without a
    /// visual tree - but registrations keep passing them, so the whole set is accepted.
    /// </summary>
    [System.Flags]
    public enum FrameworkPropertyMetadataOptions {
        None = 0,
        AffectsMeasure = 1,
        AffectsArrange = 2,
        AffectsParentMeasure = 4,
        AffectsParentArrange = 8,
        AffectsRender = 16,
        Inherits = 32,
        OverridesInheritanceBehavior = 64,
        NotDataBindable = 128,
        BindsTwoWayByDefault = 256,
        Journal = 1024,
        SubPropertiesDoNotAffectRender = 2048
    }

    /// <summary>
    /// Provides metadata for framework-level dependency properties.
    /// </summary>
    public class FrameworkPropertyMetadata : PropertyMetadata {
        public FrameworkPropertyMetadata() { }

        public FrameworkPropertyMetadata(object defaultValue) : base(defaultValue) { }

        public FrameworkPropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback) 
            : base(defaultValue, propertyChangedCallback) { }

        public FrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions flags) : base(defaultValue) {
            Flags = flags;
        }

        public FrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions flags, PropertyChangedCallback propertyChangedCallback) 
            : base(defaultValue, propertyChangedCallback) {
            Flags = flags;
        }

        public FrameworkPropertyMetadataOptions Flags { get; set; }

        /// <summary>
        /// Decides how a binding left at BindingMode.Default transfers. Only registrations that
        /// pass the flag report true; there is no default WPF metadata table to fall back on.
        /// </summary>
        public bool BindsTwoWayByDefault {
            get => Flags.HasFlag(FrameworkPropertyMetadataOptions.BindsTwoWayByDefault);
            set => Flags = value
                ? Flags | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault
                : Flags & ~FrameworkPropertyMetadataOptions.BindsTwoWayByDefault;
        }

        public bool Inherits => Flags.HasFlag(FrameworkPropertyMetadataOptions.Inherits);
    }

    /// <summary>
    /// Provides metadata for UI-level dependency properties.
    /// </summary>
    public class UIPropertyMetadata : PropertyMetadata {
        public UIPropertyMetadata() { }

        public UIPropertyMetadata(object defaultValue) : base(defaultValue) { }

        public UIPropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback) 
            : base(defaultValue, propertyChangedCallback) { }
    }

    /// <summary>
    /// Represents a callback for when a dependency property value changes.
    /// </summary>
    public delegate void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e);

    /// <summary>
    /// Represents the method that handles the DataContextChanged event.
    /// </summary>
    public delegate void DependencyPropertyChangedEventHandler(object sender, DependencyPropertyChangedEventArgs e);

    /// <summary>
    /// Provides data for dependency property changed events.
    /// </summary>
    public class DependencyPropertyChangedEventArgs : System.EventArgs {
        /// <summary>
        /// Gets the dependency property that changed.
        /// </summary>
        public DependencyProperty Property { get; set; }

        /// <summary>
        /// Gets the old value of the property.
        /// </summary>
        public object OldValue { get; set; }

        /// <summary>
        /// Gets the new value of the property.
        /// </summary>
        public object NewValue { get; set; }
    }
}

