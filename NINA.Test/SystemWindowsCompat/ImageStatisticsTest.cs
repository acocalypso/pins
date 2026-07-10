#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Imaging;
using NUnit.Framework;
using System.Drawing;
using System.Drawing.Imaging;

namespace NINA.Test.SystemWindowsCompat {

    [TestFixture]
    public class ImageStatisticsTest {

        private static Bitmap Gray8WithValues(int width, int height, Func<int, int, int> valueAt) {
            var bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    int v = valueAt(x, y);
                    bitmap.SetPixel(x, y, Color.FromArgb(v, v, v));
                }
            }
            return bitmap;
        }

        [Test]
        public void Gray8_MeanMinMaxCount_AreExact() {
            // Left half 10, right half 30 -> mean 20
            using var bitmap = Gray8WithValues(4, 4, (x, y) => x < 2 ? 10 : 30);

            var stats = new ImageStatistics(bitmap);

            Assert.That(stats.Gray.Mean, Is.EqualTo(20).Within(1e-9));
            Assert.That(stats.Gray.Min, Is.EqualTo(10));
            Assert.That(stats.Gray.Max, Is.EqualTo(30));
            Assert.That(stats.Gray.PixelsCount, Is.EqualTo(16));
        }

        [Test]
        public void Gray8_StdDev_IsExactForTwoValueDistribution() {
            // Half 10, half 30: variance = 100, stddev = 10
            using var bitmap = Gray8WithValues(4, 4, (x, y) => x < 2 ? 10 : 30);

            var stats = new ImageStatistics(bitmap);

            Assert.That(stats.Gray.StdDev, Is.EqualTo(10).Within(1e-9));
        }

        [Test]
        public void Gray8_Median_OfUniformImage_IsThatValue() {
            using var bitmap = Gray8WithValues(4, 4, (x, y) => 42);

            var stats = new ImageStatistics(bitmap);

            Assert.That(stats.Gray.Median, Is.EqualTo(42));
        }

        [Test]
        public void GrayWithoutBlack_ExcludesZeroPixels() {
            // Half zeros, half 50
            using var bitmap = Gray8WithValues(4, 4, (x, y) => x < 2 ? 0 : 50);

            var stats = new ImageStatistics(bitmap);

            Assert.That(stats.Gray.Mean, Is.EqualTo(25).Within(1e-9));
            Assert.That(stats.GrayWithoutBlack.Mean, Is.EqualTo(50).Within(1e-9));
            Assert.That(stats.GrayWithoutBlack.PixelsCount, Is.EqualTo(8));
            Assert.That(stats.GrayWithoutBlack.Min, Is.EqualTo(50));
        }

        [Test]
        public void Gray16_UsesFullBitDepthHistogram() {
            // Build a 16-bit image directly through the underlying Mat since
            // Bitmap.SetPixel only reaches 16-bit values that are multiples of 257
            var mat = new OpenCvSharp.Mat(2, 2, OpenCvSharp.MatType.CV_16UC1);
            mat.Set<ushort>(0, 0, 1000);
            mat.Set<ushort>(0, 1, 1000);
            mat.Set<ushort>(1, 0, 3000);
            mat.Set<ushort>(1, 1, 3000);
            using var bitmap = new Bitmap(mat);

            var stats = new ImageStatistics(bitmap);

            Assert.That(stats.Gray.Mean, Is.EqualTo(2000).Within(1e-9));
            Assert.That(stats.Gray.Min, Is.EqualTo(1000));
            Assert.That(stats.Gray.Max, Is.EqualTo(3000));
            Assert.That(stats.Gray.PixelsCount, Is.EqualTo(4));
        }
    }
}
