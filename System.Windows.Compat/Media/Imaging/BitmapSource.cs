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

namespace System.Windows.Media.Imaging {
    // BitmapSource wraps OpenCvSharp.Mat for headless/OpenCV mode
    public class BitmapSource : ImageSource {
        protected Mat _mat;
        private bool _disposed;
        // Bytes of unmanaged memory reported to GC so it can schedule collections appropriately
        private long _memoryPressure;

        public bool CanFreeze => true;

        public BitmapSource() {
            _mat = new Mat();
        }

        public BitmapSource(Mat source) {
            _mat = source;
            AddMemoryPressure();
        }

        // Constructor for cropping: new BitmapSource(mat, rectangle)
        public BitmapSource(Mat source, System.Drawing.Rectangle rect) {
            // Crop the source Mat using the rectangle
            var cvRect = new OpenCvSharp.Rect(rect.X, rect.Y, rect.Width, rect.Height);
            // Clone the ROI to avoid dangling reference to parent Mat
            // Creating Mat(source, cvRect) creates a view that references source.
            // If source is disposed, this view becomes invalid. Clone it instead.
            using (Mat roiMat = new Mat(source, cvRect)) {
                _mat = roiMat.Clone();
            }
            AddMemoryPressure();
        }

        protected void AddMemoryPressure() {
            if (_memoryPressure > 0) {
                GC.RemoveMemoryPressure(_memoryPressure);
                _memoryPressure = 0;
            }
            if (_mat != null && !_mat.Empty()) {
                _memoryPressure = (long)(_mat.Total() * _mat.ElemSize());
                if (_memoryPressure > 0)
                    GC.AddMemoryPressure(_memoryPressure);
            }
        }

        ~BitmapSource() {
            Dispose(false);
        }

        public override void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (_disposed) return;
            _disposed = true;
            if (_memoryPressure > 0) {
                GC.RemoveMemoryPressure(_memoryPressure);
                _memoryPressure = 0;
            }
            if (disposing) {
                _mat?.Dispose();
                _mat = null;
            }
        }

        public int PixelWidth => _mat.Width;
        public int PixelHeight => _mat.Height;
        public double DpiX { get; set; } = 96.0;
        public double DpiY { get; set; } = 96.0;

        public int Width => _mat.Width;
        public int Height => _mat.Height;

        public System.Windows.Media.PixelFormat Format {
            get {
                // Map OpenCV MatType to WPF PixelFormat
                if (_mat.Empty()) return System.Windows.Media.PixelFormats.Bgr24;

                var matType = _mat.Type();
                if (matType == MatType.CV_8UC1)
                    return System.Windows.Media.PixelFormats.Gray8;
                else if (matType == MatType.CV_16UC1)
                    return System.Windows.Media.PixelFormats.Gray16;
                else if (matType == MatType.CV_8UC3)
                    return System.Windows.Media.PixelFormats.Bgr24;
                else if (matType == MatType.CV_8UC4)
                    return System.Windows.Media.PixelFormats.Bgra32;
                else if (matType == MatType.CV_16UC3)
                    return System.Windows.Media.PixelFormats.Rgb48;
                else
                    return System.Windows.Media.PixelFormats.Bgr24;
            }
            set {
                // Optionally handle setting format (convert the Mat to the desired format)
                // This is rarely used in practice but may be needed for compatibility
            }
        }

        // Implicit conversions
        // Clone to prevent aliased ownership — caller owns the returned Mat
        public static implicit operator Mat(BitmapSource bmp) => bmp._mat?.Clone();
        public static implicit operator BitmapSource(Mat mat) => new BitmapSource(mat);

        public void Freeze() {
            // In WPF, Freeze makes objects immutable for thread safety
            // For OpenCV Mat, this is a no-op since Mat is already thread-safe for reading
        }

