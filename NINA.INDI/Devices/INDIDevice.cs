#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.INDI.Enums;
using NINA.INDI.Protocol;
using NINA.INDI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;

namespace NINA.INDI.Devices {

    public class PropertyEventArgs : EventArgs {
        public INDIProperty Property { get; }

        public PropertyEventArgs(INDIProperty property) {
            Property = property;
        }
    }

    public class INDIDevice : IINDIDevice {

        private readonly INDIDeviceInfo _device;

        public INDIDevice(INDIDeviceInfo device) {
            _device = device;

            // Register device to receive property updates
            INDIClient.Instance.RegisterDevice(this);

            // Request fresh properties from the driver
            INDIClient.Instance.GetProperties(Id);

            // Wait for CONNECTION property (always required, according to INDI)
            string[] requiredProps = ["CONNECTION"];
            var propTimeout = DateTime.Now.AddSeconds(10);
            while (!HasProperties(requiredProps) && DateTime.Now < propTimeout)
            {
                CoreUtil.Wait(TimeSpan.FromMilliseconds(500)).Wait();
            }

            // Other connection properties
            _hasConnectionModeProperty = HasProperties(["CONNECTION_MODE"]);
            _hasDevicePortProperty = HasProperties(["DEVICE_PORT"]);
            _hasDeviceBaudRateProperty = HasProperties(["DEVICE_BAUD_RATE"]);
            _hasAutoSearchProperty = HasProperties(["DEVICE_AUTO_SEARCH"]);
        }

        private readonly bool _hasConnectionModeProperty;
        private readonly bool _hasDevicePortProperty;
        private readonly bool _hasDeviceBaudRateProperty;
        private readonly bool _hasAutoSearchProperty;

        private bool _connected;
        private bool _connectionAttemptFailed;  // Track if the last connection attempt failed

        public bool Connected {
            get => _connected;
            set {
                if (_connected && !value) {
                    // Transitioning from connected to disconnected
                    Disconnect();
                }
                _connected = value;
            }
        }

        public string Category => "INDI Device";
        public string Id => _device.Id;
        public string DeviceName => _device.Name;
        public string Name => _device.Name;
        public string DisplayName => DeviceName;
        public string Description => $"INDI Device: {DeviceName}";
        public string DriverInfo => _device?.Driver ?? "INDI Driver";
        public string DriverVersion => _device?.Version ?? "1.0";

        private readonly Dictionary<string, INDIProperty> _properties = new();
        private TaskCompletionSource<bool> _propertiesReadyTcs;

        public void AddProperty(INDIProperty property) {
            lock (_properties) {
                _properties[property.Name] = property;
                
                // Signal when CONNECTION property arrives (if we're waiting)
                if (property.Name == "CONNECTION" && _propertiesReadyTcs != null && !_propertiesReadyTcs.Task.IsCompleted) {
                    Logger.Debug($"Device {DeviceName}: CONNECTION property received");
                    _propertiesReadyTcs.TrySetResult(true);
                }
            }
        }

        public void RemoveProperty(string propertyName) {
            lock (_properties) {
                if (_properties.TryGetValue(propertyName, out var prop)) {
                    _properties.Remove(propertyName);
                }
            }
        }

        public INDIProperty GetProperty(string propertyName) {
            lock (_properties) {
                _properties.TryGetValue(propertyName, out var property);
                return property;
            }
        }

        public INDINumberProperty GetNumberProperty(string propertyName) {
            return GetProperty(propertyName) as INDINumberProperty;
        }

        public INDISwitchProperty GetSwitchProperty(string propertyName) {
            return GetProperty(propertyName) as INDISwitchProperty;
        }

        public INDITextProperty GetTextProperty(string propertyName) {
            return GetProperty(propertyName) as INDITextProperty;
        }

        public double? GetNumberPropertyValue(string propertyName, string elementName) {
            var prop = GetNumberProperty(propertyName);
            return prop?.Numbers.FirstOrDefault(n => n.Name == elementName)?.Value;
        }

        public bool? GetSwitchPropertyValue(string propertyName, string elementName) {
            var prop = GetSwitchProperty(propertyName);
            return prop?.Switches.FirstOrDefault(s => s.Name == elementName)?.Value;
        }

        public string GetTextPropertyValue(string propertyName, string elementName) {
            var prop = GetTextProperty(propertyName);
            return prop?.Texts.FirstOrDefault(t => t.Name == elementName)?.Value;
        }

