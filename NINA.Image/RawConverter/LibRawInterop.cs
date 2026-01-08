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
using System.Runtime.InteropServices;

namespace NINA.Image.RawConverter {

    /// <summary>
    /// P/Invoke wrapper for LibRaw (libraw.so)
    /// LibRaw is a library for reading RAW image files from digital cameras
    /// </summary>
    public static class LibRawInterop {

        private const string LIBRAW_DLL = "libraw.so";

        /// <summary>
        /// Create a new LibRaw processor instance
        /// </summary>
        [DllImport(LIBRAW_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libraw_init(uint flags);

        /// <summary>
        /// Free a LibRaw processor instance
        /// </summary>
        [DllImport(LIBRAW_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void libraw_close(IntPtr proc);

        /// <summary>
        /// Open RAW file from memory buffer
        /// Returns 0 on success, non-zero on error
        /// </summary>
        [DllImport(LIBRAW_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int libraw_open_buffer(IntPtr proc, [In] byte[] buffer, uint size);

        /// <summary>
        /// Unpack the RAW data
        /// Returns 0 on success, non-zero on error
        /// </summary>
        [DllImport(LIBRAW_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int libraw_unpack(IntPtr proc);

        /// <summary>
        /// Obtain image from RAW
        /// Returns 0 on success, non-zero on error
        /// </summary>
        [DllImport(LIBRAW_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int libraw_raw2image(IntPtr proc);

        /// <summary>
        /// Get image width
        /// </summary>
        [DllImport(LIBRAW_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort libraw_get_iwidth(IntPtr proc);

        /// <summary>
        /// Get image height
        /// </summary>
        [DllImport(LIBRAW_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern ushort libraw_get_iheight(IntPtr proc);

        /// <summary>
        /// Get error string from last operation
        /// </summary>
        [DllImport(LIBRAW_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libraw_strerror(int errorcode);
    }
}
