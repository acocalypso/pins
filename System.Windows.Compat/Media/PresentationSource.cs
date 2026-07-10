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
    /// Minimal PresentationSource stub for DPI-aware operations
    /// </summary>
    public abstract class PresentationSource {
        public CompositionTarget CompositionTarget { get; protected set; }

        /// <summary>
        /// Gets the presentation source for a visual (stub implementation returns null)
        /// </summary>
        public static PresentationSource FromVisual(Visual visual) {
            // Stub implementation - returns null in non-WPF scenarios
            return null;
        }
    }

    /// <summary>
    /// Minimal CompositionTarget stub for DPI transforms
    /// </summary>
    public class CompositionTarget {
        /// <summary>
        /// Transform from device to DIP (device-independent pixels)
        /// </summary>
        public Matrix TransformFromDevice { get; set; } = Matrix.Identity;
    }

    /// <summary>
    /// Minimal Matrix struct for transform operations
    /// </summary>
    public struct Matrix {
        public static Matrix Identity => new Matrix { M11 = 1, M22 = 1, M12 = 0, M21 = 0, OffsetX = 0, OffsetY = 0 };
        
        public double M11 { get; set; }
        public double M12 { get; set; }
        public double M21 { get; set; }
        public double M22 { get; set; }
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }

        public Point Transform(Point point) {
            return new Point(
                point.X * M11 + point.Y * M21 + OffsetX,
                point.X * M12 + point.Y * M22 + OffsetY
            );
        }

        /// <summary>
        /// Row-vector composition: transforming by the result applies <paramref name="a"/>
        /// first, then <paramref name="b"/> (v * a * b), matching WPF's Matrix.Multiply.
        /// </summary>
        public static Matrix Multiply(Matrix a, Matrix b) {
            return new Matrix {
                M11 = a.M11 * b.M11 + a.M12 * b.M21,
                M12 = a.M11 * b.M12 + a.M12 * b.M22,
                M21 = a.M21 * b.M11 + a.M22 * b.M21,
                M22 = a.M21 * b.M12 + a.M22 * b.M22,
                OffsetX = a.OffsetX * b.M11 + a.OffsetY * b.M21 + b.OffsetX,
                OffsetY = a.OffsetX * b.M12 + a.OffsetY * b.M22 + b.OffsetY
            };
        }
    }
}