        public void SetNumberValue(string propertyName, string elementName, double value) {
            var prop = GetNumberProperty(propertyName) ?? throw new ArgumentException($"Number property '{propertyName}' not found");
            if (prop == null) return;

            var number = prop.Numbers.FirstOrDefault(n => n.Name == elementName);
            if (number == null) return;

            number.Value = value;
            INDIClient.Instance.SendProperty(prop);
        }

        public void SetNumberValues(string propertyName, params (string elementName, double value)[] values)
        {
            var prop = GetNumberProperty(propertyName) ?? throw new ArgumentException($"Number property '{propertyName}' not found");
            if (prop == null) return;

            foreach (var (elementName, value) in values)
            {
                var number = prop.Numbers.FirstOrDefault(n => n.Name == elementName);
                if (number != null)
                {
                    number.Value = value;
                }
            }

            INDIClient.Instance.SendProperty(prop);
        }
        
        public async Task<bool> SetNumberValuesAsync(string propertyName, TimeSpan timeout, params (string elementName, double value)[] values)
        {
            try
            {
                // Create a unique operation ID for this async set
                var operationId = $"{propertyName}_{Guid.NewGuid()}";
                
                // Create and register the TaskCompletionSource
                var tcs = new TaskCompletionSource<bool>();
                lock (_asyncOperationsLock)
                {
                    _pendingAsyncOperations[operationId] = tcs;
                }

                try
                {
                    // Execute the actual set operation
                    SetNumberValues(propertyName, values);

                    // Wait for server acknowledgement (property state changes to Busy) with timeout
                    var timeoutTask = Task.Delay(timeout);
                    var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        Logger.Error($"SetNumberValuesAsync ({propertyName}) - server did not acknowledge within timeout");
                        return false;
                    }

                    var result = await tcs.Task;
                    if (!result)
                    {
                        Logger.Error($"SetNumberValuesAsync ({propertyName}) - server rejected operation (Alert state)");
                        return false;
                    }

                    // Server acknowledged the operation (state changed to Busy)
                    Logger.Debug($"SetNumberValuesAsync ({propertyName}) - server acknowledged, operation proceeding");
                    return true;
                }
                finally
                {
                    // Clean up the pending operation
                    lock (_asyncOperationsLock)
                    {
                        _pendingAsyncOperations.Remove(operationId);
                    }
                }
            }
            catch(OperationCanceledException)
            {
                Logger.Warning($"SetNumberValuesAsync ({propertyName}) was cancelled");
                return false;
            }
            catch(Exception ex)
            {
                Logger.Error($"SetNumberValuesAsync failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SetSwitchValueAsync(string propertyName, string elementName, bool value, TimeSpan timeout)
        {
            try
            {
                // Create a unique operation ID for this async set
                var operationId = $"{propertyName}_{Guid.NewGuid()}";
                
                // Create and register the TaskCompletionSource
                var tcs = new TaskCompletionSource<bool>();
                lock (_asyncOperationsLock)
                {
                    _pendingAsyncOperations[operationId] = tcs;
                }

                try
                {
                    // Execute the actual set operation
                    SetSwitchValue(propertyName, elementName, value);

                    // Wait for server acknowledgement (property state changes to Busy) with timeout
                    var timeoutTask = Task.Delay(timeout);
                    var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        Logger.Error($"SetSwitchValueAsync ({propertyName}) - server did not acknowledge within timeout");
                        return false;
                    }

                    var result = await tcs.Task;
                    if (!result)
                    {
                        Logger.Error($"SetSwitchValueAsync ({propertyName}) - server rejected operation (Alert state)");
                        return false;
                    }

                    // Server acknowledged the operation (state changed to Busy)
                    Logger.Debug($"SetSwitchValueAsync ({propertyName}) - server acknowledged, operation proceeding");
                    return true;
                }
                finally
                {
                    // Clean up the pending operation
                    lock (_asyncOperationsLock)
                    {
                        _pendingAsyncOperations.Remove(operationId);
                    }
                }
            }
            catch(OperationCanceledException)
            {
                Logger.Warning($"SetSwitchValueAsync ({propertyName}) was cancelled");
                return false;
            }
            catch(Exception ex)
            {
                Logger.Error($"SetSwitchValueAsync failed: {ex.Message}");
                return false;
            }
        }

        public void SetSwitchValue(string propertyName, string elementName, bool value) {
            var prop = GetSwitchProperty(propertyName) ?? throw new ArgumentException($"Switch property '{propertyName}' not found");

            // Handle switch rules
            if (prop.Rule == SwitchRule.OneOfMany) {
                // For OneOfMany, only allow setting one switch to true at a time
                // First, turn off all switches
                foreach (var sw in prop.Switches) {
                    sw.Value = false;
                }
                // Then turn on only the requested one (if value is true)
                if (value) {
                    var targetSw = prop.Switches.FirstOrDefault(s => s.Name == elementName);
                    if (targetSw != null) {
                        targetSw.Value = true;
                    }
                }
            } else if (prop.Rule == SwitchRule.AtMostOne) {
                if (value) {
                    // Turn off all other switches
                    foreach (var sw in prop.Switches) {
                        sw.Value = sw.Name == elementName;
                    }
                } else {
                    // Just turn off this switch, leave others as is
                    var sw = prop.Switches.FirstOrDefault(s => s.Name == elementName);
                    if (sw != null) {
                        sw.Value = false;
                    }
                }
            } else // AnyOfMany
              {
                var sw = prop.Switches.FirstOrDefault(s => s.Name == elementName);
                if (sw != null) {
                    sw.Value = value;
                }
            }

            INDIClient.Instance.SendProperty(prop);
        }

        public void SetSwitchProperty(string propertyName, Dictionary<string, bool> values) {
            var prop = GetSwitchProperty(propertyName) ?? throw new ArgumentException($"Switch property '{propertyName}' not found");
            if (prop == null) return;

            // Validate based on switch rule
            if (prop.Rule == SwitchRule.OneOfMany) {
                // Must have exactly one switch set to true
                var trueCount = values.Values.Count(v => v);
                if (trueCount != 1) {
                    throw new ArgumentException($"OneOfMany rule requires exactly one switch to be true, got {trueCount}");
                }
            } else if (prop.Rule == SwitchRule.AtMostOne) {
                // Can have at most one switch set to true
                var trueCount = values.Values.Count(v => v);
                if (trueCount > 1) {
                    throw new ArgumentException($"AtMostOne rule allows at most one switch to be true, got {trueCount}");
                }
            }
            // AnyOfMany has no restrictions

            // Apply the values
            foreach (var sw in prop.Switches) {
                if (values.TryGetValue(sw.Name, out bool value)) {
                    sw.Value = value;
                }
            }

            INDIClient.Instance.SendProperty(prop);
        }

        public void SetTextValue(string propertyName, string elementName, string value) {
            var prop = GetTextProperty(propertyName) ?? throw new ArgumentException($"Text property '{propertyName}' not found");
            if (prop == null) return;

            var text = prop.Texts.FirstOrDefault(t => t.Name == elementName);
            if (text == null) return;

            text.Value = value;
            INDIClient.Instance.SendProperty(prop);
        }

        public async Task<bool> SetTextValueAsync(string propertyName, string elementName, string value, TimeSpan timeout)
        {
            try
            {
                // Create a unique operation ID for this async set
                var operationId = $"{propertyName}_{Guid.NewGuid()}";
                
                // Create and register the TaskCompletionSource
                var tcs = new TaskCompletionSource<bool>();
                lock (_asyncOperationsLock)
                {
                    _pendingAsyncOperations[operationId] = tcs;
                }

                try
                {
                    // Execute the actual set operation
                    SetTextValue(propertyName, elementName, value);

                    // Wait for server acknowledgement (property state changes to Busy) with timeout
                    var timeoutTask = Task.Delay(timeout);
                    var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        Logger.Error($"SetTextValueAsync ({propertyName}) - server did not acknowledge within timeout");
                        return false;
                    }

                    var result = await tcs.Task;
                    if (!result)
                    {
                        Logger.Error($"SetTextValueAsync ({propertyName}) - server rejected operation (Alert state)");
                        return false;
                    }

                    // Server acknowledged the operation (state changed to Busy)
                    Logger.Debug($"SetTextValueAsync ({propertyName}) - server acknowledged, operation proceeding");
                    return true;
                }
                finally
                {
                    // Clean up the pending operation
                    lock (_asyncOperationsLock)
                    {
                        _pendingAsyncOperations.Remove(operationId);
                    }
                }
            }
            catch(OperationCanceledException)
            {
                Logger.Warning($"SetTextValueAsync ({propertyName}) was cancelled");
                return false;
            }
            catch(Exception ex)
            {
                Logger.Error($"SetTextValueAsync failed: {ex.Message}");
                return false;
            }
        }

        
        // For tracking multiple concurrent SetNumberValuesAsync operations
        private readonly Dictionary<string, TaskCompletionSource<bool>> _pendingAsyncOperations = new();
        private readonly object _asyncOperationsLock = new();

