#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Image.RawConverter {

    /// <summary>
    /// RAW converter using LibRaw library
    /// LibRaw is specifically designed for RAW image processing and works reliably on Linux
    /// </summary>
    internal class LibRawConverter : IRawConverter {
        private readonly IImageDataFactory imageDataFactory;

        public LibRawConverter(IImageDataFactory imageDataFactory) {
            this.imageDataFactory = imageDataFactory;
            try {
                DllLoader.LoadDll(Path.Combine("LibRaw", "libraw.so"));
            } catch (Exception ex) {
                Logger.Error($"Failed to load LibRaw: {ex.Message}");
                throw;
            }
        }

        public Task<IImageData> Convert(
            MemoryStream s,
            int bitDepth,
            string rawType,
            ImageMetaData metaData,
            CancellationToken token = default) {
            return Task.Run(() => {
                using (MyStopWatch.Measure("LibRaw Conversion")) {
                    IntPtr processor = IntPtr.Zero;
                    try {
                        // Create LibRaw processor instance
                        processor = LibRawInterop.libraw_init(0);
                        if (processor == IntPtr.Zero) {
                            throw new Exception("Failed to initialize LibRaw processor");
                        }

                        // Get raw bytes from stream
                        byte[] rawBytes = s.ToArray();

                        Logger.Debug($"LibRaw: Processing {rawBytes.Length} bytes");

                        // Open RAW from buffer
                        int ret = LibRawInterop.libraw_open_buffer(processor, rawBytes, (uint)rawBytes.Length);
                        if (ret != 0) {
                            throw new Exception($"LibRaw open_buffer failed: {GetLibRawError(ret)}");
                        }

                        Logger.Debug("LibRaw: Buffer opened successfully");

                        // Unpack the RAW data
                        ret = LibRawInterop.libraw_unpack(processor);
                        if (ret != 0) {
                            throw new Exception($"LibRaw unpack failed: {GetLibRawError(ret)}");
                        }

                        Logger.Debug("LibRaw: Data unpacked successfully");

                        // Obtain image from RAW data
                        ret = LibRawInterop.libraw_raw2image(processor);
                        if (ret != 0) {
                            throw new Exception($"LibRaw raw2image failed: {GetLibRawError(ret)}");
                        }

                        Logger.Debug("LibRaw: Data obtained successfully");

                        // Get image dimensions before processing
                        ushort width = LibRawInterop.libraw_get_iwidth(processor);
                        ushort height = LibRawInterop.libraw_get_iheight(processor);

                        Logger.Debug($"LibRaw: Image dimensions {width}x{height}");

                        // libraw_data_t is the first entry in the LibRaw class and raw image data
                        // is the first entry in the libraw_data_t struct (which is a ushort (*image)[4])
                        // containing the RGGB data (undemosaiced raw sensor data)

                        // The processor IS the libraw_data_t structure
                        // The first field in libraw_data_t is the image pointer (ushort (*image)[4])
                        IntPtr imagePtr = Marshal.ReadIntPtr(processor, 0);

                        if (imagePtr == IntPtr.Zero) {
                            throw new Exception("Image data pointer is null");
                        }

                        if (width == 0 || height == 0) {
                            throw new Exception($"Invalid image dimensions: {width}x{height}");
                        }

                        // Calculate total ushort values: width * height * 4 (RGBG)
                        // The ushort (*image)[4] declaration means 4 ushort values per pixel:
                        // (*image)[0] = R, (*image)[1] = G, (*image)[2] = B, (*image)[3] = G2
                        int totalPixels = width * height;

                        // Convert RGBG data directly from unmanaged memory to single-channel Bayer image
                        // No intermediate buffer - read directly from the pointer
                        ushort[] bayerImageSingleChannel = new ushort[totalPixels];
                        
                        unsafe {
                            ushort* pImage = (ushort*)imagePtr;
                            for (int i = 0; i < totalPixels; i++) {
                                int baseIdx = i * 4;
                                ushort r = pImage[baseIdx];
                                ushort g = pImage[baseIdx + 1];
                                ushort b = pImage[baseIdx + 2];
                                ushort g2 = pImage[baseIdx + 3];

                                // Use the non-zero value (Bayer pattern encodes which channel applies to each pixel)
                                ushort value = (r > 0) ? r : (g > 0) ? g : (b > 0) ? b : g2;
                                bayerImageSingleChannel[i] = value;
                            }
                        }

                        Logger.Debug("LibRaw: Converted RGBG data to single-channel Bayer image (no intermediate copy)");

                        // Create image data from single-channel Bayer image (matching FreeImage format)
                        var imageArray = new ImageArray(flatArray: bayerImageSingleChannel, rawData: rawBytes, rawType: rawType);
                        var data = imageDataFactory.CreateBaseImageData(
                            imageArray: imageArray,
                            width: width,
                            height: height,
                            bitDepth: bitDepth,
                            isBayered: true,
                            metaData: metaData);

                        Logger.Debug("LibRaw: Bayer image data created successfully (single-channel format)");

                        return Task.FromResult<IImageData>(data);
                    } catch (Exception ex) {
                        Logger.Error($"LibRaw conversion failed: {ex.Message}");
                        throw;
                    } finally {
                        if (processor != IntPtr.Zero) {
                            LibRawInterop.libraw_close(processor);
                        }
                    }
                }
            });
        }

        private static string GetLibRawError(int errorCode) {
            try {
                IntPtr errorPtr = LibRawInterop.libraw_strerror(errorCode);
                if (errorPtr != IntPtr.Zero) {
                    return Marshal.PtrToStringAnsi(errorPtr) ?? $"Unknown error code {errorCode}";
                }
            } catch {
                // Ignore
            }
            return $"Unknown error code {errorCode}";
        }
    }
}
