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
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace System.Windows {
    /// <summary>
    /// Represents the result of a hit test operation.
    /// </summary>
    public interface IInputElement {
        event EventHandler<Input.MouseEventArgs> MouseMove;
    }

    /// <summary>
    /// Provides access to designer-related attached properties.
    /// </summary>
    public static class DesignerProperties {
        /// <summary>
        /// Attached property that indicates whether an element is in design mode.
        /// </summary>
        public static DependencyProperty IsInDesignModeProperty { get; } = DependencyProperty.RegisterAttached(
            "IsInDesignMode",
            typeof(bool),
            typeof(DesignerProperties),
            new PropertyMetadata(false));
    }

    /// <summary>
    /// Specifies the direction of text flow.
    /// </summary>
    /// <summary>
    /// Represents the thickness of a frame around a rectangle.
    /// </summary>
    public struct Thickness {
        public Thickness(double uniformLength) {
            Left = Top = Right = Bottom = uniformLength;
        }

        public Thickness(double leftRight, double topBottom) {
            Left = Right = leftRight;
            Top = Bottom = topBottom;
        }

        public Thickness(double left, double top, double right, double bottom) {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public double Left { get; set; }
        public double Top { get; set; }
        public double Right { get; set; }
        public double Bottom { get; set; }
    }

    public enum FlowDirection {
        LeftToRight,
        RightToLeft
    }
}

namespace System.Windows.Data {
    public delegate void CurrentChangingEventHandler(object sender, CurrentChangingEventArgs e);

    public class CurrentChangingEventArgs : System.EventArgs {
        public bool Cancel { get; set; }
    }

    public class GroupDescription { }

    public class PropertyGroupDescription : GroupDescription {
        public PropertyGroupDescription() { }
        public PropertyGroupDescription(string propertyName) {
            PropertyName = propertyName;
        }
        public string PropertyName { get; set; }
    }

    public class SortDescription {
        public SortDescription() { }
        public SortDescription(string propertyName, System.ComponentModel.ListSortDirection direction) {
            PropertyName = propertyName;
            Direction = direction;
        }
        public string PropertyName { get; set; }
        public System.ComponentModel.ListSortDirection Direction { get; set; }
    }

    public class SortDescriptionCollection : System.Collections.ObjectModel.Collection<SortDescription> { }

    public class CollectionViewSource {
        private DefaultCollectionView _view;
        private readonly System.Collections.ObjectModel.ObservableCollection<GroupDescription> _groupDescriptions;
        private readonly SortDescriptionCollection _sortDescriptions;

        public CollectionViewSource() {
            _groupDescriptions = new System.Collections.ObjectModel.ObservableCollection<GroupDescription>();
            _sortDescriptions = new SortDescriptionCollection();
            _view = null;
        }

        public object Source { 
            get { return _view?.SourceCollection; }
            set {
                _view = new DefaultCollectionView(value as System.Collections.IEnumerable);
            }
        }

        public System.ComponentModel.ICollectionView View => _view;
        public System.Collections.ObjectModel.ObservableCollection<GroupDescription> GroupDescriptions => _groupDescriptions;
        public SortDescriptionCollection SortDescriptions => _sortDescriptions;

        // Real WPF caches the default view per source collection (by reference), so repeated
        // calls for the same list return the same view instance and share Filter/Sort/Group
        // state instead of silently discarding it. Callers (e.g. SequencerFactory) rely on this.
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, DefaultCollectionView> _defaultViews = new();

        public static System.ComponentModel.ICollectionView GetDefaultView(System.Collections.IEnumerable collection) {
            if (collection == null) {
                return new DefaultCollectionView(null);
            }
            return _defaultViews.GetValue(collection, c => new DefaultCollectionView((System.Collections.IEnumerable)c));
        }
    }

    internal class DefaultCollectionView : System.ComponentModel.ICollectionView {
        private readonly System.Collections.IEnumerable _collection;
        private object _currentItem;
        private int _currentPosition = -1;

        public DefaultCollectionView(System.Collections.IEnumerable collection) {
            _collection = collection;
        }

        public System.Collections.IEnumerable SourceCollection => _collection;
        public object CurrentItem => _currentItem;
        public int CurrentPosition => _currentPosition;
        public bool IsCurrentAfterLast => _currentPosition >= GetCount();
        public bool IsCurrentBeforeFirst => _currentPosition < 0;
        public System.Globalization.CultureInfo Culture { get; set; }
        public System.Predicate<object> Filter { get; set; }
        public bool CanFilter => true;
        public SortDescriptionCollection SortDescriptions => new SortDescriptionCollection();
        public bool CanSort => false;
        public bool CanGroup => false;
        public System.Collections.ObjectModel.ObservableCollection<GroupDescription> GroupDescriptions => new System.Collections.ObjectModel.ObservableCollection<GroupDescription>();
        public System.Collections.ObjectModel.ReadOnlyObservableCollection<object> Groups => new System.Collections.ObjectModel.ReadOnlyObservableCollection<object>(new System.Collections.ObjectModel.ObservableCollection<object>());
        public bool IsEmpty => GetCount() == 0;

        public event System.Windows.Data.CurrentChangingEventHandler CurrentChanging;
        public event System.EventHandler CurrentChanged;
        public event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged;

        public bool MoveCurrentToFirst() {
            _currentPosition = 0;
            _currentItem = GetItemAt(0);
            CurrentChanged?.Invoke(this, System.EventArgs.Empty);
            return true;
        }

        public bool MoveCurrentToLast() {
            int count = GetCount();
            if (count > 0) {
                _currentPosition = count - 1;
                _currentItem = GetItemAt(_currentPosition);
                CurrentChanged?.Invoke(this, System.EventArgs.Empty);
                return true;
            }
            return false;
        }

        public bool MoveCurrentToNext() {
            if (_currentPosition < GetCount() - 1) {
                _currentPosition++;
                _currentItem = GetItemAt(_currentPosition);
                CurrentChanged?.Invoke(this, System.EventArgs.Empty);
                return true;
            }
            return false;
        }

        public bool MoveCurrentToPrevious() {
            if (_currentPosition > 0) {
                _currentPosition--;
                _currentItem = GetItemAt(_currentPosition);
                CurrentChanged?.Invoke(this, System.EventArgs.Empty);
                return true;
            }
            return false;
        }

        public bool MoveCurrentTo(object item) {
            // Enumerate through FilteredItems so the stored position indexes the same (filtered)
            // sequence that GetCount/GetItemAt resolve against - iterating the raw collection
            // here would desynchronize CurrentPosition from CurrentItem when a Filter is active.
            int index = 0;
            foreach (var obj in FilteredItems()) {
                if (obj == item) {
                    _currentPosition = index;
                    _currentItem = item;
                    CurrentChanged?.Invoke(this, System.EventArgs.Empty);
                    return true;
                }
                index++;
            }
            return false;
        }

        public bool MoveCurrentToPosition(int position) {
            if (position >= 0 && position < GetCount()) {
                _currentPosition = position;
                _currentItem = GetItemAt(position);
                CurrentChanged?.Invoke(this, System.EventArgs.Empty);
                return true;
            }
            return false;
        }

        public void Refresh() { }

        public System.IDisposable DeferRefresh() {
            return new DeferRefreshHelper(this);
        }

        public bool Contains(object item) {
            foreach (var obj in FilteredItems()) {
                if (obj == item) return true;
            }
            return false;
        }

        public System.Collections.IEnumerator GetEnumerator() {
            return FilteredItems().GetEnumerator();
        }

        // Filter is stored via the Filter property but WPF's ICollectionView only applies it
        // through enumeration/lookup, never on the raw SourceCollection; every accessor below
        // must go through this instead of iterating _collection directly.
        private System.Collections.Generic.IEnumerable<object> FilteredItems() {
            if (_collection == null) {
                // GetDefaultView(null) hands out a view with no source; treat it as empty
                // instead of NRE-ing on the first enumeration.
                yield break;
            }
            foreach (var item in _collection) {
                if (Filter == null || Filter(item)) {
                    yield return item;
                }
            }
        }

        private int GetCount() {
            if (Filter == null && _collection is System.Collections.ICollection col) {
                return col.Count;
            }
            int count = 0;
            foreach (var item in FilteredItems()) {
                count++;
            }
            return count;
        }

        private object GetItemAt(int index) {
            if (Filter == null && _collection is System.Collections.IList list) {
                return index >= 0 && index < list.Count ? list[index] : null;
            }
            int count = 0;
            foreach (var item in FilteredItems()) {
                if (count == index) return item;
                count++;
            }
            return null;
        }

        private class DeferRefreshHelper : System.IDisposable {
            private readonly DefaultCollectionView _view;

            public DeferRefreshHelper(DefaultCollectionView view) {
                _view = view;
            }

            public void Dispose() { }
        }
    }

    public class Binding : BindingBase {
        public Binding() { }
        public Binding(string path) { Path = path; }

        public string Path { get; set; }
        public BindingMode Mode { get; set; }
        public object Source { get; set; }
        public string UpdateSourceTrigger { get; set; }
        public object Converter { get; set; }
        public object ConverterParameter { get; set; }

        /// <summary>
        /// Sentinel value indicating a binding converter should not return a value
        /// </summary>
        public static readonly object DoNothing = new object();
    }

    /// <summary>
    /// Binding plumbing. Headless has no binding engine, so a binding set here never transfers
    /// a value - the target property keeps whatever it was assigned directly.
    /// </summary>
    public static class BindingOperations {
        public static BindingExpressionBase SetBinding(DependencyObject target, DependencyProperty dp, BindingBase binding) => null;

        public static void ClearBinding(DependencyObject target, DependencyProperty dp) { }

        public static void ClearAllBindings(DependencyObject target) { }

        public static BindingExpressionBase GetBindingExpressionBase(DependencyObject target, DependencyProperty dp) => null;
    }

    public class BindingExpressionBase { }

    public interface IValueConverter { }
    public class BindingBase { }
    public enum BindingMode { OneWay, TwoWay, OneTime, OneWayToSource, Default }
    public class MultiBinding : BindingBase { }
    
    public interface IMultiValueConverter {
        object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture);
        object[] ConvertBack(object value, System.Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture);
    }
    
    /// <summary>
    /// Attribute that specifies the input and output types for a value converter
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class ValueConversionAttribute : System.Attribute {
        public ValueConversionAttribute(System.Type sourceType, System.Type targetType) {
            SourceType = sourceType;
            TargetType = targetType;
        }

        public System.Type SourceType { get; }
        public System.Type TargetType { get; }
    }

    public class BindingExpression : BindingExpressionBase {
        public object ResolvedSource { get; set; }
    }
}