        public Task<bool> Connect(CancellationToken ct) {
            return Task.Run(async () => {
                if (Connected) {
                    Logger.Warning($"Device '{DeviceName}' is already connected");
                    return true;
                }

                Logger.Info($"Connecting to INDI device: {DeviceName}");

                // Call hook to configure connection properties before connecting
                if(!await OnPreConnect())
                {
                    Logger.Error($"OnPreConnect failed for {DeviceName}");
                    return false;
                }

                // Send the CONNECT command using async mechanism
                bool success = await SetSwitchValueAsync("CONNECTION", "CONNECT", true, TimeSpan.FromSeconds(30));

                if (success)
                {
                    Logger.Info($"Connected to INDI device: {DeviceName}");
                    _connectionAttemptFailed = false;  // Clear failure flag on successful connection

                    // Wait for initial property definitions to arrive from the driver
                    var requiredProps = GetRequiredConnectionProperties();
                    if (requiredProps != null && requiredProps.Length > 0)
                    {
                        Logger.Debug($"Waiting for required properties: {string.Join(", ", requiredProps)}");

                        // Poll for properties with timeout
                        var propStopwatch = System.Diagnostics.Stopwatch.StartNew();
                        while (!HasProperties(requiredProps) && propStopwatch.Elapsed < TimeSpan.FromSeconds(20) && !ct.IsCancellationRequested)
                        {
                            await CoreUtil.Wait(TimeSpan.FromMilliseconds(200), ct);
                        }
                    }
                }
                else
                {
                    Logger.Error($"Connecting to {DeviceName} failed");
                }

                Logger.Info($"[{DeviceName}] Setting Connected to {success}");
                Connected = success;

                return success;
            });
        }

