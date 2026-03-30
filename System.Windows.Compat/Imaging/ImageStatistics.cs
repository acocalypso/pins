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
using System.Drawing;

namespace Accord.Imaging {
    /// <summary>
    /// Image statistics calculator using OpenCV
    /// Computes various statistical measures for image pixels
    /// </summary>
    public class ImageStatistics {
        public HistogramStatistics Gray { get; private set; }
        public HistogramStatistics GrayWithoutBlack { get; private set; }

        public ImageStatistics() {
            Gray = new HistogramStatistics();
            GrayWithoutBlack = new HistogramStatistics();
        }

        public ImageStatistics(Bitmap image) {
            Mat mat = image;

            if (mat.Empty()) {
                Gray = new HistogramStatistics();
                GrayWithoutBlack = new HistogramStatistics();
                return;
            }

            // Convert to single-channel if needed
            Mat grayMat = mat;
            if (mat.Channels() > 1) {
                grayMat = new Mat();
                Cv2.CvtColor(mat, grayMat, ColorConversionCodes.BGR2GRAY);
            }

            // Determine histogram parameters based on image bit depth
            bool is16Bit = grayMat.Depth() == MatType.CV_16U;
            int numBins = is16Bit ? 65536 : 256;
            float rangeMax = is16Bit ? 65536f : 256f;

            int[] histSize = { numBins };
            Rangef[] ranges = { new Rangef(0, rangeMax) };
            Mat hist = new Mat();
            Cv2.CalcHist(new Mat[] { grayMat }, new int[] { 0 }, null, hist, 1, histSize, ranges);

            Gray = ComputeStatistics(hist, numBins, 0);

            // Calculate histogram without black pixels (value > 0)
            Mat mask = new Mat();
            if (is16Bit) {
                Cv2.Threshold(grayMat, mask, 0, 65535, ThresholdTypes.Binary);
                mask.ConvertTo(mask, MatType.CV_8UC1, 255.0 / 65535.0);
            } else {
                Cv2.Threshold(grayMat, mask, 0, 255, ThresholdTypes.Binary);
            }

            Mat histNoBlack = new Mat();
            Cv2.CalcHist(new Mat[] { grayMat }, new int[] { 0 }, mask, histNoBlack, 1, histSize, ranges);

            GrayWithoutBlack = ComputeStatistics(histNoBlack, numBins, 1);

            // Cleanup
            hist.Dispose();
            histNoBlack.Dispose();
            mask.Dispose();
            if (grayMat != mat) {
                grayMat.Dispose();
            }
        }

        private static HistogramStatistics ComputeStatistics(Mat hist, int numBins, int startBin) {
            double sum = 0, weightedSum = 0;
            int count = 0;
            int min = numBins, max = -1;

            for (int i = startBin; i < numBins; i++) {
                int binCount = (int)hist.At<float>(i);
                if (binCount > 0) {
                    sum += binCount;
                    weightedSum += (double)i * binCount;
                    count += binCount;
                    if (min > i) min = i;
                    if (max < i) max = i;
                }
            }

            double mean = count > 0 ? weightedSum / sum : 0;

            double variance = 0;
            for (int i = startBin; i < numBins; i++) {
                int binCount = (int)hist.At<float>(i);
                if (binCount > 0) {
                    double diff = i - mean;
                    variance += diff * diff * binCount;
                }
            }
            double stdDev = count > 0 ? System.Math.Sqrt(variance / sum) : 0;

            double median = 0;
            int medianCount = (int)(sum / 2);
            int cumulative = 0;
            for (int i = startBin; i < numBins; i++) {
                cumulative += (int)hist.At<float>(i);
                if (cumulative >= medianCount) {
                    median = i;
                    break;
                }
            }

            return new HistogramStatistics {
                Mean = mean,
                StdDev = stdDev,
                Median = median,
                Min = min,
                Max = max,
                PixelsCount = count
            };
        }
    }

    /// <summary>
    /// Histogram statistics - stores mean and standard deviation
    /// Inherits from Accord.Statistics.Visualizations.Histogram for API compatibility
    /// </summary>
    public class HistogramStatistics : Statistics.Visualizations.Histogram {
        public HistogramStatistics() : base(new int[256]) {
        }

        public double Mean { get; set; }
        public double StdDev { get; set; }
        public new double Median { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
        public int PixelsCount { get; set; }
    }
}