        public void CopyPixels(byte[] pixels, int stride, int offset) {
            // Copy Mat data to byte array
            if (_mat.Empty()) return;

            int bytesPerPixel = _mat.ElemSize();
            int dataSize = _mat.Rows * _mat.Cols * bytesPerPixel;

            if (pixels.Length < offset + dataSize) {
                throw new ArgumentException($"Destination array too small. Need {offset + dataSize} bytes, got {pixels.Length}");
            }

            // Copy the Mat data directly to the byte array
            System.Runtime.InteropServices.Marshal.Copy(_mat.Data, pixels, offset, dataSize);
        }

        public void CopyPixels(ushort[] pixels, int stride, int offset) {
            // Copy Mat data to ushort array
            if (_mat.Empty()) return;

            int dataSize = _mat.Rows * _mat.Cols * _mat.Channels();

            if (pixels.Length < offset + dataSize) {
                throw new ArgumentException($"Destination array too small. Need {offset + dataSize} elements, got {pixels.Length}");
            }

            // Copy as raw bytes then reinterpret — Marshal.Copy does not accept ushort[]
            int byteCount = dataSize * sizeof(ushort);
            byte[] bytes = new byte[byteCount];
            System.Runtime.InteropServices.Marshal.Copy(_mat.Data, bytes, 0, byteCount);
            System.Buffer.BlockCopy(bytes, 0, pixels, offset * sizeof(ushort), byteCount);
        }

        public void CopyPixels(Array pixels, int stride, int offset) {
            // Generic CopyPixels that dispatches to specific type
            if (pixels is byte[] byteArray) {
                CopyPixels(byteArray, stride, offset);
            } else if (pixels is ushort[] ushortArray) {
                CopyPixels(ushortArray, stride, offset);
            } else {
                throw new NotSupportedException($"Array type {pixels.GetType()} not supported");
            }
        }

        public void CopyPixels(Int32Rect sourceRect, IntPtr buffer, int bufferSize, int stride) {
            // Copy Mat data to a buffer pointer
            if (_mat.Empty()) return;

            // If sourceRect is empty, copy the entire image
            int srcX = sourceRect.IsEmpty ? 0 : sourceRect.X;
            int srcY = sourceRect.IsEmpty ? 0 : sourceRect.Y;
            int width = sourceRect.IsEmpty ? _mat.Width : sourceRect.Width;
            int height = sourceRect.IsEmpty ? _mat.Height : sourceRect.Height;

            // Ensure we don't exceed bounds
            width = Math.Min(width, _mat.Width - srcX);
            height = Math.Min(height, _mat.Height - srcY);

            // Copy the data
            if (sourceRect.IsEmpty && stride == _mat.Step()) {
                // Fast path: copy entire image in one go
                int dataSize = (int)(_mat.Step() * _mat.Height);
                unsafe {
                    Buffer.MemoryCopy(_mat.Data.ToPointer(), buffer.ToPointer(), bufferSize, Math.Min(dataSize, bufferSize));
                }
            } else {
                // Copy row by row
                int rowBytes = Math.Min(width * _mat.ElemSize(), stride);
                for (int y = 0; y < height; y++) {
                    IntPtr srcPtr = _mat.Data + ((srcY + y) * (int)_mat.Step()) + (srcX * _mat.ElemSize());
                    IntPtr dstPtr = buffer + (y * stride);
                    unsafe {
                        Buffer.MemoryCopy(srcPtr.ToPointer(), dstPtr.ToPointer(), bufferSize - (y * stride), rowBytes);
                    }
                }
            }
        }