        public async Task<bool> DisconnectAsync() {
            Logger.Info($"Disconnecting from INDI device: {DeviceName}");

            // Check if INDI client is still connected to server
            if (!INDIClient.Instance.IsConnected) {
                Logger.Info($"INDI server already disconnected, skipping graceful disconnect for {DeviceName}");
                _connected = false;
                return true;
            }

            // If the last connection attempt failed, skip DISCONNECT (device never actually connected)
            if (_connectionAttemptFailed) {
                Logger.Info($"Device '{DeviceName}' connection failed previously, skipping graceful disconnect");
                _connected = false;
                _connectionAttemptFailed = false;  // Reset flag
                return true;
            }

            // Check if device is actually connected before trying to disconnect
            var connProp = GetSwitchProperty("CONNECTION");
            if (connProp != null) {
                var connectSwitch = connProp.Switches.FirstOrDefault(s => s.Name == "CONNECT");
                if (connectSwitch != null && !connectSwitch.Value) {
                    Logger.Info($"Device '{DeviceName}' is not connected to server (CONNECT=false), skipping graceful disconnect");
                    _connected = false;
                    return true;
                }
            }

            // Send DISCONNECT command using async mechanism
            bool success = await SetSwitchValueAsync("CONNECTION", "DISCONNECT", true, TimeSpan.FromSeconds(60));
            
            if (success) {
                Logger.Info($"Disconnected from INDI device: {DeviceName}");
            } else {
                Logger.Warning($"Disconnecting from {DeviceName} timed out or failed");
            }

            _connected = false;
            return success;
        }

        public void Disconnect() {
            // Use async variant as fire-and-forget for Dispose compatibility
            _ = Task.Run(async () => {
                try {
                    await DisconnectAsync();
                } catch (Exception ex) {
                    Logger.Error($"Disconnect failed: {ex.Message}");
                }
            });
        }

        public void Dispose() {
            if (Connected) {
                Disconnect();
            }

            // Unregister device from client
            INDIClient.Instance.UnregisterDevice(this);
        }