namespace System.ComponentModel {
    public interface ICollectionView : System.Collections.IEnumerable, System.Collections.Specialized.INotifyCollectionChanged {
        System.Collections.IEnumerable SourceCollection { get; }
        object CurrentItem { get; }
        int CurrentPosition { get; }
        bool IsCurrentAfterLast { get; }
        bool IsCurrentBeforeFirst { get; }
        System.Globalization.CultureInfo Culture { get; set; }
        System.Predicate<object> Filter { get; set; }
        bool CanFilter { get; }
        System.Windows.Data.SortDescriptionCollection SortDescriptions { get; }
        bool CanSort { get; }
        bool CanGroup { get; }
        System.Collections.ObjectModel.ObservableCollection<System.Windows.Data.GroupDescription> GroupDescriptions { get; }
        System.Collections.ObjectModel.ReadOnlyObservableCollection<object> Groups { get; }
        bool IsEmpty { get; }
        event System.Windows.Data.CurrentChangingEventHandler CurrentChanging;
        event System.EventHandler CurrentChanged;
        bool MoveCurrentToFirst();
        bool MoveCurrentToLast();
        bool MoveCurrentToNext();
        bool MoveCurrentToPrevious();
        bool MoveCurrentTo(object item);
        bool MoveCurrentToPosition(int position);
        void Refresh();
        System.IDisposable DeferRefresh();
        bool Contains(object item);
    }
}

