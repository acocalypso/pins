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

namespace Nitecrawler {
    /// <summary>
    /// Nitecrawler SDK for Focuser and Rotator control
    /// P/Invoke wrapper for mlnc.dll/mlnc.so
    /// </summary>
    public class NitecrawlerSDK {
        private const string DLLNAME = "libNitecrawlerSDK.so";

        static NitecrawlerSDK() {
            DllLoader.LoadDll(Path.Combine("Nitecrawler", DLLNAME));
        }

        // Constants
        public const int NC_MAX_NUM = 32;                    // Maximum focuser numbers supported by this SDK
        public const int NC_VERSION_LEN = 32;               // Buffer length for version strings
        public const int NC_NAME_LEN = 32;                  // Buffer length for name strings
        public const uint TEMPERATURE_INVALID = 0xFFFFFFFF;   // Invalid temperature value

        // Configuration masks for device
        public const int MASK_DEVICE_BRIGHTNESS = 1 << 0;
        public const int MASK_DEVICE_SLEEP_BRIGHTNESS = 1 << 1;
        public const int MASK_DEVICE_VOLTAGE_OFFSET = 1 << 2;
        public const int MASK_DEVICE_ENCODERS = 1 << 3;
        public const int MASK_DEVICE_FLIP_DISPLAY = 1 << 4;
        public const int MASK_DEVICE_ALL = 1 << 8;

        // Configuration masks for focuser
        public const int MASK_FOCUSER_MAX_STEP = 1 << 0;
        public const int MASK_FOCUSER_BACKLASH = 1 << 1;
        public const int MASK_FOCUSER_BACKLASH_DIRECTION = 1 << 2;
        public const int MASK_FOCUSER_REVERSE_DIRECTION = 1 << 3;
        public const int MASK_FOCUSER_STEP_RATE = 1 << 4;
        public const int MASK_FOCUSER_TEMPERATURE_OFFSET = 1 << 5;
        public const int MASK_FOCUSER_ALL = 1 << 8;

        // Configuration masks for rotator
        public const int MASK_ROTATOR_REVERSE_DIRECTION = 1 << 0;
        public const int MASK_ROTATOR_STEP_RATE = 1 << 1;
        public const int MASK_ROTATOR_ALL = 1 << 8;

        /// <summary>
        /// Error types returned by SDK functions
        /// </summary>
        public enum NC_ERROR_TYPE {
            NC_SUCCESS = 0,                   // Success
            NC_ERROR_INVALID_ID,              // Device ID is invalid
            NC_ERROR_INVALID_PARAMETER,       // One or more parameters are invalid
            NC_ERROR_INVALID_STATE,           // Device is not in correct state for specific API call
            NC_ERROR_COMMUNICATION,           // Data communication error such as device has been removed from USB port
            NC_ERROR_NULL_POINTER,            // Caller passes null-pointer parameter which is not expected
        }

        /// <summary>
        /// Version information structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct NC_VERSION {
            public uint firmware;   // Nitecrawler firmware version
            public uint serial;     // Nitecrawler serial
        }