        /// <summary>
        /// Override this to specify which properties must be received before Connect() completes.
        /// Return null/empty to skip waiting (uses fixed delay fallback).
        /// </summary>
        protected virtual string[] GetRequiredConnectionProperties() {
            return null;
        }

        /// <summary>
        /// Override this to configure device properties after driver load but before CONNECT
        /// </summary>
        protected virtual async Task<bool> OnPreConnect()
        {
            // Check if we have a valid connection mode setting
            bool HasConnectionMode = _hasConnectionModeProperty && !string.IsNullOrEmpty(_connectionMode);
            bool HasAddress = !string.IsNullOrEmpty(_address);
            bool HasPort = !string.IsNullOrEmpty(_port);
            bool IsAutoMode = _autoSearch && _hasAutoSearchProperty;

            // Determine actual connection mode
            bool IsUsingSerialMode = HasConnectionMode && !_connectionMode.Equals("CONNECTION_TCP");

            Logger.Info($"[{DeviceName}] Connection config: HasConnectionMode={HasConnectionMode}, HasAddress={HasAddress}, HasPort={HasPort}, IsAutoMode={IsAutoMode}, IsUsingSerialMode={IsUsingSerialMode}");

            // If no connection configuration is available or configured, skip pre-connect (e.g., direct USB devices)
            if (!HasConnectionMode && !HasAddress && !HasPort && !IsAutoMode) {
                Logger.Info($"[{DeviceName}] No connection configuration needed - device will connect directly");
                return true;
            }
            
            // Validate configuration if we're trying to configure something
            if (HasConnectionMode || HasPort || HasAddress || IsAutoMode) {
                if (!IsAutoMode && !HasPort && !HasConnectionMode) {
                    Logger.Error($"[{DeviceName}] OnPreConnect validation failed: No auto-search and no port specified");
                    return false;
                }
                
                if(!IsUsingSerialMode && HasConnectionMode && (!HasAddress || !HasPort)) {
                    Logger.Error($"[{DeviceName}] OnPreConnect validation failed: TCP mode requires both address ({HasAddress}) and port ({HasPort})");
                    return false;
                }
            }

            // Apply connection mode
            if (HasConnectionMode)
            {
                Logger.Info($"[{DeviceName}] Setting CONNECTION_MODE to {_connectionMode}");
                if(!await SetSwitchValueAsync("CONNECTION_MODE", _connectionMode, true, TimeSpan.FromSeconds(10)))
                {
                    Logger.Error($"[{DeviceName}] Failed to set CONNECTION_MODE to {_connectionMode}");
                    return false;
                }
                Logger.Info($"[{DeviceName}] CONNECTION_MODE successfully set to {_connectionMode}");
            }

            if (IsUsingSerialMode)
            {
                Logger.Info($"[{DeviceName}] Configuring SERIAL mode connection");
                // Process SERIAL mode
                if (IsAutoMode)
                {
                    // Just set auto mode and return
                    Logger.Info($"[{DeviceName}] Enabling DEVICE_AUTO_SEARCH");
                    bool autoSearchResult = await SetSwitchValueAsync("DEVICE_AUTO_SEARCH", "INDI_ENABLED", true, TimeSpan.FromSeconds(10));
                    if (autoSearchResult)
                    {
                        Logger.Info($"[{DeviceName}] DEVICE_AUTO_SEARCH enabled successfully");
                    }
                    else
                    {
                        Logger.Error($"[{DeviceName}] Failed to enable DEVICE_AUTO_SEARCH");
                    }
                    return autoSearchResult;
                }

                // Disable auto mode
                await SetSwitchValueAsync("DEVICE_AUTO_SEARCH", "INDI_DISABLED", true, TimeSpan.FromSeconds(10));

                if (_hasDevicePortProperty)
                {
                    Logger.Info($"[{DeviceName}] Setting DEVICE_PORT to {_port}");
                    if(!await SetTextValueAsync("DEVICE_PORT", "PORT", _port, TimeSpan.FromSeconds(10)))
                    {
                        Logger.Error($"[{DeviceName}] Failed to set DEVICE_PORT to {_port}");
                        return false;
                    }
                    Logger.Info($"[{DeviceName}] DEVICE_PORT set to {_port}");
                }
                if (_hasDeviceBaudRateProperty)
                {
                    Logger.Info($"[{DeviceName}] Setting DEVICE_BAUD_RATE to {_baudRate}");
                    if(!await SetSwitchValueAsync("DEVICE_BAUD_RATE", $"{_baudRate}", true, TimeSpan.FromSeconds(10)))
                    {
                        Logger.Error($"[{DeviceName}] Failed to set DEVICE_BAUD_RATE to {_baudRate}");
                        return false;
                    }
                    Logger.Info($"[{DeviceName}] DEVICE_BAUD_RATE set to {_baudRate}");
                }
                Logger.Info($"[{DeviceName}] Serial mode configuration complete - DEVICE_PORT={_port}, BAUD_RATE={_baudRate}");
            }
            else
            {
                Logger.Info($"[{DeviceName}] Configuring TCP mode connection");
                // Process TCP mode
                if (HasAddress)
                {
                    try
                    {
                        Logger.Info($"[{DeviceName}] Setting DEVICE_ADDRESS to {_address}:{_port}");
                        if(!await SetTextValueAsync("DEVICE_ADDRESS", "ADDRESS", _address, TimeSpan.FromSeconds(10)))
                        {
                            Logger.Error($"[{DeviceName}] Failed to set DEVICE_ADDRESS address to {_address}");
                            return false;
                        }
                        if(!await SetTextValueAsync("DEVICE_ADDRESS", "PORT", _port, TimeSpan.FromSeconds(10)))
                        {
                            Logger.Error($"[{DeviceName}] Failed to set DEVICE_ADDRESS port to {_port}");
                            return false;
                        }
                        Logger.Info($"[{DeviceName}] TCP mode configuration complete - ADDRESS={_address}:{_port}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[{DeviceName}] Exception during TCP configuration: {ex.Message}");
                        return false;
                    }
                }
            }

            Logger.Info($"[{DeviceName}] OnPreConnect completed successfully");
            return true;
        }

