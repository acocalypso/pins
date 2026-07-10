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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Accord.Imaging;

namespace Accord.Imaging.Filters {
    /// <summary>
    /// Base class for filters that implement ApplyInPlace
    /// </summary>
    public abstract class BaseUsingCopyPartialFilter {
        /// <summary>
        /// Format translations dictionary.
        /// </summary>
        public virtual Dictionary<PixelFormat, PixelFormat> FormatTranslations { get; }

        /// <summary>
        /// Process the filter on the specified image.
        /// Must be implemented by derived classes to provide the actual filter logic.
        /// </summary>
        protected abstract unsafe void ProcessFilter(UnmanagedImage source, UnmanagedImage destination, Rectangle rect);

        /// <summary>
        /// Applies the filter in-place to the bitmap
        /// </summary>
        public unsafe void ApplyInPlace(Bitmap image) {
            Mat mat = image;

            // ProcessFilter's contract is a read-only source and a separate destination.
            // Handing both UnmanagedImages the same Mat lets destination writes feed back into
            // source reads for any neighborhood-based filter - give the source its own copy.
            using (Mat sourceCopy = mat.Clone())
            using (var unmanagedSrc = UnmanagedImage.FromMat(sourceCopy))
            using (var unmanagedDst = UnmanagedImage.FromMat(mat)) {
                var rect = new Rectangle(0, 0, image.Width, image.Height);
                ProcessFilter(unmanagedSrc, unmanagedDst, rect);

                // Copy result back to Mat
                unmanagedDst.CopyToMat(mat);
            }
        }

        /// <summary>
        /// Applies the filter and returns a new bitmap
        /// </summary>
        public virtual unsafe Bitmap Apply(Bitmap source) {
            Mat sourceMat = source;
            Mat result = new Mat();
            sourceMat.CopyTo(result);

            using (var unmanagedSrc = UnmanagedImage.FromMat(sourceMat))
            using (var unmanagedDst = UnmanagedImage.FromMat(result)) {
                var rect = new Rectangle(0, 0, source.Width, source.Height);
                ProcessFilter(unmanagedSrc, unmanagedDst, rect);
                unmanagedDst.CopyToMat(result);
            }

            return new Bitmap(result);
        }
    }
}
