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

            // Required driver properties
            string[] requiredProps = ["CONNECTION_MODE", "CONNECTION"];

            // Poll for required properties with timeout
            var propTimeout = DateTime.Now.AddSeconds(20);
            while (!HasRequiredProperties(requiredProps) && DateTime.Now < propTimeout)
            {
                CoreUtil.Wait(TimeSpan.FromMilliseconds(1000)).Wait();
            }
        }

        private bool _connected;
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

        private TaskCompletionSource<bool> _operationTcs;
        private string _pendingOperation; // Track which operation ("CONNECT" or "DISCONNECT") is pending
        private readonly object _operationLock = new();
        
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
                await OnPreConnect();

                // Initialize operation TCS for connection
                lock (_operationLock) {
                    _operationTcs = new TaskCompletionSource<bool>();
                    _pendingOperation = "CONNECT";
                }

                // Now send the actual CONNECT command using SetSwitchValue
                SetSwitchValue("CONNECTION", "CONNECT", true);

                try {
                    // Check token before we start
                    ct.ThrowIfCancellationRequested();

                    // Wait for the connection callback with timeout and cancellation support
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), ct);
                    var completedTask = await Task.WhenAny(_operationTcs.Task, timeoutTask);

                    if (completedTask == timeoutTask) {
                        // Check if it was a timeout or cancellation
                        if (ct.IsCancellationRequested) {
                            Logger.Warning($"Connecting to {DeviceName} was cancelled");
                        } else {
                            Logger.Error($"Connecting to {DeviceName} timed out");
                        }
                        return false;
                    }

                    bool success = await _operationTcs.Task;
                    if (success) {
                        Logger.Info($"Connected to INDI device: {DeviceName}");

                        // Wait for initial property definitions to arrive from the driver
                        var requiredProps = GetRequiredConnectionProperties();
                        if (requiredProps != null && requiredProps.Length > 0) {
                            Logger.Debug($"Waiting for required properties: {string.Join(", ", requiredProps)}");

                            // Poll for properties with timeout
                            var propTimeout = DateTime.Now.AddSeconds(20);
                            while (!HasRequiredProperties(requiredProps) && DateTime.Now < propTimeout && !ct.IsCancellationRequested) {
                                await CoreUtil.Wait(TimeSpan.FromMilliseconds(200), ct);
                            }
                        }
                    } else {
                        Logger.Error($"Connecting to {DeviceName} failed");
                    }

                    Connected = success;
                    return success;
                } catch (OperationCanceledException) {
                    Logger.Warning($"Connecting to {DeviceName} was cancelled");
                    return false;
                } catch (Exception ex) {
                    Logger.Error(ex.Message);
                    return false;
                }
            });
        }

        public void Disconnect() {
            if (!_connected) {
                Logger.Warning($"Device '{DeviceName}' is not connected");
                return;
            }

            Logger.Info($"Disconnecting from INDI device: {DeviceName}");

            // Check if INDI client is still connected to server
            if (!INDIClient.Instance.IsConnected)
            {
                Logger.Info($"INDI server already disconnected, skipping graceful disconnect for {DeviceName}");
                _connected = false;
                return;
            }

            // Initialize operation TCS for disconnect
            Logger.Info("Waiting on lock ...");
            lock (_operationLock)
            {
                _operationTcs = new TaskCompletionSource<bool>();
                _pendingOperation = "DISCONNECT";
            }

            Logger.Info("Setting connection switch to false");

            // Wait for disconnection synchronously to avoid race with Dispose()
            try
            {
                // Now send the actual DISCONNECT command using SetSwitchValue
                SetSwitchValue("CONNECTION", "DISCONNECT", true);

                // Wait for the disconnection callback with shorter timeout (server may be dead)
                var completedTask = Task.WhenAny(_operationTcs.Task, Task.Delay(TimeSpan.FromSeconds(60))).Result;

                if (completedTask == _operationTcs.Task)
                {
                    bool isConnected = _operationTcs.Task.Result;
                    if (!isConnected)
                    {
                        Logger.Info($"Disconnected from INDI device: {DeviceName}");
                    }
                    else
                    {
                        Logger.Warning($"Disconnect command completed but device reports still connected");
                    }
                }
                else
                {
                    Logger.Warning($"Disconnecting from {DeviceName} timed out (server may be disconnected)");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error during disconnect: {ex.Message}");
            }
            
            Logger.Info("Disconnected state reached");

            // Update the backing field directly to avoid recursion
            _connected = false;
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
        protected virtual async Task OnPreConnect() {
            // Initialize operation TCS for connection
            lock (_operationLock) {
                _operationTcs = new TaskCompletionSource<bool>();
            }

            // Set connection mode
            if (!string.IsNullOrEmpty(_connectionMode)) {
                try {
                    SetSwitchValue("CONNECTION_MODE", _connectionMode, true);
                    Logger.Info($"Set CONNECTION_MODE to {_connectionMode}");
                } catch (Exception ex) {
                    Logger.Info($"Could not set CONNECTION_MODE: {ex.Message}");
                }
            }

            try {
                // Wait for the connection mode switch to happen with timeout
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                var completedTask = await Task.WhenAny(_operationTcs.Task, timeoutTask);

                if (completedTask == timeoutTask) {
                    Logger.Error($"Setting CONNECTION_MODE to {DeviceName} timed out");
                    return;
                }
            } catch (Exception ex) {
                Logger.Error($"Setting CONNECTION_MODE failed: {ex.Message}");
                return;
            }

            // Set device address, if we are using network mode
            if (_connectionMode == "CONNECTION_TCP" && !string.IsNullOrEmpty(_address)) {
                try {
                    SetTextValue("DEVICE_ADDRESS", "ADDRESS", _address);
                    SetTextValue("DEVICE_ADDRESS", "PORT", _port);
                    Logger.Info($"Set DEVICE_ADDRESS to {_address}:{_port}");
                } catch (Exception ex) {
                    Logger.Info($"Could not set DEVICE_ADDRESS: {ex.Message}");
                }
            } else if (_connectionMode == "CONNECTION_SERIAL" && !string.IsNullOrEmpty(_port)) {
                try {
                    if (_autoSearch) {
                    } else {
                        SetTextValue("DEVICE_PORT", "PORT", _port);
                        SetSwitchValue("DEVICE_BAUD_RATE", "_baudRate", true);
                        Logger.Info($"Set DEVICE_PORT to {_port} ({_baudRate})");
                    }
                } catch (Exception ex) {
                    Logger.Info($"Could not set DEVICE_PORT: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Check if all required properties have been received
        /// </summary>
        private bool HasRequiredProperties(string[] requiredProps) {
            if (requiredProps == null || requiredProps.Length == 0) {
                return true;
            }

            lock (_properties) {
                foreach (var propName in requiredProps) {
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
            // Check for CONNECTION property updates (for device connection flow)
            if (p.Name == "CONNECTION") {
                // Check the actual CONNECT switch to determine connection state
                var connectSwitch = p.Switches.FirstOrDefault(s => s.Name == "CONNECT");
                var disconnectSwitch = p.Switches.FirstOrDefault(s => s.Name == "DISCONNECT");

                if (connectSwitch != null) {
                    bool isConnected = connectSwitch.Value;

                    // Complete the operation when:
                    // - State is Ok (successful connection/disconnection)
                    // - State is Alert (connection/disconnection failed)
                    // - State is Idle with the matching switch state (some drivers like LX200 OnStep report Idle after successful operation)
                    //   For CONNECT: when CONNECT=True (Idle state)
                    //   For DISCONNECT: when DISCONNECT=True and CONNECT=False (Idle state)
                    // - Don't complete for Busy (operation in progress)
                    bool shouldComplete = (p.State == PropertyState.Ok || p.State == PropertyState.Alert);

                    if (!shouldComplete && p.State == PropertyState.Idle && _pendingOperation != null) {
                        if (_pendingOperation == "CONNECT" && isConnected) {
                            shouldComplete = true;
                        } else if (_pendingOperation == "DISCONNECT" && !isConnected && disconnectSwitch?.Value == true) {
                            shouldComplete = true;
                        }
                    }

                    if (shouldComplete) {
                        lock (_operationLock) {
                            if (_operationTcs != null && !_operationTcs.Task.IsCompleted) {
                                Logger.Info($"Completing connection TCS with result: {isConnected} (state: {p.State}, pending: {_pendingOperation})");
                                _operationTcs.SetResult(isConnected);
                                _pendingOperation = null; // Clear pending operation
                            }
                        }
                    } else {
                        Logger.Debug($"CONNECTION property state is {p.State}, CONNECT={isConnected}, DISCONNECT={disconnectSwitch?.Value}, pending={_pendingOperation}, waiting for completion state");
                    }
                }
            }

            // Check for CONNECTION_MODE property updates
            if (p.Name == "CONNECTION_MODE") {
                lock (_operationLock) {
                    if (_operationTcs != null && !_operationTcs.Task.IsCompleted) {
                        _operationTcs.SetResult(true);
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
                    // - Alert: server rejected the command
                    // - Idle/Ok: ignore (operation completes in background, we don't wait for it)
                    if (p.State == PropertyState.Busy)
                    {
                        if (!tcs.Task.IsCompleted)
                        {
                            Logger.Debug($"Async operation {operationId} acknowledged by server (state: Busy)");
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
