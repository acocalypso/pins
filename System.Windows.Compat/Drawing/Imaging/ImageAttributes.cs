#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace System.Drawing.Drawing2D {

    /// <summary>
    /// Specifies how a texture or gradient is tiled when it is smaller than the area being filled.
    /// </summary>
    public enum WrapMode {
        Tile = 0,
        TileFlipX = 1,
        TileFlipY = 2,
        TileFlipXY = 3,
        Clamp = 4
    }
}

namespace System.Drawing.Imaging {

    /// <summary>
    /// Carries the per-draw image rendering settings that GDI+ accepts on DrawImage.
    /// Only the wrap mode is honoured here, because that is the only setting the renderers
    /// in this solution rely on; it selects the OpenCV border mode used when sampling
    /// outside the source rectangle.
    /// </summary>
    public class ImageAttributes : IDisposable {

        /// <summary>
        /// The wrap mode set by <see cref="SetWrapMode(System.Drawing.Drawing2D.WrapMode)"/>.
        /// Defaults to Clamp, matching GDI+.
        /// </summary>
        internal System.Drawing.Drawing2D.WrapMode WrapMode { get; private set; } = System.Drawing.Drawing2D.WrapMode.Clamp;

        public void SetWrapMode(System.Drawing.Drawing2D.WrapMode mode) {
            WrapMode = mode;
        }

        public void SetWrapMode(System.Drawing.Drawing2D.WrapMode mode, Color color) {
            WrapMode = mode;
        }

        public void SetWrapMode(System.Drawing.Drawing2D.WrapMode mode, Color color, bool clamp) {
            WrapMode = mode;
        }

        public void Dispose() {
            // No unmanaged GDI+ handle backs this object; Dispose exists for source compatibility.
            GC.SuppressFinalize(this);
        }
    }
}