        public static BitmapSource Create(int pixelWidth, int pixelHeight, double dpiX, double dpiY,
            System.Windows.Media.PixelFormat pixelFormat, BitmapPalette palette, IntPtr buffer, int bufferSize, int stride) {
            // Determine the OpenCV Mat type from the pixel format
            MatType matType = pixelFormat;

            // Create a Mat that owns its data, not just a view
            // Mat.FromPixelData creates a view that references the buffer, which may become invalid
            Mat mat = new Mat(pixelHeight, pixelWidth, matType);

            // Always copy row by row to properly handle stride mismatches and buffer bounds.
            // When a bitmap is locked with one format but converted to another, stride calculations
            // can mismatch, causing buffer overruns. This approach is safe against all variants.
            int rowBytes = (int)System.Math.Min((long)stride, mat.Step());
            int maxRowsToCopy = bufferSize / (stride > 0 ? stride : 1);
            int rowsToCopy = System.Math.Min(pixelHeight, maxRowsToCopy);
            
            for (int y = 0; y < rowsToCopy; y++) {
                IntPtr srcPtr = buffer + (y * stride);
                IntPtr dstPtr = mat.Data + (y * (int)mat.Step());
                unsafe {
                    System.Buffer.MemoryCopy(srcPtr.ToPointer(), dstPtr.ToPointer(), mat.Step(), rowBytes);
                }
            }

            return new BitmapSource(mat);
        }

