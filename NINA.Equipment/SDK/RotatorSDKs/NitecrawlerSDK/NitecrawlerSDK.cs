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
        public const int MLNC_MAX_NUM = 32;                    // Maximum focuser numbers supported by this SDK
        public const int MLNC_VERSION_LEN = 32;               // Buffer length for version strings
        public const int MLNC_NAME_LEN = 32;                  // Buffer length for name strings
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
        public enum MLNC_ERROR_TYPE {
            MLNC_SUCCESS = 0,                   // Success
            MLNC_ERROR_INVALID_ID,              // Device ID is invalid
            MLNC_ERROR_INVALID_PARAMETER,       // One or more parameters are invalid
            MLNC_ERROR_INVALID_STATE,           // Device is not in correct state for specific API call
            MLNC_ERROR_COMMUNICATION,           // Data communication error such as device has been removed from USB port
            MLNC_ERROR_NULL_POINTER,            // Caller passes null-pointer parameter which is not expected
        }

        /// <summary>
        /// Version information structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MLNC_VERSION {
            public uint hardware;   // Focuser hardware version
            public uint firmware;   // Focuser firmware version
        }

        /// <summary>
        /// Device configuration structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MLNC_DEVICE_CONFIG {
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
        public struct MLNC_DEVICE_STATUS {
            public int dcPower;                 // Current DC power in 0.1V unit
        }

        /// <summary>
        /// Focuser configuration structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MLNC_FOCUSER_CONFIG {
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
        public struct MLNC_FOCUSER_STATUS {
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
        public struct MLNC_ROTATOR_CONFIG {
            public uint mask;                   // Bitmask for which fields to set
            public int reverseDirection;        // 0 - Not reverse, others - Reverse
            public int stepRate;                // Step rate/delay (7-100)
        }

        /// <summary>
        /// Rotator status structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MLNC_ROTATOR_STATUS {
            public float position;              // Current motor position in degrees
            public int moving;                  // 0 - not moving, others - moving
            public int stepsPerRevolution;      // Steps per full revolution (hardware dependent)
            public float stepSize;              // Step size in degrees per step
        }

        // P/Invoke declarations
        [DllImport(DLLNAME, SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern MLNC_ERROR_TYPE MLNCGetProductModel(int id, StringBuilder model);

        [DllImport(DLLNAME, SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern MLNC_ERROR_TYPE MLNCGetSDKVersion(StringBuilder version);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCGetVersion(int id, out MLNC_VERSION version);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCGetConfig(int id, out MLNC_DEVICE_CONFIG config);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCSetConfig(int id, ref MLNC_DEVICE_CONFIG config);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCGetStatus(int id, out MLNC_DEVICE_STATUS status);

        // Focuser functions
        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCFocuserScan(out int number, [Out] int[] ids);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCFocuserOpen(int id);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCFocuserClose(int id);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCFocuserGetConfig(int id, out MLNC_FOCUSER_CONFIG config);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCFocuserSetConfig(int id, ref MLNC_FOCUSER_CONFIG config);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCFocuserGetStatus(int id, out MLNC_FOCUSER_STATUS status);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCFocuserFindHome(int id);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCFocuserSyncPosition(int id, int position);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCFocuserMove(int id, int step);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCFocuserMoveTo(int id, int position);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCFocuserStopMove(int id);

        // Rotator functions
        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCRotatorScan(out int number, [Out] int[] ids);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCRotatorOpen(int id);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCRotatorClose(int id);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCRotatorGetConfig(int id, out MLNC_ROTATOR_CONFIG config);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCRotatorSetConfig(int id, ref MLNC_ROTATOR_CONFIG config);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCRotatorGetStatus(int id, out MLNC_ROTATOR_STATUS status);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCRotatorFindHome(int id);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCRotatorSyncPosition(int id, int position);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCRotatorMove(int id, float angle);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCRotatorMoveTo(int id, float angle);

        [DllImport(DLLNAME, SetLastError = true)]
        private static extern MLNC_ERROR_TYPE MLNCRotatorStopMove(int id);

        // Wrapper methods for easier use
        public static string GetProductModel(int id) {
            var model = new StringBuilder(MLNC_NAME_LEN);
            MLNCGetProductModel(id, model);
            return model.ToString();
        }

        public static string GetSDKVersion() {
            var version = new StringBuilder(MLNC_VERSION_LEN);
            MLNCGetSDKVersion(version);
            return version.ToString();
        }

        public static MLNC_ERROR_TYPE GetVersion(int id, out MLNC_VERSION version) {
            return MLNCGetVersion(id, out version);
        }

        public static MLNC_ERROR_TYPE GetDeviceConfig(int id, out MLNC_DEVICE_CONFIG config) {
            return MLNCGetConfig(id, out config);
        }

        public static MLNC_ERROR_TYPE SetDeviceConfig(int id, MLNC_DEVICE_CONFIG config) {
            return MLNCSetConfig(id, ref config);
        }

        public static MLNC_ERROR_TYPE GetDeviceStatus(int id, out MLNC_DEVICE_STATUS status) {
            return MLNCGetStatus(id, out status);
        }

        // Focuser wrapper methods
        public static MLNC_ERROR_TYPE ScanFocusers(out int number, [Out] int[] ids) {
            return MLNCFocuserScan(out number, ids);
        }

        public static MLNC_ERROR_TYPE FocuserOpen(int id) => MLNCFocuserOpen(id);
        public static MLNC_ERROR_TYPE FocuserClose(int id) => MLNCFocuserClose(id);

        public static MLNC_ERROR_TYPE GetFocuserConfig(int id, out MLNC_FOCUSER_CONFIG config) {
            return MLNCFocuserGetConfig(id, out config);
        }

        public static MLNC_ERROR_TYPE SetFocuserConfig(int id, MLNC_FOCUSER_CONFIG config) {
            return MLNCFocuserSetConfig(id, ref config);
        }

        public static MLNC_ERROR_TYPE GetFocuserStatus(int id, out MLNC_FOCUSER_STATUS status) {
            return MLNCFocuserGetStatus(id, out status);
        }

        public static MLNC_ERROR_TYPE FocuserFindHome(int id) => MLNCFocuserFindHome(id);
        public static MLNC_ERROR_TYPE FocuserSyncPosition(int id, int position) => MLNCFocuserSyncPosition(id, position);
        public static MLNC_ERROR_TYPE FocuserMove(int id, int step) => MLNCFocuserMove(id, step);
        public static MLNC_ERROR_TYPE FocuserMoveTo(int id, int position) => MLNCFocuserMoveTo(id, position);
        public static MLNC_ERROR_TYPE FocuserStopMove(int id) => MLNCFocuserStopMove(id);

        // Rotator wrapper methods
        public static MLNC_ERROR_TYPE ScanRotators(out int number, [Out] int[] ids) {
            return MLNCRotatorScan(out number, ids);
        }

        public static MLNC_ERROR_TYPE RotatorOpen(int id) => MLNCRotatorOpen(id);
        public static MLNC_ERROR_TYPE RotatorClose(int id) => MLNCRotatorClose(id);

        public static MLNC_ERROR_TYPE GetRotatorConfig(int id, out MLNC_ROTATOR_CONFIG config) {
            return MLNCRotatorGetConfig(id, out config);
        }

        public static MLNC_ERROR_TYPE SetRotatorConfig(int id, MLNC_ROTATOR_CONFIG config) {
            return MLNCRotatorSetConfig(id, ref config);
        }

        public static MLNC_ERROR_TYPE GetRotatorStatus(int id, out MLNC_ROTATOR_STATUS status) {
            return MLNCRotatorGetStatus(id, out status);
        }

        public static MLNC_ERROR_TYPE RotatorFindHome(int id) => MLNCRotatorFindHome(id);
        public static MLNC_ERROR_TYPE RotatorSyncPosition(int id, int position) => MLNCRotatorSyncPosition(id, position);
        public static MLNC_ERROR_TYPE RotatorMove(int id, float angle) => MLNCRotatorMove(id, angle);
        public static MLNC_ERROR_TYPE RotatorMoveTo(int id, float angle) => MLNCRotatorMoveTo(id, angle);
        public static MLNC_ERROR_TYPE RotatorStopMove(int id) => MLNCRotatorStopMove(id);
    }
}