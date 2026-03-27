#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FreeImageAPI;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Image.Interfaces;
using System;
using System.Runtime.InteropServices;

namespace NINA.Image.RawConverter {

    public class RawConverterFactory {

        // Versioned SO names in preference order.
        // libraw 0.21+ ships SONAME libraw.so.20; libraw 0.20.x shipped libraw.so.19.
        // The bare "libraw.so" symlink only exists when the -dev package is installed.
        private static readonly string[] LibRawVersionedNames = [
            "libraw.so.23", "libraw.so.22", "libraw.so.21", "libraw.so.20", "libraw.so.19"
        ];

        static RawConverterFactory() {
            IntPtr freeImageHandle = IntPtr.Zero;
            IntPtr libRawHandle = IntPtr.Zero;

            DllImportResolver resolver = (libraryName, assembly, searchPath) => {
                if (libraryName == "FreeImage") {
                    if (freeImageHandle == IntPtr.Zero) {
                        if (NativeLibrary.TryLoad("libfreeimage.so.3", out freeImageHandle)) {
                            Logger.Info("FreeImage: resolved 'FreeImage' → system libfreeimage.so.3");
                        } else if (NativeLibrary.TryLoad("libfreeimage.so", out freeImageHandle)) {
                            Logger.Info("FreeImage: resolved 'FreeImage' → libfreeimage.so");
                        }
                    }
                    return freeImageHandle;
                }

                if (libraryName == "libraw.so") {
                    if (libRawHandle == IntPtr.Zero) {
                        foreach (var name in LibRawVersionedNames) {
                            if (NativeLibrary.TryLoad(name, out libRawHandle)) {
                                Logger.Info($"LibRaw: resolved 'libraw.so' → '{name}'");
                                break;
                            }
                        }
                    }
                    return libRawHandle;
                }

                return IntPtr.Zero;
            };

            // Register on NINA.Image (covers LibRawInterop's DllImports)
            NativeLibrary.SetDllImportResolver(typeof(RawConverterFactory).Assembly, resolver);
            // Register on FreeImage.Standard (covers FreeImageAPI's DllImport("FreeImage"))
            NativeLibrary.SetDllImportResolver(typeof(FreeImage).Assembly, resolver);
        }

        public static IRawConverter CreateInstance(RawConverterEnum converter, IImageDataFactory imageDataFactory) {
            switch (converter) {
                case RawConverterEnum.DCRAW:
                    return new DCRaw(imageDataFactory);

                case RawConverterEnum.LIBRAW:
                    return new LibRawConverter(imageDataFactory);

                case RawConverterEnum.FREEIMAGE:
                    return new FreeImageConverter(imageDataFactory);

                default:
                    return new FreeImageConverter(imageDataFactory);
            }
        }
    }
}