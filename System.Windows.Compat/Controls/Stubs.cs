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
    /// <summary>
    /// Represents a popup window that appears above other content.
    /// </summary>
    public class Popup : FrameworkElement {
        public double HorizontalOffset { get; set; }
        public double VerticalOffset { get; set; }
        public IInputElement PlacementTarget { get; set; }
        public Controls.Primitives.PlacementMode Placement { get; set; }
        public UIElement Child { get; set; }
        public bool IsOpen { get; set; }

        public event EventHandler Opened;
        public event EventHandler Closed;
    }
}

namespace System.Windows.Controls {
    /// <summary>
    /// Represents a grid panel that contains rows and columns.
    /// </summary>
    public class Grid : FrameworkElement {
        public System.Collections.Generic.List<RowDefinition> RowDefinitions { get; set; } = new System.Collections.Generic.List<RowDefinition>();
        public System.Collections.Generic.List<ColumnDefinition> ColumnDefinitions { get; set; } = new System.Collections.Generic.List<ColumnDefinition>();
        public System.Collections.Generic.List<UIElement> Children { get; set; } = new System.Collections.Generic.List<UIElement>();
    }

    /// <summary>
    /// Represents a row definition in a grid.
    /// </summary>
    public class RowDefinition {
        public GridLength Height { get; set; }
    }

    /// <summary>
    /// Represents a column definition in a grid.
    /// </summary>
    public class ColumnDefinition {
        public GridLength Width { get; set; }
    }

    /// <summary>
    /// Represents a length value in a grid.
    /// </summary>
    public struct GridLength {
        public double Value { get; set; }
        public GridUnitType UnitType { get; set; }
    }

    /// <summary>
    /// Specifies the unit type of a grid length.
    /// </summary>
    public enum GridUnitType {
        Auto,
        Pixel,
        Star
    }

    /// <summary>
    /// Represents a popup window that appears above other content.
    /// </summary>
    public class Popup : System.Windows.Popup {
    }

    /// <summary>
    /// Represents a control that allows content to be scrolled.
    /// </summary>
    public class ScrollViewer : FrameworkElement {
        public double VerticalOffset { get; set; }
        public double HorizontalOffset { get; set; }
        public double ViewportHeight { get; set; }
        public double ViewportWidth { get; set; }
        public double ExtentHeight { get; set; }
        public double ExtentWidth { get; set; }
        public UIElement Content { get; set; }
        
        public ScrollBarVisibility VerticalScrollBarVisibility { get; set; }
        public ScrollBarVisibility HorizontalScrollBarVisibility { get; set; }

        public void ScrollToVerticalOffset(double offset) {
            VerticalOffset = offset;
        }

        public void ScrollToHorizontalOffset(double offset) {
            HorizontalOffset = offset;
        }
    }

    /// <summary>
    /// Specifies whether a control lays its content out horizontally or vertically.
    /// </summary>
    public enum Orientation {
        Horizontal = 0,
        Vertical = 1
    }

    /// <summary>
    /// Specifies the visibility of a scrollbar.
    /// </summary>
    public enum ScrollBarVisibility {
        Disabled,
        Auto,
        Hidden,
        Visible
    }
    /// <summary>
    /// Represents a menu item control.
    /// </summary>
    public class MenuItem : FrameworkElement {
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(object), typeof(MenuItem), new PropertyMetadata(null));

        public string Header { get; set; }
        public System.Collections.Generic.List<MenuItem> Items { get; set; } = new System.Collections.Generic.List<MenuItem>();
        public System.Windows.Input.ICommand Command { get; set; }
        public object CommandParameter { get; set; }
        public bool IsLoaded { get; set; }
        public bool IsSubmenuOpen { get; set; }

