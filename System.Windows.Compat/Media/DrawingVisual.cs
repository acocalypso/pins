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

namespace System.Windows.Media {
    /// <summary>
    /// DrawingVisual is a visual object that can be used to render graphics on screen
    /// </summary>
    public class DrawingVisual : Visual {
        internal List<DrawingOperation> Operations { get; } = new List<DrawingOperation>();

        public DrawingContext RenderOpen() {
            Operations.Clear();
            return new DrawingContext(this);
        }
    }

    internal class DrawingOperation {
        public enum OperationType {
            DrawImage,
            DrawLine,
            DrawRectangle,
            DrawGeometry,
            DrawText
        }

        public OperationType Type { get; set; }
        public Imaging.BitmapSource Image { get; set; }
        public Rect Rect { get; set; }
        public Pen Pen { get; set; }
        public Brush Brush { get; set; }
        public Geometry Geometry { get; set; }
        public FormattedText FormattedText { get; set; }
        public Point Point1 { get; set; }
        public Point Point2 { get; set; }

        /// <summary>
        /// Transform in effect when the operation was issued, from the context's PushTransform
        /// stack, or null under the identity transform.
        /// </summary>
        public Transform Transform { get; set; }
    }

    /// <summary>
    /// DrawingContext is used to describe visual content
    /// </summary>
    public class DrawingContext : IDisposable {
        private readonly List<DrawingOperation> _operations;
        private readonly Stack<Transform> _transformStack = new Stack<Transform>();

        internal DrawingContext(DrawingVisual visual) {
            _operations = visual.Operations;
        }

        internal DrawingContext(DrawingGroup group) {
            _operations = group.Operations;
        }

        /// <summary>
        /// Pushes a transform onto the context's stack. Recorded onto each subsequent operation
        /// until the matching <see cref="Pop"/>, so a caller's intent survives even though this
        /// shim does not rasterize the recording.
        /// </summary>
        public void PushTransform(Transform transform) {
            _transformStack.Push(transform);
        }

        /// <summary>
        /// Pops the most recent push. Tolerates an unbalanced pop rather than throwing - a
        /// mismatch here would take down a caller that WPF would have let through.
        /// </summary>
        public void Pop() {
            if (_transformStack.Count > 0) {
                _transformStack.Pop();
            }
        }

        private Transform CurrentTransform => _transformStack.Count > 0 ? _transformStack.Peek() : null;

        private void Record(DrawingOperation operation) {
            operation.Transform = CurrentTransform;
            _operations.Add(operation);
        }

        public void DrawImage(Imaging.BitmapSource imageSource, Rect rectangle) {
            Record(new DrawingOperation {
                Type = DrawingOperation.OperationType.DrawImage,
                Image = imageSource,
                Rect = rectangle
            });
        }

        public void DrawLine(Pen pen, Point point1, Point point2) {
            Record(new DrawingOperation {
                Type = DrawingOperation.OperationType.DrawLine,
                Pen = pen,
                Point1 = point1,
                Point2 = point2
            });
        }

        public void DrawRectangle(Brush brush, Pen pen, Rect rectangle) {
            Record(new DrawingOperation {
                Type = DrawingOperation.OperationType.DrawRectangle,
                Brush = brush,
                Pen = pen,
                Rect = rectangle
            });
        }

        public void DrawGeometry(Brush brush, Pen pen, Geometry geometry) {
            Record(new DrawingOperation {
                Type = DrawingOperation.OperationType.DrawGeometry,
                Brush = brush,
                Pen = pen,
                Geometry = geometry
            });
        }

        public void DrawText(FormattedText text, Point point) {
            Record(new DrawingOperation {
                Type = DrawingOperation.OperationType.DrawText,
                FormattedText = text,
                Point1 = point
            });
        }

        public void DrawDrawing(Drawing drawing) {
            // Stub - drawing operations are not actually rendered in headless mode
            // This is called during image thumbnail creation but we handle it differently
        }

        public void Dispose() {
            // Context is closed
        }
    }
}
