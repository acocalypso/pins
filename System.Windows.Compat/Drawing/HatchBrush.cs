#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using OpenCvSharp;
using CvPoint = OpenCvSharp.Point;

namespace System.Drawing.Drawing2D {

    /// <summary>
    /// Hatch pattern styles, mirroring System.Drawing.Drawing2D.HatchStyle. The numeric values
    /// are GDI+'s, so anything that persists or interops with the raw value keeps working.
    /// </summary>
    public enum HatchStyle {
        Horizontal = 0,
        Vertical = 1,
        ForwardDiagonal = 2,
        BackwardDiagonal = 3,
        Cross = 4,
        DiagonalCross = 5,
        Percent05 = 6,
        Percent10 = 7,
        Percent20 = 8,
        Percent25 = 9,
        Percent30 = 10,
        Percent40 = 11,
        Percent50 = 12,
        Percent60 = 13,
        Percent70 = 14,
        Percent75 = 15,
        Percent80 = 16,
        Percent90 = 17,
        LightDownwardDiagonal = 18,
        LightUpwardDiagonal = 19,
        DarkDownwardDiagonal = 20,
        DarkUpwardDiagonal = 21,
        WideDownwardDiagonal = 22,
        WideUpwardDiagonal = 23,
        LightVertical = 24,
        LightHorizontal = 25,
        NarrowVertical = 26,
        NarrowHorizontal = 27,
        DarkVertical = 28,
        DarkHorizontal = 29,
        DashedDownwardDiagonal = 30,
        DashedUpwardDiagonal = 31,
        DashedHorizontal = 32,
        DashedVertical = 33,
        SmallConfetti = 34,
        LargeConfetti = 35,
        ZigZag = 36,
        Wave = 37,
        DiagonalBrick = 38,
        HorizontalBrick = 39,
        Weave = 40,
        Plaid = 41,
        Divot = 42,
        DottedGrid = 43,
        DottedDiamond = 44,
        Shingle = 45,
        Trellis = 46,
        Sphere = 47,
        SmallGrid = 48,
        SmallCheckerBoard = 49,
        LargeCheckerBoard = 50,
        OutlinedDiamond = 51,
        SolidDiamond = 52,
        LargeGrid = Cross,
        Min = Horizontal,
        Max = SolidDiamond
    }

    /// <summary>
    /// A two-color patterned brush, mirroring System.Drawing.Drawing2D.HatchBrush.
    ///
    /// The line-based styles (the horizontal, vertical and diagonal families, plus Cross,
    /// DiagonalCross and SmallGrid) are drawn as real patterns. The Percent* styles are
    /// rendered as a flat blend of the two colors at the style's coverage ratio, which is what
    /// they approximate at any normal viewing size anyway. The remaining decorative styles
    /// (confetti, brick, weave, checkerboard, ...) are not reproduced and fall back to an even
    /// blend of the two colors - they exist so the enum is complete, not because this shim
    /// draws them.
    ///
    /// Only <see cref="Graphics.FillPolygon(Brush, PointF[])"/> renders the pattern. Passing a
    /// HatchBrush to a fill that is not pattern-aware yields a flat
    /// <see cref="BackgroundColor"/> fill via <see cref="ToScalar"/>.
    /// </summary>
    public class HatchBrush : Brush {

        public HatchBrush(HatchStyle hatchStyle, Color foreColor)
            : this(hatchStyle, foreColor, Color.Black) { }

        public HatchBrush(HatchStyle hatchStyle, Color foreColor, Color backColor) {
            HatchStyle = hatchStyle;
            ForegroundColor = foreColor;
            BackgroundColor = backColor;
        }

        public HatchStyle HatchStyle { get; }
        public Color ForegroundColor { get; }
        public Color BackgroundColor { get; }

        private Scalar ForegroundScalar => new Scalar(ForegroundColor.B, ForegroundColor.G, ForegroundColor.R, ForegroundColor.A);
        private Scalar BackgroundScalar => new Scalar(BackgroundColor.B, BackgroundColor.G, BackgroundColor.R, BackgroundColor.A);

        internal override Scalar ToScalar() => BackgroundScalar;