namespace System.Windows {
    public static class MessageBox {
        public static MessageBoxResult Show(string messageBoxText) => MessageBoxResult.OK;
        public static MessageBoxResult Show(string messageBoxText, string caption) => MessageBoxResult.OK;
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button) => MessageBoxResult.OK;
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon) => MessageBoxResult.OK;
    }
    public enum SizeToContent {
        Manual = 0,
        Width = 1,
        Height = 2,
        WidthAndHeight = 3
    }

    public enum MessageBoxResult {
        None = 0,
        OK = 1,
        Cancel = 2,
        Yes = 6,
        No = 7
    }

    public enum MessageBoxButton {
        OK = 0,
        OKCancel = 1,
        YesNoCancel = 3,
        YesNo = 4
    }

    public enum MessageBoxImage {
        None = 0,
        Error = 16,
        Question = 32,
        Warning = 48,
        Information = 64
    }

    public enum Visibility {
        Visible = 0,
        Hidden = 1,
        Collapsed = 2
    }

    public class Style { }

    public class DependencyObject : Media.Visual {
        private Dictionary<DependencyProperty, object> _propertyValues = new Dictionary<DependencyProperty, object>();

        public object GetValue(DependencyProperty dp) {
            if (dp != null && _propertyValues.TryGetValue(dp, out var value)) {
                return value;
            }
            return dp?.GetMetadata(GetType())?.DefaultValue;
        }

        public void SetValue(DependencyProperty dp, object value) {
            if (dp != null) {
                _propertyValues[dp] = value;
            }
        }

        public void ClearValue(DependencyProperty dp) {
            if (dp != null) {
                _propertyValues.Remove(dp);
            }
        }

        public void BeginAnimation(DependencyProperty dp, Media.Animation.AnimationTimeline animation) {
            // Stub for animation - in headless mode, animations are not executed
            // This method is called but doesn't actually animate anything
        }
    }

    /// <summary>
    /// Provides a base class for objects that can be made immutable.
    /// </summary>
    public abstract class Freezable : DependencyObject {
        private bool _isFrozen = false;

        public bool IsFrozen => _isFrozen;

        public void Freeze() {
            _isFrozen = true;
        }

        protected abstract Freezable CreateInstanceCore();

        public Freezable Clone() {
            var clone = CreateInstanceCore();
            if (clone != null) {
                // Copy dependency properties from this instance to clone
                var props = GetType().GetProperties();
                foreach (var prop in props) {
                    if (prop.CanRead && prop.CanWrite) {
                        try {
                            prop.SetValue(clone, prop.GetValue(this));
                        } catch {
                            // Ignore properties that can't be copied
                        }
                    }
                }
            }
            return clone;
        }
    }

    /// <summary>
    /// A generic collection of Freezable objects.
    /// </summary>
    public class FreezableCollection<T> : Freezable, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable
        where T : Freezable {
        private readonly System.Collections.ObjectModel.ObservableCollection<T> _collection;

        public FreezableCollection() {
            _collection = new System.Collections.ObjectModel.ObservableCollection<T>();
        }

        public void Add(T item) {
            _collection.Add(item);
        }

        public void Remove(T item) {
            _collection.Remove(item);
        }

        public void Clear() {
            _collection.Clear();
        }

        public System.Collections.Generic.IEnumerator<T> GetEnumerator() {
            return _collection.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return _collection.GetEnumerator();
        }

        protected override Freezable CreateInstanceCore() {
            return new FreezableCollection<T>();
        }
    }

    public enum ShutdownMode {
        OnLastWindowClose,
        OnMainWindowClose,
        OnExplicitShutdown
    }

    public class Application {
        public static Application Current { get; } = new Application();
        public ShutdownMode ShutdownMode { get; set; } = ShutdownMode.OnLastWindowClose;
        public Window MainWindow { get; set; }
        public Threading.Dispatcher Dispatcher { get; } = Threading.Dispatcher.CurrentDispatcher;
        public ResourceDictionary Resources { get; set; } = new ResourceDictionary();
        public WindowCollection Windows { get; } = new WindowCollection();
        public object TryFindResource(object key) => null;

        // Store reference to ASP.NET Core application lifetime for shutdown
        public static Microsoft.Extensions.Hosting.IHostApplicationLifetime ApplicationLifetime { get; set; }

        public void Shutdown() {
            ApplicationLifetime?.StopApplication();
        }

        public void Shutdown(int exitCode) {
            ApplicationLifetime?.StopApplication();
        }
    }

    public class WindowCollection : System.Collections.Generic.List<Window> {
        // Collection of Window objects - empty in headless mode
    }

    public class Window : UIElement {
        public bool IsFocused { get; set; }
        public bool IsLoaded { get; set; }
        public bool IsActive { get; set; }
        public bool IsVisible { get; set; }
        public WindowState WindowState { get; set; }
        public object SizeToContent { get; set; }
        public string Title { get; set; }
        public object Background { get; set; }
        public object ResizeMode { get; set; }
        public object WindowStyle { get; set; }
        public double MinHeight { get; set; }
        public double MinWidth { get; set; }
        public object Style { get; set; }
        public object Content { get; set; }
        public event CancelEventHandler Closing;
        public object DataContext { get; set; }
        public object Owner { get; set; }
        public WindowStartupLocation WindowStartupLocation { get; set; }
        public double Opacity { get; set; } = 1.0;
        public bool? DialogResult { get; set; }
        public Threading.Dispatcher Dispatcher { get; } = Threading.Dispatcher.CurrentDispatcher;
        public event EventHandler Closed;
        public event System.EventHandler SourceInitialized;
        public event EventHandler ContentRendered;
        public double ActualWidth { get; set; } = 800;
        public double ActualHeight { get; set; } = 600;
        public Rect RestoreBounds { get; set; } = new Rect(0, 0, 800, 600);
        public double Left { get; set; } = 0;
        public double Top { get; set; } = 0;
        public double Width { get; set; } = 800;
        public double Height { get; set; } = 600;
        public bool Topmost { get; set; }

        public bool? ShowDialog() => true;
        public void Show() { }
        public void Hide() { IsVisible = false; }
        public void Close() {
            var e = new CancelEventArgs(false);
            Closing?.Invoke(this, e);
            if (e.Cancel) {
                return;
            }
            Closed?.Invoke(this, EventArgs.Empty);
        }
        public bool Focus() => true;
        public bool Activate() => true;
        public Point PointToScreen(Point pt) => pt;
        public object FindName(string name) => null;

        // Added for stub compatibility
        public object GetValue(object dp) => null;
        public void SetValue(object dp, object value) { }
        public void InvalidateMeasure() { }
        public static object Register(string name, System.Type propertyType, System.Type ownerType, object metadata) => null;

        /// <summary>
        /// Gets the Window for an element, walking up the visual tree if needed.
        /// </summary>
        public static Window GetWindow(object element) {
            // In headless mode, return the current application's main window
            if (element is Window window) {
                return window;
            }
            return Application.Current?.MainWindow as Window;
        }
    }

    public enum WindowStartupLocation {
        Manual = 0,
        CenterScreen = 1,
        CenterOwner = 2
    }

    public enum WindowState {
        Normal = 0,
        Minimized = 1,
        Maximized = 2
    }

    public enum ResizeMode {
        NoResize = 0,
        CanMinimize = 1,
        CanResize = 2,
        CanResizeWithGrip = 3
    }

    public enum WindowStyle {
        None = 0,
        SingleBorderWindow = 1,
        ThreeDBorderWindow = 2,
        ToolWindow = 3
    }

    public class ResourceDictionary : System.Collections.Generic.Dictionary<object, object> {
        public Uri Source { get; set; }
        public System.Collections.Generic.List<ResourceDictionary> MergedDictionaries { get; set; } = new System.Collections.Generic.List<ResourceDictionary>();

        public ResourceDictionary() : base() { }

        // REVIEW.md F14: the indexer used to be entirely disconnected from the inherited
        // Dictionary's real storage - the setter silently dropped writes, and the getter never
        // consulted the real storage, so a resource written via Add() was invisible through the
        // indexer and a resource written via the indexer was invisible to Add/TryGetValue/
        // FindResource. Both now share the single real backing store.
        public new object this[object key] {
            get {
                if (TryGetValue(key, out var value)) {
                    return value;
                }

                // Fall back to a fabricated placeholder for graphics/geometry-shaped keys that
                // were never actually registered, so existing icon lookups in headless mode
                // (where no real icon resources are loaded) keep returning something drawable.
                string keyStr = key?.ToString() ?? "";
                if (keyStr.Contains("SVG") || keyStr.Contains("Icon") || keyStr.Contains("Geometry")) {
                    return new System.Windows.Media.GeometryGroup();
                }
                return null;
            }
            set {
                base[key] = value;
            }
        }

        public bool Contains(object key) {
            return ContainsKey(key);
        }
    }

    public struct Point {
        private OpenCvSharp.Point2d _point;

        public double X {
            get => _point.X;
            set => _point.X = value;
        }

        public double Y {
            get => _point.Y;
            set => _point.Y = value;
        }

        public Point(double x, double y) {
            _point = new OpenCvSharp.Point2d(x, y);
        }

        public static implicit operator OpenCvSharp.Point2d(Point p) =>
            new OpenCvSharp.Point2d(p.X, p.Y);

        public static implicit operator Point(OpenCvSharp.Point2d p) =>
            new Point(p.X, p.Y);

        public static Vector operator -(Point point1, Point point2) =>
            new Vector(point1.X - point2.X, point1.Y - point2.Y);

        public static Point operator +(Point point, Vector vector) =>
            new Point(point.X + vector.X, point.Y + vector.Y);

        public static Point operator -(Point point, Vector vector) =>
            new Point(point.X - vector.X, point.Y - vector.Y);

        // Exact coordinate comparison, matching WPF's Point operators - two points are equal
        // only if both components are bit-identical.
        public static bool operator ==(Point point1, Point point2) =>
            point1.X == point2.X && point1.Y == point2.Y;

        public static bool operator !=(Point point1, Point point2) => !(point1 == point2);

        public bool Equals(Point other) => this == other;

        public override bool Equals(object obj) => obj is Point other && this == other;

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => $"{X},{Y}";
    }

    public struct Size {
        public double Width { get; set; }
        public double Height { get; set; }

        public Size(double width, double height) {
            Width = width;
            Height = height;
        }
    }

    public struct Vector {
        private OpenCvSharp.Vec2d _vec;

        public double X {
            get => _vec.Item0;
            set => _vec.Item0 = value;
        }

        public double Y {
            get => _vec.Item1;
            set => _vec.Item1 = value;
        }

        public Vector(double x, double y) {
            _vec = new OpenCvSharp.Vec2d(x, y);
        }

        public double Length => OpenCvSharp.Cv2.Norm(_vec);

        /// <summary>
        /// Squared length. Worth preferring over <see cref="Length"/> when only comparing
        /// magnitudes, since it skips the square root.
        /// </summary>
        public double LengthSquared => X * X + Y * Y;

        /// <summary>
        /// Dot product, matching WPF's Vector.Multiply(Vector, Vector).
        /// </summary>
        public static double Multiply(Vector vector1, Vector vector2) =>
            vector1.X * vector2.X + vector1.Y * vector2.Y;

        public static Vector Multiply(Vector vector, double scalar) => vector * scalar;

        public static Vector Multiply(double scalar, Vector vector) => vector * scalar;

        public void Normalize() {
            double length = Length;
            if (length > 0) {
                _vec = _vec / length;
            }
        }

        public static Vector operator +(Vector v1, Vector v2) =>
            new Vector { _vec = v1._vec + v2._vec };

        public static Vector operator -(Vector v1, Vector v2) =>
            new Vector { _vec = v1._vec - v2._vec };

        public static Vector operator *(Vector v, double scalar) =>
            new Vector { _vec = v._vec * scalar };

        public static Vector operator *(double scalar, Vector v) => v * scalar;

        public static Vector operator /(Vector v, double scalar) =>
            new Vector { _vec = v._vec / scalar };

        public static double operator *(Vector v1, Vector v2) =>
            v1.X * v2.X + v1.Y * v2.Y;

        public static implicit operator OpenCvSharp.Vec2d(Vector v) =>
            new OpenCvSharp.Vec2d(v.X, v.Y);

        public static implicit operator Vector(OpenCvSharp.Vec2d v) =>
            new Vector(v.Item0, v.Item1);
    }

    /// <summary>
    /// Represents the method that handles routed events.
    /// </summary>
    public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);

    /// <summary>
    /// Represents a template for displaying data.
    /// </summary>
    public class DataTemplate {
        public object DataType { get; set; }
    }

    /// <summary>
    /// Represents a hierarchical data template for displaying data with child items.
    /// </summary>
    public class HierarchicalDataTemplate : DataTemplate {
        public string ItemsSource { get; set; }
        public DataTemplate ItemTemplate { get; set; }
    }

    /// <summary>
    /// Key used to reference a DataTemplate resource.
    /// </summary>
    public class DataTemplateKey {
        public object DataType { get; set; }

        public DataTemplateKey() { }

        public DataTemplateKey(object dataType) {
            DataType = dataType;
        }
    }

    /// <summary>
    /// Base class for template selectors.
    /// </summary>
    public abstract class DataTemplateSelector {
        public virtual DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container) {
            return null;
        }
    }

    /// <summary>
    /// Base class for style selectors.
    /// </summary>
    public abstract class StyleSelector {
        public abstract System.Windows.Style SelectStyle(object item, System.Windows.DependencyObject container);
    }
}