        /// <summary>
        /// Device configuration structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct NC_DEVICE_CONFIG {
            public uint mask;                   // Bitmask for which fields to set
            public int displayBrightness;       // Display brightness
            public int sleepBrightness;         // Sleep mode brightness
            public float voltageOffset;         // Voltage offset
            public int encoders;                // 0 - Disabled, 1 - Enabled
            public int flipDisplay;             // 0 - Normal, 1 - Flipped
        }

        /// <summary>
        /// Device status structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct NC_DEVICE_STATUS {
            public int dcPower;                 // Current DC power in 0.1V unit
        }

        /// <summary>
        /// Focuser configuration structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct NC_FOCUSER_CONFIG {
            public uint mask;                   // Bitmask for which fields to set
            public int maxStep;                 // Maximum step or position
            public int backlash;                // Backlash value
            public int backlashDirection;       // 0 - IN, others - OUT
            public int reverseDirection;        // 0 - Not reverse, others - Reverse
            public int stepRate;                // Step rate/delay (7-100)
            public float temperatureOffset;     // Temperature offset in degrees C (-15.0 to 15.0)
        }

        /// <summary>
        /// Focuser status structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct NC_FOCUSER_STATUS {
            public int temperatureExt;          // External (ambient) temperature in 0.01 degree unit
            public int temperatureDetection;    // 0 - no probe, others - probe inserted
            public int position;                // Current motor position
            public int moving;                  // 0 - not moving, others - moving
            public float micronsPerStep;        // Microns per step resolution
        }

        /// <summary>
        /// Rotator configuration structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct NC_ROTATOR_CONFIG {
            public uint mask;                   // Bitmask for which fields to set
            public int reverseDirection;        // 0 - Not reverse, others - Reverse
            public int stepRate;                // Step rate/delay (7-100)
        }

        /// <summary>
        /// Rotator status structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct NC_ROTATOR_STATUS {
            public float position;              // Current motor position in degrees
            public int moving;                  // 0 - not moving, others - moving
            public int stepsPerRevolution;      // Steps per full revolution (hardware dependent)
            public float stepSize;              // Step size in degrees per step
        }

        // P/Invoke declarations
        [DllImport(DLLNAME, SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern NC_ERROR_TYPE NCGetProductModel(int id, StringBuilder model);

        [DllImport(DLLNAME, SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern NC_ERROR_TYPE NCGetSDKVersion(StringBuilder version);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCGetVersion(int id, out NC_VERSION version);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCGetConfig(int id, out NC_DEVICE_CONFIG config);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCSetConfig(int id, ref NC_DEVICE_CONFIG config);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCGetStatus(int id, out NC_DEVICE_STATUS status);

        // Focuser functions
        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCFocuserScan(out int number, [Out] int[] ids);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCFocuserOpen(int id);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCFocuserClose(int id);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCFocuserGetConfig(int id, out NC_FOCUSER_CONFIG config);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCFocuserSetConfig(int id, ref NC_FOCUSER_CONFIG config);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCFocuserGetStatus(int id, out NC_FOCUSER_STATUS status);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCFocuserFindHome(int id);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCFocuserSyncPosition(int id, int position);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCFocuserMove(int id, int step);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCFocuserMoveTo(int id, int position);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCFocuserStopMove(int id);

        // Rotator functions
        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCRotatorScan(out int number, [Out] int[] ids);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCRotatorOpen(int id);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCRotatorClose(int id);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCRotatorGetConfig(int id, out NC_ROTATOR_CONFIG config);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCRotatorSetConfig(int id, ref NC_ROTATOR_CONFIG config);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCRotatorGetStatus(int id, out NC_ROTATOR_STATUS status);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCRotatorFindHome(int id);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCRotatorSyncPosition(int id, int position);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCRotatorMove(int id, float angle);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCRotatorMoveTo(int id, float angle);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern NC_ERROR_TYPE NCRotatorStopMove(int id);

        // Wrapper methods for easier use
        public static string GetProductModel(int id) {
            var model = new StringBuilder(NC_NAME_LEN);
            NCGetProductModel(id, model);
            return model.ToString();
        }

        public static string GetSDKVersion() {
            var version = new StringBuilder(NC_VERSION_LEN);
            NCGetSDKVersion(version);
            return version.ToString();
        }

        public static NC_ERROR_TYPE GetVersion(int id, out NC_VERSION version) {
            return NCGetVersion(id, out version);
        }

        public static NC_ERROR_TYPE GetDeviceConfig(int id, out NC_DEVICE_CONFIG config) {
            return NCGetConfig(id, out config);
        }

        public static NC_ERROR_TYPE SetDeviceConfig(int id, NC_DEVICE_CONFIG config) {
            return NCSetConfig(id, ref config);
        }

        public static NC_ERROR_TYPE GetDeviceStatus(int id, out NC_DEVICE_STATUS status) {
            return NCGetStatus(id, out status);
        }

        // Focuser wrapper methods
        public static NC_ERROR_TYPE ScanFocusers(out int number, [Out] int[] ids) {
            return NCFocuserScan(out number, ids);
        }

        public static NC_ERROR_TYPE FocuserOpen(int id) => NCFocuserOpen(id);
        public static NC_ERROR_TYPE FocuserClose(int id) => NCFocuserClose(id);

        public static NC_ERROR_TYPE GetFocuserConfig(int id, out NC_FOCUSER_CONFIG config) {
            return NCFocuserGetConfig(id, out config);
        }

        public static NC_ERROR_TYPE SetFocuserConfig(int id, NC_FOCUSER_CONFIG config) {
            return NCFocuserSetConfig(id, ref config);
        }

        public static NC_ERROR_TYPE GetFocuserStatus(int id, out NC_FOCUSER_STATUS status) {
            return NCFocuserGetStatus(id, out status);
        }

        public static NC_ERROR_TYPE FocuserFindHome(int id) => NCFocuserFindHome(id);
        public static NC_ERROR_TYPE FocuserSyncPosition(int id, int position) => NCFocuserSyncPosition(id, position);
        public static NC_ERROR_TYPE FocuserMove(int id, int step) => NCFocuserMove(id, step);
        public static NC_ERROR_TYPE FocuserMoveTo(int id, int position) => NCFocuserMoveTo(id, position);
        public static NC_ERROR_TYPE FocuserStopMove(int id) => NCFocuserStopMove(id);

        // Rotator wrapper methods
        public static NC_ERROR_TYPE ScanRotators(out int number, [Out] int[] ids) {
            return NCRotatorScan(out number, ids);
        }

        public static NC_ERROR_TYPE RotatorOpen(int id) => NCRotatorOpen(id);
        public static NC_ERROR_TYPE RotatorClose(int id) => NCRotatorClose(id);

        public static NC_ERROR_TYPE GetRotatorConfig(int id, out NC_ROTATOR_CONFIG config) {
            return NCRotatorGetConfig(id, out config);
        }

        public static NC_ERROR_TYPE SetRotatorConfig(int id, NC_ROTATOR_CONFIG config) {
            return NCRotatorSetConfig(id, ref config);
        }

        public static NC_ERROR_TYPE GetRotatorStatus(int id, out NC_ROTATOR_STATUS status) {
            return NCRotatorGetStatus(id, out status);
        }

        public static NC_ERROR_TYPE RotatorFindHome(int id) => NCRotatorFindHome(id);
        public static NC_ERROR_TYPE RotatorSyncPosition(int id, int position) => NCRotatorSyncPosition(id, position);
        public static NC_ERROR_TYPE RotatorMove(int id, float angle) => NCRotatorMove(id, angle);
        public static NC_ERROR_TYPE RotatorMoveTo(int id, float angle) => NCRotatorMoveTo(id, angle);
        public static NC_ERROR_TYPE RotatorStopMove(int id) => NCRotatorStopMove(id);
    }
}