        /// <summary>
        /// Check if all required properties have been received
        /// </summary>
        private bool HasProperties(string[] props) {
            if (props == null || props.Length == 0) {
                return true;
            }

            lock (_properties) {
                foreach (var propName in props) {
                    if (!_properties.ContainsKey(propName)) {
                        return false;
                    }
                }
            }
            return true;
        }

        public virtual void OnSwitchPropertyUpdated(INDISwitchProperty p) {
            /*
            Logger.Info($"{p.Name}, {p.Label}, {p.State}, {p.Rule}");
            foreach (var s in p.Switches)
            {
                Logger.Info($"        {s.Name}, {s.Label}, {s.Value}");
            }
            */
            // Track CONNECTION failures to prevent spurious DISCONNECT attempts
            if (p.Name == "CONNECTION" && p.State == PropertyState.Alert) {
                _connectionAttemptFailed = true;
                Logger.Warning($"Device '{DeviceName}' connection attempt failed (Alert state)");
            }

            // Check if there are any pending async operations for this property
            lock (_asyncOperationsLock)
            {
                // Find all pending operations for this property
                var operationsForProperty = _pendingAsyncOperations
                    .Where(kvp => kvp.Key.StartsWith(p.Name + "_"))
                    .ToList();

                foreach (var kvp in operationsForProperty)
                {
                    var operationId = kvp.Key;
                    var tcs = kvp.Value;

                    // Resolve based on property state:
                    // - Busy: server has acknowledged the command and is processing it
                    // - Ok: operation completed successfully (some drivers skip Busy and go straight to Ok)
                    // - Alert: server rejected the command
                    // - Idle: operation still pending, keep waiting
                    if (p.State == PropertyState.Busy)
                    {
                        if (!tcs.Task.IsCompleted)
                        {
                            Logger.Debug($"Async operation {operationId} acknowledged by server (state: Busy)");
                            tcs.TrySetResult(true);
                        }
                    }
                    else if (p.State == PropertyState.Ok)
                    {
                        if (!tcs.Task.IsCompleted)
                        {
                            Logger.Debug($"Async operation {operationId} completed by server (state: Ok)");
                            tcs.TrySetResult(true);
                        }
                    }
                    else if (p.State == PropertyState.Alert)
                    {
                        if (!tcs.Task.IsCompleted)
                        {
                            Logger.Warning($"Async operation {operationId} rejected by server (state: Alert)");
                            tcs.TrySetResult(false);
                        }
                    }
                }
            }
        }

