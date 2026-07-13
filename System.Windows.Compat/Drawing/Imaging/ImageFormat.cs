#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace System.Drawing.Imaging {
    /// <summary>
    /// Specifies the format of an image
    /// </summary>
    public partial class ImageFormat {
        private readonly string _name;

        private ImageFormat(string name, System.Guid guid) {
            _name = name;
            Guid = guid;
        }

        // The GUIDs are GDI+'s canonical format identifiers (same values as the real
        // System.Drawing.Imaging.ImageFormat). They must be distinct: Bitmap.Save and the
        // ImageCodecInfo entries route on FormatID equality, and defaulted Guid.Empty values
        // made every encoder compare equal to Jpeg.
        public static readonly ImageFormat Png = new ImageFormat("PNG", new System.Guid("b96b3caf-0728-11d3-9d7b-0000f81ef32e"));
        public static readonly ImageFormat Jpeg = new ImageFormat("JPEG", new System.Guid("b96b3cae-0728-11d3-9d7b-0000f81ef32e"));
        public static readonly ImageFormat Bmp = new ImageFormat("BMP", new System.Guid("b96b3cab-0728-11d3-9d7b-0000f81ef32e"));
        public static readonly ImageFormat Tiff = new ImageFormat("TIFF", new System.Guid("b96b3cb1-0728-11d3-9d7b-0000f81ef32e"));
        public static readonly ImageFormat Gif = new ImageFormat("GIF", new System.Guid("b96b3cb0-0728-11d3-9d7b-0000f81ef32e"));

        public override string ToString() => _name;
    }
}

namespace System.Drawing.Imaging {
    public partial class ImageFormat {
        public System.Guid Guid { get; private set; }
    }
}
