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
using System.Windows.Media;

namespace NINA.Test.SystemWindowsCompat {

    [TestFixture]
    public class WpfPrimitivesTest {

        [Test]
        public void Rect_Properties_AreConsistent() {
            var rect = new Rect(1, 2, 3, 4);

            Assert.That(rect.Left, Is.EqualTo(1));
            Assert.That(rect.Top, Is.EqualTo(2));
            Assert.That(rect.Right, Is.EqualTo(4));
            Assert.That(rect.Bottom, Is.EqualTo(6));
            Assert.That(rect.IsEmpty, Is.False);
        }

        [Test]
        public void Rect_FromTwoPoints_ComputesSize() {
            var rect = new Rect(new Point(1, 2), new Point(4, 6));

            Assert.That(rect.X, Is.EqualTo(1));
            Assert.That(rect.Y, Is.EqualTo(2));
            Assert.That(rect.Width, Is.EqualTo(3));
            Assert.That(rect.Height, Is.EqualTo(4));
        }

        [Test]
        public void Int32Rect_Empty_IsEmpty() {
            Assert.That(Int32Rect.Empty.IsEmpty, Is.True);
            Assert.That(new Int32Rect(0, 0, 5, 5).IsEmpty, Is.False);
        }

        // REVIEW.md F17: WPF's Int32Rect.IsEmpty requires all four components to be zero
        // (matching Int32Rect.Empty exactly) - a rect with only Width or Height at zero is a
        // legitimate degenerate rect, not "empty".
        [Test]
        public void Int32Rect_ZeroWidthOrHeightOnly_IsNotEmpty() {
            Assert.That(new Int32Rect(0, 0, 5, 0).IsEmpty, Is.False);
            Assert.That(new Int32Rect(0, 0, 0, 5).IsEmpty, Is.False);
            Assert.That(new Int32Rect(1, 1, 0, 0).IsEmpty, Is.False, "non-zero position with zero size is not the Empty sentinel");
        }

        // REVIEW.md F17: WPF's Rect.IsEmpty is true only for the special Rect.Empty sentinel
        // (X = Y = +Infinity, Width = Height = -Infinity) - a legitimate zero-sized rect at a
        // real position is NOT empty.
        [Test]
        public void Rect_Empty_IsEmpty() {
            Assert.That(Rect.Empty.IsEmpty, Is.True);
            Assert.That(new Rect(5, 5, 0, 0).IsEmpty, Is.False);
        }

        // REVIEW.md F14: ResourceDictionary's indexer setter silently dropped writes and its
        // getter never consulted the inherited Dictionary's real storage - Add() and the
        // indexer were two disconnected stores. They must now agree.
        [Test]
        public void ResourceDictionary_IndexerSet_IsVisibleThroughIndexerGetAndTryGetValue() {
            var resources = new ResourceDictionary();
            resources["MyKey"] = "hello";

            Assert.That(resources["MyKey"], Is.EqualTo("hello"));
            Assert.That(resources.TryGetValue("MyKey", out var value), Is.True);
            Assert.That(value, Is.EqualTo("hello"));
        }

        [Test]
        public void ResourceDictionary_Add_IsVisibleThroughIndexer() {
            var resources = new ResourceDictionary();
            resources.Add("MyKey", "hello");

            Assert.That(resources["MyKey"], Is.EqualTo("hello"));
        }

        [Test]
        public void ResourceDictionary_UnregisteredSvgKey_FallsBackToFabricatedGeometry() {
            var resources = new ResourceDictionary();

            Assert.That(resources["SomeIconSVG"], Is.InstanceOf<GeometryGroup>());
        }

        [Test]
        public void ResourceDictionary_UnregisteredNonSvgKey_ReturnsNull() {
            var resources = new ResourceDictionary();

            Assert.That(resources["Whatever"], Is.Null);
        }

        [Test]
        public void Point_Minus_Point_YieldsVector() {
            var v = new Point(5, 7) - new Point(2, 3);

            Assert.That(v.X, Is.EqualTo(3));
            Assert.That(v.Y, Is.EqualTo(4));
        }

        [Test]
        public void Point_Plus_Vector_YieldsPoint() {
            var p = new Point(1, 1) + new Vector(2, 3);

            Assert.That(p.X, Is.EqualTo(3));
            Assert.That(p.Y, Is.EqualTo(4));
        }

        [Test]
        public void Vector_Length_IsEuclidean() {
            Assert.That(new Vector(3, 4).Length, Is.EqualTo(5).Within(1e-12));
        }

        [Test]
        public void Vector_DotProduct() {
            Assert.That(new Vector(1, 2) * new Vector(3, 4), Is.EqualTo(11));
        }

        [Test]
        public void Vector_Normalize_MakesUnitLength() {
            var v = new Vector(3, 4);
            v.Normalize();

            Assert.That(v.Length, Is.EqualTo(1).Within(1e-12));
            Assert.That(v.X, Is.EqualTo(0.6).Within(1e-12));
            Assert.That(v.Y, Is.EqualTo(0.8).Within(1e-12));
        }

