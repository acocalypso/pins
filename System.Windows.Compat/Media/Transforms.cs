#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace System.Windows.Media {
    /// <summary>
    /// Represents a transform that translates (moves) an object.
    /// </summary>
    public class TranslateTransform : Transform {
        public double X { get; set; }
        public double Y { get; set; }

        public override Matrix Value => new Matrix {
            M11 = 1,
            M12 = 0,
            M21 = 0,
            M22 = 1,
            OffsetX = X,
            OffsetY = Y
        };
    }

    /// <summary>
    /// Represents a transform that rotates an object. Like WPF, a positive angle rotates
    /// clockwise on screen (y-down coordinates).
    /// </summary>
    public class RotateTransform : Transform {
        public double Angle { get; set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }

        public RotateTransform() {
        }

        public RotateTransform(double angle) {
            Angle = angle;
        }

        public RotateTransform(double angle, double centerX, double centerY) {
            Angle = angle;
            CenterX = centerX;
            CenterY = centerY;
        }

        public override Matrix Value {
            get {
                double radians = Angle * Math.PI / 180.0;
                double cos = Math.Cos(radians);
                double sin = Math.Sin(radians);
                // Rotation about (CenterX, CenterY): p' = (p - c) * R + c
                return new Matrix {
                    M11 = cos,
                    M12 = sin,
                    M21 = -sin,
                    M22 = cos,
                    OffsetX = CenterX - cos * CenterX + sin * CenterY,
                    OffsetY = CenterY - sin * CenterX - cos * CenterY
                };
            }
        }
    }

    /// <summary>
    /// Represents a transform defined directly by a matrix.
    /// </summary>
    public class MatrixTransform : Transform {
        public Matrix Matrix { get; set; } = Matrix.Identity;

        public MatrixTransform() {
        }

        public MatrixTransform(Matrix matrix) {
            Matrix = matrix;
        }

        public override Matrix Value => Matrix;
    }

    /// <summary>
    /// Represents a composite transform; children apply in list order, matching WPF.
    /// </summary>
    public class TransformGroup : Transform {
        public System.Collections.Generic.List<Transform> Children { get; } = new System.Collections.Generic.List<Transform>();

        public override Matrix Value {
            get {
                var result = Matrix.Identity;
                foreach (var child in Children) {
                    if (child != null) {
                        result = Matrix.Multiply(result, child.Value);
                    }
                }
                return result;
            }
        }
    }

    /// <summary>
    /// Represents formatted text for rendering.
    /// </summary>
    public class FormattedText {
        public string Text { get; set; }
        public System.Globalization.CultureInfo Culture { get; set; }
        public System.Windows.Media.FontFamily FontFamily { get; set; }
        public double FontSize { get; set; }
        public System.Windows.Media.Brush Foreground { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public FormattedText(string text, System.Globalization.CultureInfo culture, 
            System.Windows.FlowDirection flowDirection, System.Windows.Media.Typeface typeface, 
            double emSize, System.Windows.Media.Brush foreground) {
            Text = text;
            Culture = culture;
            FontSize = emSize;
            Foreground = foreground;
            CalculateDimensions();
        }

        public FormattedText(string text, System.Globalization.CultureInfo culture, 
            System.Windows.FlowDirection flowDirection, System.Windows.Media.Typeface typeface, 
            double emSize, System.Windows.Media.Brush foreground, double pixelsPerDip) {
            Text = text;
            Culture = culture;
            FontSize = emSize;
            Foreground = foreground;
            CalculateDimensions();
        }

        private void CalculateDimensions() {
            Width = Text?.Length * FontSize / 2 ?? 0;
            Height = FontSize;
        }

        public System.Windows.Size Measure() {
            return new System.Windows.Size(Width, Height);
        }
    }
}
