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
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Wanderer {
    /// <summary>
    /// Wanderer Rotator SDK wrapper for device control
    /// P/Invoke wrapper for WandererRotatorSDK.dll/.so
    /// </summary>
    public class WandererRotatorSDK {
        private const string DLLNAME = "libWandererRotatorSDK.so";

        static WandererRotatorSDK() {
            DllLoader.LoadDll(Path.Combine("Wanderer", DLLNAME));
        }

        // Constants
        public const int WR_MAX_NUM = 32;              // Maximum rotator numbers supported by this SDK
        public const int WR_VERSION_LEN = 32;          // Buffer length for version strings

        // Configuration masks for rotator
        public const uint MASK_ROTATOR_REVERSE_DIRECTION = 1 << 0;
        public const uint MASK_ROTATOR_BACKLASH = 1 << 1;
        public const uint MASK_ROTATOR_ALL = 1 << 8;

        /// <summary>
        /// Error types returned by SDK functions
        /// </summary>
        public enum WR_ERROR_TYPE {
            WR_SUCCESS = 0,                     // Success
            WR_ERROR_INVALID_ID,                // Device ID is invalid
            WR_ERROR_INVALID_PARAMETER,         // One or more parameters are invalid
            WR_ERROR_INVALID_STATE,             // Device is not in correct state for specific API call
            WR_ERROR_COMMUNICATION,             // Data communication error such as device has been removed from USB port
            WR_ERROR_NULL_POINTER,              // Caller passes null-pointer parameter which is not expected
        }

        /// <summary>
        /// Rotator version information structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct WR_VERSION {
            public uint firmware;               // Rotator firmware version
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public string model;                // Model type (e.g., "Lite", "Mini")
        }

        /// <summary>
        /// Rotator configuration structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WR_ROTATOR_CONFIG {
            public uint mask;                   // Bitmask for which fields to set
            public int reverseDirection;        // 0 - Not reverse, others - Reverse
            public float backlash;              // Backlash in degrees
        }

        /// <summary>
        /// Rotator status structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WR_ROTATOR_STATUS {
            public float position;              // Current motor position in degrees
            public int moving;                  // 0 - not moving, others - moving
            public int stepsPerRevolution;      // Steps per full revolution (hardware dependent)
            public float stepSize;              // Step size in degrees per step
        }

        // P/Invoke declarations
        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WR_ERROR_TYPE WRRotatorScan(out int number, [Out] int[] ids);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WR_ERROR_TYPE WRRotatorOpen(int id);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WR_ERROR_TYPE WRRotatorClose(int id);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WR_ERROR_TYPE WRRotatorGetConfig(int id, out WR_ROTATOR_CONFIG config);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WR_ERROR_TYPE WRRotatorSetConfig(int id, ref WR_ROTATOR_CONFIG config);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WR_ERROR_TYPE WRRotatorGetStatus(int id, out WR_ROTATOR_STATUS status);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WR_ERROR_TYPE WRRotatorGetVersion(int id, out WR_VERSION version);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WR_ERROR_TYPE WRRotatorFindHome(int id);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WR_ERROR_TYPE WRRotatorSyncPosition(int id, float angle);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WR_ERROR_TYPE WRRotatorMove(int id, float angle);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WR_ERROR_TYPE WRRotatorMoveTo(int id, float angle);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern WR_ERROR_TYPE WRRotatorStopMove(int id);

        [DllImport(DLLNAME, SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern WR_ERROR_TYPE WRGetSDKVersion(StringBuilder version);

        // Wrapper methods for easier use
        public static string GetSDKVersion() {
            var version = new StringBuilder(WR_VERSION_LEN);
            WRGetSDKVersion(version);
            return version.ToString();
        }

        public static WR_ERROR_TYPE RotatorScan(out int number, int[] ids) {
            return WRRotatorScan(out number, ids);
        }

        public static WR_ERROR_TYPE RotatorOpen(int id) {
            return WRRotatorOpen(id);
        }

        public static WR_ERROR_TYPE RotatorClose(int id) {
            return WRRotatorClose(id);
        }

        public static WR_ERROR_TYPE RotatorGetConfig(int id, out WR_ROTATOR_CONFIG config) {
            return WRRotatorGetConfig(id, out config);
        }

        public static WR_ERROR_TYPE RotatorSetConfig(int id, WR_ROTATOR_CONFIG config) {
            return WRRotatorSetConfig(id, ref config);
        }

        public static WR_ERROR_TYPE RotatorGetStatus(int id, out WR_ROTATOR_STATUS status) {
            return WRRotatorGetStatus(id, out status);
        }

        public static WR_ERROR_TYPE RotatorGetVersion(int id, out WR_VERSION version) {
            return WRRotatorGetVersion(id, out version);
        }

        public static WR_ERROR_TYPE RotatorFindHome(int id) {
            return WRRotatorFindHome(id);
        }

        public static WR_ERROR_TYPE RotatorSyncPosition(int id, float angle) {
            return WRRotatorSyncPosition(id, angle);
        }

        public static WR_ERROR_TYPE RotatorMove(int id, float angle) {
            return WRRotatorMove(id, angle);
        }

        public static WR_ERROR_TYPE RotatorMoveTo(int id, float angle) {
            return WRRotatorMoveTo(id, angle);
        }

        public static WR_ERROR_TYPE RotatorStopMove(int id) {
            return WRRotatorStopMove(id);
        }
    }
}