        [Test]
        public void Thickness_Constructors() {
            var uniform = new Thickness(5);
            Assert.That(uniform.Left, Is.EqualTo(5));
            Assert.That(uniform.Bottom, Is.EqualTo(5));

            var full = new Thickness(1, 2, 3, 4);
            Assert.That(full.Left, Is.EqualTo(1));
            Assert.That(full.Top, Is.EqualTo(2));
            Assert.That(full.Right, Is.EqualTo(3));
            Assert.That(full.Bottom, Is.EqualTo(4));
        }

        [Test]
        public void Color_Equality_ComparesComponents() {
            var a = Color.FromArgb(10, 20, 30, 40);
            var b = Color.FromArgb(10, 20, 30, 40);
            var c = Color.FromArgb(10, 20, 30, 41);

            Assert.That(a == b, Is.True);
            Assert.That(a == c, Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Color_FromRgb_SetsOpaqueAlpha() {
            Assert.That(Color.FromRgb(1, 2, 3).A, Is.EqualTo(255));
        }

        [Test]
        public void ColorConverter_ParsesEightDigitHex() {
            var color = (Color)ColorConverter.ConvertFromString("#80112233");

            Assert.That(color.A, Is.EqualTo(0x80));
            Assert.That(color.R, Is.EqualTo(0x11));
            Assert.That(color.G, Is.EqualTo(0x22));
            Assert.That(color.B, Is.EqualTo(0x33));
        }

        [Test]
        public void ColorConverter_ParsesSixDigitHex_AsOpaque() {
            var color = (Color)ColorConverter.ConvertFromString("#112233");

            Assert.That(color.A, Is.EqualTo(255));
            Assert.That(color.R, Is.EqualTo(0x11));
        }

        [Test]
        public void ColorConverter_RoundTrips() {
            var original = Color.FromArgb(0xAA, 0x01, 0x02, 0x03);
            var text = new ColorConverter().ConvertToString(original);
            var parsed = (Color)ColorConverter.ConvertFromString(text);

            Assert.That(parsed, Is.EqualTo(original));
        }

        [Test]
        public void ColorConverter_InvalidLength_Throws() {
            Assert.Throws<FormatException>(() => ColorConverter.ConvertFromString("#12345"));
        }

        [Test]
        public void PixelFormat_Equality_DistinguishesSameBppFormats() {
            Assert.That(PixelFormats.Gray16 == PixelFormats.Gray16, Is.True);
            // Both 16 bpp but different formats — must not compare equal
            Assert.That(PixelFormats.Gray16 == PixelFormats.Bgr565, Is.False);
            // Both 32 bpp
            Assert.That(PixelFormats.Bgra32 == PixelFormats.Bgr32, Is.False);
            Assert.That(PixelFormats.Gray8 != null, Is.True);
        }

        [Test]
        public void PixelFormat_MapsToExpectedMatType() {
            Assert.That((OpenCvSharp.MatType)PixelFormats.Gray8, Is.EqualTo(OpenCvSharp.MatType.CV_8UC1));
            Assert.That((OpenCvSharp.MatType)PixelFormats.Gray16, Is.EqualTo(OpenCvSharp.MatType.CV_16UC1));
            Assert.That((OpenCvSharp.MatType)PixelFormats.Bgr24, Is.EqualTo(OpenCvSharp.MatType.CV_8UC3));
            Assert.That((OpenCvSharp.MatType)PixelFormats.Bgra32, Is.EqualTo(OpenCvSharp.MatType.CV_8UC4));
            Assert.That((OpenCvSharp.MatType)PixelFormats.Rgb48, Is.EqualTo(OpenCvSharp.MatType.CV_16UC3));
        }

        [Test]
        public void DependencyObject_GetValue_ReturnsMetadataDefault_UntilSet() {
            var property = DependencyProperty.Register(
                "TestValue", typeof(int), typeof(WpfPrimitivesTest), new PropertyMetadata(42));
            var obj = new DependencyObject();

            Assert.That(obj.GetValue(property), Is.EqualTo(42));

            obj.SetValue(property, 7);
            Assert.That(obj.GetValue(property), Is.EqualTo(7));
        }

        [Test]
        public void DependencyObject_ValuesArePerInstance() {
            var property = DependencyProperty.Register(
                "TestValue2", typeof(int), typeof(WpfPrimitivesTest), new PropertyMetadata(0));
            var first = new DependencyObject();
            var second = new DependencyObject();

            first.SetValue(property, 1);

            Assert.That(second.GetValue(property), Is.EqualTo(0));
        }

        [Test]
        public void Colors_Green_MatchesWpfDefinition() {
            Assert.That(Colors.Green.R, Is.EqualTo(0x00));
            Assert.That(Colors.Green.G, Is.EqualTo(0x80));
            Assert.That(Colors.Green.B, Is.EqualTo(0x00));
        }
    }
}