        public static BitmapSource Create(int pixelWidth, int pixelHeight, double dpiX, double dpiY,
            System.Windows.Media.PixelFormat pixelFormat, BitmapPalette palette, Array pixels, int stride) {
            // Determine the OpenCV Mat type from the pixel format
            MatType matType = pixelFormat;

            // Create an empty Mat with the specified dimensions
            Mat mat = new Mat(pixelHeight, pixelWidth, matType);

            // Copy the pixel data using the same approach as the backup
            if (pixels is ushort[] ushortArray) {
                int copyElements = (int)System.Math.Min((long)pixelWidth * pixelHeight * mat.Channels(), ushortArray.Length);
                int copyBytes = copyElements * sizeof(ushort);

                byte[] bytes = new byte[copyBytes];
                System.Buffer.BlockCopy(ushortArray, 0, bytes, 0, copyBytes);
                System.Runtime.InteropServices.Marshal.Copy(bytes, 0, mat.Data, copyBytes);
            } else if (pixels is byte[] byteArray) {
                int copyBytes = (int)System.Math.Min(mat.Total() * mat.ElemSize(), byteArray.Length);
                System.Runtime.InteropServices.Marshal.Copy(byteArray, 0, mat.Data, copyBytes);
            } else {
                // Fallback: pin and copy
                var handle = System.Runtime.InteropServices.GCHandle.Alloc(pixels, System.Runtime.InteropServices.GCHandleType.Pinned);
                try {
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    using (Mat tempMat = Mat.FromPixelData(pixelHeight, pixelWidth, matType, ptr, stride)) {
                        tempMat.CopyTo(mat);
                    }
                } finally {
                    handle.Free();
                }
            }

            return new BitmapSource(mat);
        }
    }

    public class BitmapPalette {
        public BitmapPalette(System.Collections.Generic.List<System.Windows.Media.Color> colors) { }
    }

    public static class BitmapPalettes {
        private static BitmapPalette _gray256;

        public static BitmapPalette Gray256 {
            get {
                if (_gray256 == null) {
                    var colors = new System.Collections.Generic.List<System.Windows.Media.Color>(256);
                    for (int i = 0; i < 256; i++) {
                        byte value = (byte)i;
                        colors.Add(System.Windows.Media.Color.FromRgb(value, value, value));
                    }
                    _gray256 = new BitmapPalette(colors);
                }
                return _gray256;
            }
        }
    }

    public class WriteableBitmap : BitmapSource {
        public WriteableBitmap(int pixelWidth, int pixelHeight, double dpiX, double dpiY,
            System.Windows.Media.PixelFormat pixelFormat, BitmapPalette palette) : base() {
            // Create a new Mat with the specified dimensions and format
            MatType matType = pixelFormat;
            _mat = new Mat(pixelHeight, pixelWidth, matType);
            AddMemoryPressure();
        }

        public WriteableBitmap(BitmapSource source) : base() {
            // Implicit operator already clones — take ownership directly
            if (source != null) {
                Mat sourceMat = (Mat)source;
                if (sourceMat != null && !sourceMat.Empty()) {
                    _mat = sourceMat;
                } else {
                    sourceMat?.Dispose();
                    _mat = new Mat();
                }
            } else {
                _mat = new Mat();
            }
            AddMemoryPressure();
        }
    }

    public class BitmapImage : BitmapSource {
        public BitmapImage() : base() { }
        public BitmapImage(Uri uriSource) : base() { }

        public Uri UriSource { get; set; }
        public System.IO.Stream StreamSource { get; set; }
        public BitmapCacheOption CacheOption { get; set; }

        public void BeginInit() { }
        public void EndInit() {
            // Load from StreamSource if set
            if (StreamSource != null) {
                SetSource(StreamSource);
            }
        }

        public new void Freeze() {
            // In WPF, Freeze makes objects immutable for thread safety
            // For OpenCV Mat, this is a no-op since Mat is already thread-safe for reading
        }

        public void SetSource(System.IO.Stream stream) {
            // Load image from stream using OpenCV
            if (stream != null && stream.CanSeek) {
                stream.Position = 0;
                var bytes = new byte[stream.Length];
                int bytesRead = 0;
                int offset = 0;
                while (offset < bytes.Length && (bytesRead = stream.Read(bytes, offset, bytes.Length - offset)) > 0) {
                    offset += bytesRead;
                }
                var mat = OpenCvSharp.Cv2.ImDecode(bytes, OpenCvSharp.ImreadModes.Unchanged);
                if (mat != null && !mat.Empty()) {
                    _mat?.Dispose();
                    _mat = mat;
                    AddMemoryPressure();
                } else {
                    mat?.Dispose();
                }
            }
        }
    }

    public enum BitmapCacheOption {
        Default = 0,
        OnDemand = 0,
        OnLoad = 1,
        None = 2
    }

    /// <summary>
    /// RenderTargetBitmap renders visual content into a bitmap
    /// </summary>
    public class RenderTargetBitmap : BitmapSource {
        public RenderTargetBitmap(int pixelWidth, int pixelHeight, double dpiX, double dpiY,
            System.Windows.Media.PixelFormat pixelFormat) : base() {
            // Create the target Mat based on pixel format
            MatType matType = pixelFormat;
            _mat = new Mat(pixelHeight, pixelWidth, matType);

            // Initialize with transparent/black background
            if (matType == MatType.CV_8UC4) {
                _mat.SetTo(new OpenCvSharp.Scalar(0, 0, 0, 0)); // Transparent
            } else {
                _mat.SetTo(OpenCvSharp.Scalar.All(0)); // Black
            }
            AddMemoryPressure();
        }

        /// <summary>
        /// Renders a DrawingVisual onto this bitmap
        /// </summary>
        public void Render(System.Windows.Media.DrawingVisual visual) {
            if (visual == null || _mat == null || _mat.Empty()) {
                return;
            }

            // Process each drawing operation
            foreach (var operation in visual.Operations) {
                switch (operation.Type) {
                    case System.Windows.Media.DrawingOperation.OperationType.DrawImage:
                        RenderDrawImage(operation);
                        break;
                    case System.Windows.Media.DrawingOperation.OperationType.DrawLine:
                        RenderDrawLine(operation);
                        break;
                    case System.Windows.Media.DrawingOperation.OperationType.DrawRectangle:
                        RenderDrawRectangle(operation);
                        break;
                    case System.Windows.Media.DrawingOperation.OperationType.DrawText:
                        RenderDrawText(operation);
                        break;
                    case System.Windows.Media.DrawingOperation.OperationType.DrawGeometry:
                        // Geometry path decomposition is complex; not yet implemented
                        break;
                }
            }
        }

        private void RenderDrawImage(System.Windows.Media.DrawingOperation operation) {
            if (operation.Image == null) return;

            using Mat sourceMat = (Mat)operation.Image;
            if (sourceMat == null || sourceMat.Empty()) return;

            int x = (int)operation.Rect.X;
            int y = (int)operation.Rect.Y;
            int width = (int)operation.Rect.Width;
            int height = (int)operation.Rect.Height;

            if (x < 0 || y < 0 || x + width > _mat.Width || y + height > _mat.Height) {
                return;
            }

            Mat resizedSource = sourceMat;
            if (sourceMat.Width != width || sourceMat.Height != height) {
                resizedSource = new Mat();
                OpenCvSharp.Cv2.Resize(sourceMat, resizedSource, new OpenCvSharp.Size(width, height));
            }

            try {
                Mat convertedSource = resizedSource;
                if (resizedSource.Type() != _mat.Type()) {
                    convertedSource = new Mat();
                    if (resizedSource.Channels() == 1 && _mat.Channels() == 4) {
                        OpenCvSharp.Cv2.CvtColor(resizedSource, convertedSource, OpenCvSharp.ColorConversionCodes.GRAY2BGRA);
                    } else if (resizedSource.Channels() == 3 && _mat.Channels() == 4) {
                        OpenCvSharp.Cv2.CvtColor(resizedSource, convertedSource, OpenCvSharp.ColorConversionCodes.BGR2BGRA);
                    } else if (resizedSource.Channels() == 4 && _mat.Channels() == 3) {
                        OpenCvSharp.Cv2.CvtColor(resizedSource, convertedSource, OpenCvSharp.ColorConversionCodes.BGRA2BGR);
                    } else {
                        resizedSource.ConvertTo(convertedSource, _mat.Depth());
                    }
                }

                var roi = new OpenCvSharp.Rect(x, y, width, height);
                using (var targetRoi = new Mat(_mat, roi)) {
                    convertedSource.CopyTo(targetRoi);
                }

                if (convertedSource != resizedSource) {
                    convertedSource.Dispose();
                }
            } finally {
                if (resizedSource != sourceMat) {
                    resizedSource.Dispose();
                }
            }
        }

        private static OpenCvSharp.Scalar GetScalarFromBrush(System.Windows.Media.Brush brush) {
            if (brush is System.Windows.Media.SolidColorBrush scb) {
                var c = scb.Color;
                return new OpenCvSharp.Scalar(c.B, c.G, c.R, c.A);
            }
            return new OpenCvSharp.Scalar(255, 255, 255, 255);
        }

        private void RenderDrawLine(System.Windows.Media.DrawingOperation operation) {
            if (operation.Pen == null) return;
            var color = GetScalarFromBrush(operation.Pen.Brush);
            int thickness = System.Math.Max(1, (int)operation.Pen.Thickness);
            OpenCvSharp.Cv2.Line(_mat,
                new OpenCvSharp.Point((int)operation.Point1.X, (int)operation.Point1.Y),
                new OpenCvSharp.Point((int)operation.Point2.X, (int)operation.Point2.Y),
                color, thickness);
        }

        private void RenderDrawRectangle(System.Windows.Media.DrawingOperation operation) {
            var rect = new OpenCvSharp.Rect(
                (int)operation.Rect.X, (int)operation.Rect.Y,
                (int)operation.Rect.Width, (int)operation.Rect.Height);
            if (operation.Brush != null) {
                var fillColor = GetScalarFromBrush(operation.Brush);
                OpenCvSharp.Cv2.Rectangle(_mat, rect, fillColor, -1);
            }
            if (operation.Pen != null) {
                var penColor = GetScalarFromBrush(operation.Pen.Brush);
                int thickness = System.Math.Max(1, (int)operation.Pen.Thickness);
                OpenCvSharp.Cv2.Rectangle(_mat, rect, penColor, thickness);
            }
        }

        private void RenderDrawText(System.Windows.Media.DrawingOperation operation) {
            if (operation.FormattedText == null) return;
            string text = operation.FormattedText.Text ?? "";
            var color = GetScalarFromBrush(operation.FormattedText.Foreground);
            double fontScale = operation.FormattedText.FontSize / 20.0;
            OpenCvSharp.Cv2.PutText(_mat, text,
                new OpenCvSharp.Point((int)operation.Point1.X, (int)operation.Point1.Y),
                OpenCvSharp.HersheyFonts.HersheySimplex, fontScale, color, 1);
        }
    }
}
