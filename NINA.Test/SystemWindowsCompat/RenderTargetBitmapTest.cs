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
using System.Windows.Media.Imaging;

namespace NINA.Test.SystemWindowsCompat {

    [TestFixture]
    public class RenderTargetBitmapTest {

        private static byte[] ReadBytes(BitmapSource source) {
            var stride = source.PixelWidth * source.Format.BitsPerPixel / 8;
            var buffer = new byte[stride * source.PixelHeight];
            source.CopyPixels(buffer, stride, 0);
            return buffer;
        }

        // REVIEW.md F12: RenderDrawImage rejected the entire draw when a rect was only
        // partially out of bounds, instead of clipping to the visible portion.
        [Test]
        public void Render_DrawImage_PartiallyOutOfBounds_ClipsInsteadOfVanishing() {
            var sourcePixels = new byte[] {
                10, 20,
                30, 40
            };
            using var source = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Gray8, null, sourcePixels, 2);
            using var target = new RenderTargetBitmap(3, 3, 96, 96, PixelFormats.Gray8);

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen()) {
                // A 2x2 image drawn at (2,2) on a 3x3 canvas is half off-canvas.
                dc.DrawImage(source, new Rect(2, 2, 2, 2));
            }
            target.Render(visual);

            var output = ReadBytes(target);
            // Only the source's top-left pixel (value 10) lands within the canvas, at (2,2).
            Assert.That(output[2 * 3 + 2], Is.EqualTo(10));
        }

        // REVIEW.md F12: DrawGeometry operations were silently skipped by Render.
        [Test]
        public void Render_DrawGeometry_RectangleGeometry_Fills() {
            using var target = new RenderTargetBitmap(10, 10, 96, 96, PixelFormats.Gray8);
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen()) {
                dc.DrawGeometry(new SolidColorBrush(Colors.White), null,
                    new RectangleGeometry { Rect = new Rect(2, 2, 4, 4) });
            }
            target.Render(visual);

            var output = ReadBytes(target);
            Assert.That(output[4 * 10 + 4], Is.EqualTo(255), "inside the filled rectangle");
            Assert.That(output[0], Is.EqualTo(0), "outside the rectangle stays background");
        }

        [Test]
        public void Render_DrawGeometry_GeometryGroup_RendersAllChildren() {
            using var target = new RenderTargetBitmap(10, 10, 96, 96, PixelFormats.Gray8);
            var group = new GeometryGroup();
            group.Children.Add(new RectangleGeometry { Rect = new Rect(0, 0, 2, 2) });
            group.Children.Add(new RectangleGeometry { Rect = new Rect(7, 7, 2, 2) });

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen()) {
                dc.DrawGeometry(new SolidColorBrush(Colors.White), null, group);
            }
            target.Render(visual);

            var output = ReadBytes(target);
            Assert.That(output[1 * 10 + 1], Is.EqualTo(255), "inside the first child rectangle");
            Assert.That(output[8 * 10 + 8], Is.EqualTo(255), "inside the second child rectangle");
        }

        [Test]
        public void Render_DrawGeometry_PathGeometry_FillsClosedFigure() {
            using var target = new RenderTargetBitmap(10, 10, 96, 96, PixelFormats.Gray8);
            var figure = new PathFigure {
                StartPoint = new Point(1, 1),
                IsClosed = true,
                Segments = new PathSegmentCollection {
                    new LineSegment(new Point(8, 1), true),
                    new LineSegment(new Point(8, 8), true),
                    new LineSegment(new Point(1, 8), true)
                }
            };
            var geometry = new PathGeometry { Figures = new PathFigureCollection { figure } };

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen()) {
                dc.DrawGeometry(new SolidColorBrush(Colors.White), null, geometry);
            }
            target.Render(visual);

            var output = ReadBytes(target);
            Assert.That(output[4 * 10 + 4], Is.EqualTo(255), "inside the filled path");
            Assert.That(output[0], Is.EqualTo(0), "outside the path stays background");
        }
    }
}