        public event System.Windows.RoutedEventHandler Loaded;
        public event System.Windows.RoutedEventHandler Click;
    }

    /// <summary>
    /// Represents a context menu. Headless has no menu surface, so this only holds the item
    /// collection callers build up.
    /// </summary>
    public class ContextMenu : ItemsControl {
        public bool IsOpen { get; set; }
        public IInputElement PlacementTarget { get; set; }
        public System.Windows.Controls.Primitives.PlacementMode Placement { get; set; }
    }

    /// <summary>
    /// Represents a horizontal or vertical separator between menu or list items.
    /// </summary>
    public class Separator : FrameworkElement {
    }

    /// <summary>
    /// Base class for validation rules
    /// </summary>
    public class ValidationRule {
        public virtual ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo) {
            return new ValidationResult(true, null);
        }
    }

    /// <summary>
    /// Represents the result of a validation
    /// </summary>
    public class ValidationResult {
        public ValidationResult(bool isValid, object errorContent) {
            IsValid = isValid;
            ErrorContent = errorContent;
        }

        public bool IsValid { get; }
        public object ErrorContent { get; }
    }

    /// <summary>
    /// Represents a control that can contain a collection of items.
    /// </summary>
    public class ItemsControl : FrameworkElement {
        public System.Collections.IList Items { get; } = new System.Collections.ArrayList();

        public static ItemsControl ItemsControlFromItemContainer(DependencyObject container) {
            return null;
        }

        public System.Windows.Controls.Primitives.ItemContainerGenerator ItemContainerGenerator { get; } = new System.Windows.Controls.Primitives.ItemContainerGenerator();
    }

    /// <summary>
    /// Represents an item in a TreeView control.
    /// </summary>
    public class TreeViewItem : ItemsControl {
        public bool IsExpanded { get; set; }
        public object Header { get; set; }
    }

    /// <summary>
    /// Represents a tree view control.
    /// </summary>
    public class TreeView : ItemsControl {
    }

    /// <summary>
    /// Represents a control that can be expanded or collapsed.
    /// </summary>
    public class Expander : System.Windows.FrameworkElement {
        public bool IsExpanded { get; set; }
    }

    /// <summary>
    /// Provides data for ContextMenuOpening and ContextMenuClosing events.
    /// </summary>
    public class ContextMenuEventArgs : System.Windows.RoutedEventArgs {
        public bool Handled { get; set; }
        public double CursorLeft { get; set; }
        public double CursorTop { get; set; }
    }
}

namespace System.Windows.Controls.Primitives {
    /// <summary>
    /// Specifies the placement of a popup element.
    /// </summary>
    public enum PlacementMode {
        Absolute,
        Relative,
        Left,
        Right,
        Top,
        Bottom,
        Center,
        Mouse,
        MousePoint,
        Custom
    }

    /// <summary>
    /// Generates containers for items in an ItemsControl.
    /// </summary>
    public class ItemContainerGenerator {
        public event EventHandler StatusChanged { add { } remove { } }
        public event EventHandler<ItemsChangedEventArgs> ItemsChanged { add { } remove { } }

        public System.Windows.DependencyObject ContainerFromIndex(int index) => null;
        public System.Windows.DependencyObject ContainerFromItem(object item) => null;
    }

    /// <summary>
    /// Base class for text input controls.
    /// </summary>
    public class TextBoxBase : System.Windows.FrameworkElement {
    }

    /// <summary>
    /// Base class for controls that select items.
    /// </summary>
    public class Selector : System.Windows.FrameworkElement {
    }

    /// <summary>
    /// Base class for controls that have a numeric range.
    /// </summary>
    public class RangeBase : System.Windows.FrameworkElement {
        public double Value { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
    }

    /// <summary>
    /// Represents a button that can be checked or unchecked.
    /// </summary>
    public class ToggleButton : ButtonBase {
        public bool? IsChecked { get; set; }
    }

    /// <summary>
    /// Provides data for the ItemsChanged event raised by an ItemContainerGenerator.
    /// </summary>
    public class ItemsChangedEventArgs : EventArgs {
        public System.Collections.Specialized.NotifyCollectionChangedAction Action { get; }
        public System.Windows.Controls.Primitives.GeneratorPosition Position { get; }
        public System.Windows.Controls.Primitives.GeneratorPosition OldPosition { get; }
        public int ItemCount { get; }
        public int ItemUICount { get; }
    }

    /// <summary>
    /// Describes the position of an item that has been generated.
    /// </summary>
    public struct GeneratorPosition {
        public int Index { get; set; }
        public int Offset { get; set; }

        public GeneratorPosition(int index, int offset) {
            Index = index;
            Offset = offset;
        }
    }
}