        /// <summary>
        /// Paints the hatch pattern over the whole of <paramref name="patch"/>.
        ///
        /// <paramref name="originX"/>/<paramref name="originY"/> are the patch's position on the
        /// destination canvas. The pattern is phased against that origin rather than against the
        /// patch, so two polygons filled with the same brush line up seamlessly no matter where
        /// their bounding boxes happen to fall.
        /// </summary>
        internal void DrawPattern(Mat patch, int originX, int originY) {
            if (TryGetPercentCoverage(HatchStyle, out double coverage)) {
                patch.SetTo(Blend(BackgroundScalar, ForegroundScalar, coverage));
                return;
            }

            if (!TryGetLinePattern(HatchStyle, out LinePattern pattern)) {
                // Decorative style with no line-based equivalent - see the class remarks.
                patch.SetTo(Blend(BackgroundScalar, ForegroundScalar, 0.5));
                return;
            }

            patch.SetTo(BackgroundScalar);
            Scalar foreground = ForegroundScalar;
            if (pattern.Direction.HasFlag(HatchDirection.Horizontal)) {
                DrawHorizontalLines(patch, foreground, pattern, originY);
            }
            if (pattern.Direction.HasFlag(HatchDirection.Vertical)) {
                DrawVerticalLines(patch, foreground, pattern, originX);
            }
            if (pattern.Direction.HasFlag(HatchDirection.Downward)) {
                DrawDownwardLines(patch, foreground, pattern, originX, originY);
            }
            if (pattern.Direction.HasFlag(HatchDirection.Upward)) {
                DrawUpwardLines(patch, foreground, pattern, originX, originY);
            }
        }

        public override void Dispose() {
            // No unmanaged resources - the pattern is generated on demand.
        }

        private static void DrawHorizontalLines(Mat patch, Scalar color, LinePattern pattern, int originY) {
            for (int y = FirstOffset(originY, pattern.Spacing); y < patch.Height; y += pattern.Spacing) {
                DrawLine(patch, color, new CvPoint(0, y), new CvPoint(patch.Width - 1, y), pattern);
            }
        }

        private static void DrawVerticalLines(Mat patch, Scalar color, LinePattern pattern, int originX) {
            for (int x = FirstOffset(originX, pattern.Spacing); x < patch.Width; x += pattern.Spacing) {
                DrawLine(patch, color, new CvPoint(x, 0), new CvPoint(x, patch.Height - 1), pattern);
            }
        }

        /// <summary>
        /// Draws the '\' family: lines where absolute (x - y) is a multiple of the spacing.
        /// </summary>
        private static void DrawDownwardLines(Mat patch, Scalar color, LinePattern pattern, int originX, int originY) {
            int phase = FirstOffset(originX - originY, pattern.Spacing);
            for (int intercept = phase - patch.Height; intercept < patch.Width; intercept += pattern.Spacing) {
                DrawLine(patch, color, new CvPoint(intercept, 0), new CvPoint(intercept + patch.Height, patch.Height), pattern);
            }
        }

        /// <summary>
        /// Draws the '/' family: lines where absolute (x + y) is a multiple of the spacing.
        /// </summary>
        private static void DrawUpwardLines(Mat patch, Scalar color, LinePattern pattern, int originX, int originY) {
            int phase = FirstOffset(originX + originY, pattern.Spacing);
            for (int sum = phase; sum < patch.Width + patch.Height; sum += pattern.Spacing) {
                DrawLine(patch, color, new CvPoint(sum, 0), new CvPoint(sum - patch.Height, patch.Height), pattern);
            }
        }

        private static void DrawLine(Mat patch, Scalar color, CvPoint start, CvPoint end, LinePattern pattern) {
            // Link8 rather than AntiAlias: a hatch reads as a crisp pattern, and anti-aliasing a
            // one-pixel line at this density just muddies it.
            if (!pattern.Dashed) {
                Cv2.Line(patch, start, end, color, pattern.Thickness, LineTypes.Link8);
                return;
            }

            const int DashLength = 4;
            double deltaX = end.X - start.X;
            double deltaY = end.Y - start.Y;
            double length = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            if (length <= 0) {
                return;
            }
            double unitX = deltaX / length;
            double unitY = deltaY / length;
            for (double along = 0; along < length; along += DashLength * 2) {
                double dashEnd = Math.Min(along + DashLength, length);
                Cv2.Line(
                    patch,
                    new CvPoint((int)(start.X + unitX * along), (int)(start.Y + unitY * along)),
                    new CvPoint((int)(start.X + unitX * dashEnd), (int)(start.Y + unitY * dashEnd)),
                    color,
                    pattern.Thickness,
                    LineTypes.Link8);
            }
        }

        /// <summary>
        /// The smallest non-negative patch offset at which a pattern line falls, given the
        /// patch's absolute position along that axis.
        /// </summary>
        private static int FirstOffset(int origin, int spacing) {
            return ((-origin % spacing) + spacing) % spacing;
        }

        private static Scalar Blend(Scalar background, Scalar foreground, double coverage) {
            return new Scalar(
                background.Val0 + (foreground.Val0 - background.Val0) * coverage,
                background.Val1 + (foreground.Val1 - background.Val1) * coverage,
                background.Val2 + (foreground.Val2 - background.Val2) * coverage,
                background.Val3 + (foreground.Val3 - background.Val3) * coverage);
        }