namespace System.Windows.Controls {

    public class Button : System.Windows.FrameworkElement {
        public string Name { get; set; }
        public object Content { get; set; }
        public object ToolTip { get; set; }
        public System.Windows.Visibility Visibility { get; set; }

        public event System.EventHandler Click;

        public void RaiseEvent(System.Windows.RoutedEventArgs e) { }
    }

    public class TextBlock : System.Windows.DependencyObject {
        public string Text { get; set; }
        public System.Windows.Documents.InlineCollection Inlines { get; } = new System.Windows.Documents.InlineCollection();
    }

    public class TextBox : System.Windows.FrameworkElement {
        public string Text { get; set; }
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(TextBox));
        public object ToolTip { get; set; }
        
        public System.Windows.Data.BindingExpression GetBindingExpression(DependencyProperty dp) {
            return null;
        }
    }

    public class Image : System.Windows.FrameworkElement {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(System.Windows.Media.ImageSource), typeof(Image));
        public static readonly DependencyProperty StretchProperty = DependencyProperty.Register("Stretch", typeof(System.Windows.Media.Stretch), typeof(Image));

        public System.Windows.Media.ImageSource Source { get; set; }
        public System.Windows.Media.Stretch Stretch { get; set; }
    }

    public class Border : System.Windows.FrameworkElement {
        public object Child { get; set; }
    }

    public class StackPanel : System.Windows.FrameworkElement {
        public System.Collections.Generic.List<System.Windows.UIElement> Children { get; } = new System.Collections.Generic.List<System.Windows.UIElement>();
    }

    public class Label : System.Windows.DependencyObject {
        public object Content { get; set; }
    }
}

