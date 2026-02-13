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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Wanderer {
    /// <summary>
    /// Wanderer Cover SDK wrapper for device control
    /// P/Invoke wrapper for WandererCoverSDK.dll/.so
    /// </summary>
    public class WandererCoverSDK {
        private const string DLLNAME = "libWandererCoverSDK.so";

        static WandererCoverSDK() {
            DllLoader.LoadDll(Path.Combine("Wanderer", DLLNAME));
        }

        // Constants
        public const int WC_MAX_NUM = 32;              // Maximum cover numbers supported by this SDK
        public const int WC_VERSION_LEN = 32;          // Buffer length for version strings

        // Configuration masks for cover
        public const uint MASK_COVER_BRIGHTNESS = 0x01;
        public const uint MASK_COVER_HEATER_POWER = 0x02;
        public const uint MASK_COVER_ASIAIR_CONTROL = 0x04;
        public const uint MASK_COVER_OPEN_POSITION = 0x08;
        public const uint MASK_COVER_CLOSE_POSITION = 0x10;
        public const uint MASK_COVER_ALL = 0x1F;

        /// <summary>
        /// Error types returned by SDK functions
        /// </summary>
        public enum WC_ERROR_TYPE {
            WC_SUCCESS = 0,                     // Success
            WC_ERROR_INVALID_ID,                // Device ID is invalid
            WC_ERROR_INVALID_PARAMETER,         // One or more parameters are invalid
            WC_ERROR_INVALID_STATE,             // Device is not in correct state for specific API call
            WC_ERROR_COMMUNICATION,             // Data communication error such as device has been removed from USB port
            WC_ERROR_NULL_POINTER,              // Caller passes null-pointer parameter which is not expected
        }

        /// <summary>
        /// Cover version information structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct WC_VERSION {
            public uint firmware;               // Cover firmware version
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public string model;                // Model type (e.g., "Lite", "Mini")
        }

        /// <summary>
        /// Cover configuration structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WC_COVER_CONFIG {
            public uint mask;         // Used by WRCoverSetConfig() to indicate which field wants to be set
            public float openPositionAngle;  // Open position angle in degrees
            public float closePositionAngle; // Close position angle in degrees
            public int brightness;            // Brightness level
            public int heaterPower;       // Heater power level
            public int asiairControl;		  // ASIAIR control setting
        }

        /// <summary>
        /// Cover status structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WC_COVER_STATUS {
            public int coverState;              // Current cover state (0 = close, 1 = open, 2 = intermediate, 3 = moving)
            public float currentPositionAngle; // Current motor position angle
            public float closePositionAngle;    // Closed position angle
            public float openPositionAngle;	// Open position angle
        }

        // P/Invoke declarations
        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WC_ERROR_TYPE WCCoverScan(out int number, [Out] int[] ids);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WC_ERROR_TYPE WCCoverOpen(int id);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WC_ERROR_TYPE WCCoverClose(int id);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WC_ERROR_TYPE WCCoverGetConfig(int id, out WC_COVER_CONFIG config);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WC_ERROR_TYPE WCCoverSetConfig(int id, ref WC_COVER_CONFIG config);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WC_ERROR_TYPE WCCoverGetStatus(int id, out WC_COVER_STATUS status);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WC_ERROR_TYPE WCCoverGetVersion(int id, out WC_VERSION version);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WC_ERROR_TYPE WCCoverOpenCover(int id);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WC_ERROR_TYPE WCCoverCloseCover(int id);

        [DllImport(DLLNAME, SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern WC_ERROR_TYPE WCGetSDKVersion(StringBuilder version);

        // Wrapper methods for easier use
        public static string GetSDKVersion() {
            var version = new StringBuilder(WC_VERSION_LEN);
            WCGetSDKVersion(version);
            return version.ToString();
        }

        public static WC_ERROR_TYPE CoverScan(out int number, int[] ids) {
            return WCCoverScan(out number, ids);
        }

        public static WC_ERROR_TYPE CoverOpen(int id) {
            return WCCoverOpen(id);
        }

        public static WC_ERROR_TYPE CoverClose(int id) {
            return WCCoverClose(id);
        }

        public static WC_ERROR_TYPE CoverGetConfig(int id, out WC_COVER_CONFIG config) {
            return WCCoverGetConfig(id, out config);
        }

        public static WC_ERROR_TYPE CoverSetConfig(int id, WC_COVER_CONFIG config) {
            return WCCoverSetConfig(id, ref config);
        }

        public static WC_ERROR_TYPE CoverGetStatus(int id, out WC_COVER_STATUS status) {
            return WCCoverGetStatus(id, out status);
        }

        public static WC_ERROR_TYPE CoverGetVersion(int id, out WC_VERSION version) {
            return WCCoverGetVersion(id, out version);
        }

        public static WC_ERROR_TYPE CoverOpenCover(int id) {
            return WCCoverOpenCover(id);
        }

        public static WC_ERROR_TYPE CoverCloseCover(int id) {
            return WCCoverCloseCover(id);
        }
    }
}