        private static bool TryGetPercentCoverage(HatchStyle style, out double coverage) {
            switch (style) {
                case HatchStyle.Percent05: coverage = 0.05; return true;
                case HatchStyle.Percent10: coverage = 0.10; return true;
                case HatchStyle.Percent20: coverage = 0.20; return true;
                case HatchStyle.Percent25: coverage = 0.25; return true;
                case HatchStyle.Percent30: coverage = 0.30; return true;
                case HatchStyle.Percent40: coverage = 0.40; return true;
                case HatchStyle.Percent50: coverage = 0.50; return true;
                case HatchStyle.Percent60: coverage = 0.60; return true;
                case HatchStyle.Percent70: coverage = 0.70; return true;
                case HatchStyle.Percent75: coverage = 0.75; return true;
                case HatchStyle.Percent80: coverage = 0.80; return true;
                case HatchStyle.Percent90: coverage = 0.90; return true;
                default: coverage = 0; return false;
            }
        }

        private static bool TryGetLinePattern(HatchStyle style, out LinePattern pattern) {
            switch (style) {
                case HatchStyle.Horizontal:
                    pattern = new LinePattern(HatchDirection.Horizontal, 8, 1, false); return true;
                case HatchStyle.LightHorizontal:
                    pattern = new LinePattern(HatchDirection.Horizontal, 6, 1, false); return true;
                case HatchStyle.NarrowHorizontal:
                    pattern = new LinePattern(HatchDirection.Horizontal, 4, 1, false); return true;
                case HatchStyle.DarkHorizontal:
                    pattern = new LinePattern(HatchDirection.Horizontal, 4, 2, false); return true;
                case HatchStyle.DashedHorizontal:
                    pattern = new LinePattern(HatchDirection.Horizontal, 8, 1, true); return true;

                case HatchStyle.Vertical:
                    pattern = new LinePattern(HatchDirection.Vertical, 8, 1, false); return true;
                case HatchStyle.LightVertical:
                    pattern = new LinePattern(HatchDirection.Vertical, 6, 1, false); return true;
                case HatchStyle.NarrowVertical:
                    pattern = new LinePattern(HatchDirection.Vertical, 4, 1, false); return true;
                case HatchStyle.DarkVertical:
                    pattern = new LinePattern(HatchDirection.Vertical, 4, 2, false); return true;
                case HatchStyle.DashedVertical:
                    pattern = new LinePattern(HatchDirection.Vertical, 8, 1, true); return true;

                case HatchStyle.ForwardDiagonal:
                    pattern = new LinePattern(HatchDirection.Downward, 8, 1, false); return true;
                case HatchStyle.LightDownwardDiagonal:
                    pattern = new LinePattern(HatchDirection.Downward, 6, 1, false); return true;
                case HatchStyle.DarkDownwardDiagonal:
                    pattern = new LinePattern(HatchDirection.Downward, 4, 2, false); return true;
                case HatchStyle.WideDownwardDiagonal:
                    pattern = new LinePattern(HatchDirection.Downward, 10, 3, false); return true;
                case HatchStyle.DashedDownwardDiagonal:
                    pattern = new LinePattern(HatchDirection.Downward, 8, 1, true); return true;

                case HatchStyle.BackwardDiagonal:
                    pattern = new LinePattern(HatchDirection.Upward, 8, 1, false); return true;
                case HatchStyle.LightUpwardDiagonal:
                    pattern = new LinePattern(HatchDirection.Upward, 6, 1, false); return true;
                case HatchStyle.DarkUpwardDiagonal:
                    pattern = new LinePattern(HatchDirection.Upward, 4, 2, false); return true;
                case HatchStyle.WideUpwardDiagonal:
                    pattern = new LinePattern(HatchDirection.Upward, 10, 3, false); return true;
                case HatchStyle.DashedUpwardDiagonal:
                    pattern = new LinePattern(HatchDirection.Upward, 8, 1, true); return true;

                // LargeGrid shares Cross's value, so this arm covers both.
                case HatchStyle.Cross:
                    pattern = new LinePattern(HatchDirection.Horizontal | HatchDirection.Vertical, 8, 1, false); return true;
                case HatchStyle.SmallGrid:
                    pattern = new LinePattern(HatchDirection.Horizontal | HatchDirection.Vertical, 4, 1, false); return true;
                case HatchStyle.DiagonalCross:
                    pattern = new LinePattern(HatchDirection.Downward | HatchDirection.Upward, 8, 1, false); return true;

                default:
                    pattern = default;
                    return false;
            }
        }

        [Flags]
        private enum HatchDirection {
            Horizontal = 1,
            Vertical = 2,
            Downward = 4,
            Upward = 8
        }

        private readonly struct LinePattern {

            public LinePattern(HatchDirection direction, int spacing, int thickness, bool dashed) {
                Direction = direction;
                Spacing = spacing;
                Thickness = thickness;
                Dashed = dashed;
            }

            public HatchDirection Direction { get; }
            public int Spacing { get; }
            public int Thickness { get; }
            public bool Dashed { get; }
        }
    }
}