namespace System.Windows.Controls.Primitives {

    public class ButtonBase : System.Windows.FrameworkElement {
        public static readonly System.Windows.RoutedEvent ClickEvent = new System.Windows.RoutedEvent();
    }
}

namespace System.Windows {

    public class RoutedEventArgs : EventArgs {
        public RoutedEvent RoutedEvent { get; set; }
        public object OriginalSource { get; set; }
        public bool Handled { get; set; }

        public RoutedEventArgs(RoutedEvent routedEvent) {
            RoutedEvent = routedEvent;
        }

        public RoutedEventArgs() { }
    }

    /// <summary>
    /// Provides a delegate for ContextMenu events.
    /// </summary>
    public delegate void ContextMenuEventHandler(object sender, System.Windows.Controls.ContextMenuEventArgs e);

    public class RoutedEvent {
        // Stub
    }

    /// <summary>
    /// Provides the base class for framework content elements.
    /// </summary>
    public class FrameworkContentElement : DependencyObject {
        public DependencyObject Parent { get; set; }
        public DependencyObject TemplatedParent { get; set; }
    }

    /// <summary>
    /// Provides methods for working with the logical tree.
    /// </summary>
    public static class LogicalTreeHelper {
        public static DependencyObject GetParent(DependencyObject current) {
            return null;
        }
    }