        public virtual void OnNumberPropertyUpdated(INDINumberProperty p) {
            /*
            Logger.Info($"{p.Name}, {p.Label}, {p.State}");
            foreach (var n in p.Numbers) {
                Logger.Info($"        {n.Name}, {n.Label}, {n.Value}, {n.Min}, {n.Max}, {n.Step}, {n.Format}");
            }
            */

            // Check if there are any pending async operations for this property
            lock (_asyncOperationsLock)
            {
                // Find all pending operations for this property
                var operationsForProperty = _pendingAsyncOperations
                    .Where(kvp => kvp.Key.StartsWith(p.Name + "_"))
                    .ToList();

                foreach (var kvp in operationsForProperty)
                {
                    var operationId = kvp.Key;
                    var tcs = kvp.Value;

                    // Resolve based on property state:
                    // - Busy: server has acknowledged the command and is processing it
                    // - Ok: operation completed successfully (some drivers skip Busy and go straight to Ok)
                    // - Alert: server rejected the command
                    // - Idle: operation still pending, keep waiting
                    if (p.State == PropertyState.Busy)
                    {
                        if (!tcs.Task.IsCompleted)
                        {
                            Logger.Debug($"Async operation {operationId} acknowledged by server (state: Busy)");
                            tcs.TrySetResult(true);
                        }
                    }
                    else if (p.State == PropertyState.Ok)
                    {
                        if (!tcs.Task.IsCompleted)
                        {
                            Logger.Debug($"Async operation {operationId} completed by server (state: Ok)");
                            tcs.TrySetResult(true);
                        }
                    }
                    else if (p.State == PropertyState.Alert)
                    {
                        if (!tcs.Task.IsCompleted)
                        {
                            Logger.Warning($"Async operation {operationId} rejected by server (state: Alert)");
                            tcs.TrySetResult(false);
                        }
                    }
                }
            }
        }

        public virtual void OnTextPropertyUpdated(INDITextProperty p) {
            /*
            Logger.Info($"{p.Name}, {p.Label}, {p.State}");
            foreach (var t in p.Texts) {
                Logger.Info($"        {t.Name}, {t.Label}, {t.Value}");
            }
            */

            // Check if there are any pending async operations for this property
            lock (_asyncOperationsLock)
            {
                // Find all pending operations for this property
                var operationsForProperty = _pendingAsyncOperations
                    .Where(kvp => kvp.Key.StartsWith(p.Name + "_"))
                    .ToList();

                foreach (var kvp in operationsForProperty)
                {
                    var operationId = kvp.Key;
                    var tcs = kvp.Value;

                    // Resolve based on property state:
                    // - Busy: server has acknowledged the command and is processing it
                    // - Ok: operation completed successfully (some drivers skip Busy and go straight to Ok)
                    // - Alert: server rejected the command
                    // - Idle: operation still pending, keep waiting
                    if (p.State == PropertyState.Busy)
                    {
                        if (!tcs.Task.IsCompleted)
                        {
                            Logger.Debug($"Async operation {operationId} acknowledged by server (state: Busy)");
                            tcs.TrySetResult(true);
                        }
                    }
                    else if (p.State == PropertyState.Ok)
                    {
                        if (!tcs.Task.IsCompleted)
                        {
                            Logger.Debug($"Async operation {operationId} completed by server (state: Ok)");
                            tcs.TrySetResult(true);
                        }
                    }
                    else if (p.State == PropertyState.Alert)
                    {
                        if (!tcs.Task.IsCompleted)
                        {
                            Logger.Warning($"Async operation {operationId} rejected by server (state: Alert)");
                            tcs.TrySetResult(false);
                        }
                    }
                }
            }
        }

        public virtual void OnBlobPropertyUpdated(INDIBlobProperty p) {
        }

        private string _connectionMode;
        private bool _autoSearch;
        private string _address;
        private string _port;
        private int _baudRate;

        public void ConfigureConnectionProperties(string connectionMode, bool autoSearch, string address, string port, int baudRate) {
            _connectionMode = connectionMode;
            _autoSearch = autoSearch;
            _address = address;
            _port = port;
            _baudRate = baudRate;
        }

        #region Unsupported
        public virtual IList<string> SupportedActions => new List<string>();

        public virtual string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public virtual void CommandBlind(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        public virtual bool CommandBool(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        public virtual string CommandString(string command, bool raw = false) {
            throw new NotImplementedException();
        }
        #endregion
    }
}