    /// <summary>
    /// Provides static text decoration instances.
    /// </summary>
    public static class TextDecorations {
        public static Media.TextDecoration Underline { get; } = new Media.TextDecoration();
        public static Media.TextDecoration Strikethrough { get; } = new Media.TextDecoration();
        public static Media.TextDecoration Overline { get; } = new Media.TextDecoration();
        public static Media.TextDecoration Baseline { get; } = new Media.TextDecoration();
    }
}

namespace System.Windows.Media {

    public static class VisualTreeHelper {
        public static int GetChildrenCount(DependencyObject reference) {
            // In headless mode, no visual tree
            return 0;
        }

        public static DependencyObject GetChild(DependencyObject reference, int childIndex) {
            // In headless mode, no visual tree
            return null;
        }

        public static DependencyObject GetParent(DependencyObject reference) {
            // In headless mode, no visual tree
            return null;
        }

        public static HitTestResult HitTest(Visual visual, Point point) {
            return new HitTestResult();
        }

        public static HitTestResult HitTest(FrameworkElement element, Point point) {
            return new HitTestResult();
        }

        public static HitTestResult HitTest(UIElement visual, HitTestFilterCallback filterCallback, HitTestResultCallback resultCallback, HitTestParameters hitTestParameters) {
            // In headless mode, no visual tree - just return empty result
            return new HitTestResult();
        }
    }

    /// <summary>
    /// Represents the result of a hit test operation.
    /// </summary>
    public class HitTestResult {
        public Visual VisualHit { get; set; }
    }

    /// <summary>
    /// Represents a text decoration.
    /// </summary>
    public class TextDecoration {
        public Pen Pen { get; set; }
        public TextDecorationLocation Location { get; set; }
    }

    /// <summary>
    /// Specifies the location of a text decoration.
    /// </summary>
    public enum TextDecorationLocation {
        Underline,
        Strikethrough,
        Overline,
        Baseline
    }

    /// <summary>
    /// Specifies where theme-specific resources are located.
    /// </summary>
    public enum ResourceDictionaryLocation {
        None = 0,
        SourceAssembly = 1,
        ExternalAssembly = 2
    }

    /// <summary>
    /// Specifies the location of theme-specific resource dictionaries for a theme-aware custom control library or application.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ThemeInfoAttribute : Attribute {
        public ThemeInfoAttribute(ResourceDictionaryLocation genericDictionaryLocation, ResourceDictionaryLocation themeDictionaryLocation) {
            GenericDictionaryLocation = genericDictionaryLocation;
            ThemeDictionaryLocation = themeDictionaryLocation;
        }

        public ResourceDictionaryLocation GenericDictionaryLocation { get; }
        public ResourceDictionaryLocation ThemeDictionaryLocation { get; }
    }
}

namespace System.Windows.Markup {
    /// <summary>
    /// Stub for WPF's XmlnsDefinitionAttribute, which maps XML namespaces to CLR namespaces.
    /// Not functional on Linux — exists only to allow the assembly to compile.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class XmlnsDefinitionAttribute : Attribute {
        public XmlnsDefinitionAttribute(string xmlNamespace, string clrNamespace) {
            XmlNamespace = xmlNamespace;
            ClrNamespace = clrNamespace;
        }

        public string XmlNamespace { get; }
        public string ClrNamespace { get; }
        public string AssemblyName { get; set; }
    }
}