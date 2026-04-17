# PINS First Time Setup Handbook

This handbook covers first-time setup, system updates, and Touch N Stars configuration for PINS.

## 1. Prerequisites

- Download the PINS image from the official source.
- Flash to SD card or internal eMMC.
- Minimum storage: 32 GB.
- Recommended storage: 128 GB.
- Use any flashing tool you prefer.

## 2. First Boot

1. Insert flashed media and power on the Raspberry Pi.
2. Connect LAN or ensure internet access is available.
3. Wait for first boot to complete.
4. The Pi creates a hotspot named pins_xxxxx.

Default hotspot password:
- touchnstars

Hotspot network:
- 10.42.0.1/24

The interface is reachable by web and by Touch N Stars.

## 3. System Update Procedure

You can update the system in two ways.

### Option A: Update via SSH

1. Connect to the Pi via SSH.
2. Login:
	- Username: pi
	- Password: pins
3. Run:

```bash
sudo apt update && sudo apt upgrade
```

4. Confirm package installation when prompted.

### Option B: Update via Touch N Stars

1. Open Touch N Stars.
2. Go to Settings -> Plugin Management.
3. Enable PINS.
4. Open the PINS plugin and trigger system update.

## 4. First-Time App Requirement (Android and iOS)

For first-time users of the Touch N Stars mobile app:

1. Open app settings.
2. Enable Beta.
3. Ensure internet connectivity is available so the app can fetch the latest update.

If Beta is not enabled, new PINS-related features may not appear.

## 5. PINS Plugin Features

In the PINS plugin you can:

- Update the system.
- Install INDI 3rd-party drivers.
- Change hotspot password.
- Switch the Pi to Stationary Mode (connect Pi to local Wi-Fi).

## 6. Configure Touch N Stars

Reference project:
- https://github.com/Touch-N-Stars/Touch-N-Stars

### 6.1 Equipment Page

1. Open Equipment Page.
2. Go to INDI Setup.
3. Choose existing INDI drivers for your equipment.
4. Select equipment entries and connect.

### 6.2 Settings -> General

Configure:

- GPS location.
- Location sync direction.
- Time synchronization.
- Connection settings (discover NINA instances).
- Language selection.

### 6.3 Settings -> Image

Configure image behavior:

- Image quality.
- Maximum image size.
- Stretch factor.
- Debayer options.

Configure storage:

- Image save path (supports external devices).
- File name pattern.
- File type.

### 6.4 Settings -> Equipment

Configure:

- Camera chip settings.
- Telescope settings.
- Mount settings.

### 6.5 Settings -> Plugin Management

Enable or disable plugins to extend functionality.

### 6.6 Settings -> Plate Solver

Configure basic plate solving parameters:

- Exposure time.
- Gain.
- Search settings.
- Retry settings.

### 6.7 Settings -> Meridian Flip

Configure meridian flip behavior:

- Minutes after meridian.
- Settle time.
- Recenter behavior.
- Side of pier handling.
- Auto focus after flip.
- Rotate image after flip.

## 7. Component Guide: Camera

Use this section to configure and operate the camera component in Touch N Stars.

### 7.1 Capture Controls

- Record button: Start a single image.
- Loop button: Start continuous image capture.
- Exposure time (s): Set exposure duration.

### 7.2 Camera Settings

Open Settings -> Camera and configure:

- Exposure time (s).
- Gain / ISO.
- Binning.
- Readout Mode Sequence.
- Readout Mode Snapshot.

### 7.3 Cooler Settings

Configure camera thermal behavior:

- Camera cool down: Enable or disable.
- Target temperature (C).
- Cooling time (min).
- Camera warm up: Enable or disable.
- Warm-up time (min).

### 7.4 Filter

- Select the filter to use for the current capture.

### 7.5 Save and Solve

Configure save and plate solve workflow:

- Use platesolve: Enable or disable.
- Sync to mount: Enable or disable.
- Save snapshots: Enable or disable.
- Target name: Enter name used for saved image naming.

### 7.6 Manual Control Icons

- Mount control icon: Manually control the mount.
- Focuser icon: Manually adjust focus.
- Filter icon: Manually change filter.
- Rotator (if connected): Rotate the camera.

## 8. Component Guide: Autofocuser

Use this section to monitor focus status, adjust focus manually, and run autofocus.

### 8.1 General Overview

Check these live values in the Autofocuser panel:

- Current focuser position.
- Temperature (C).
- Focuser state (moving or stopped).
- Autofocus state (running or idle).
- Step size (um / micrometer).

### 8.2 Manual Focus Adjustment

Use manual focus controls to move inward or outward in small or large step increments.

- Use small step moves for fine tuning.
- Use larger step moves to quickly reach rough focus.
- Verify star size or HFR trend after each adjustment.

### 8.3 Run Autofocus

Start autofocus from the Autofocuser page.

- The autofocus graph shows each measurement point.
- The fitted focus curve helps identify best focus position.
- Last focus curve is available for comparison with current run.

### 8.4 Settings

Open Autofocuser -> Settings.

General Settings section is available.

Autofocus Settings:

- Use filterwheel offsets.
- Autofocus filter.
- Default Exposure Time.
- Initial Offset Steps.
- Step Size.
- Focuser Settle Time.
- Total Number of Attempts.
- Number of Frames Per Point.

Crop and Stars:

- Inner Crop Ratio.
- Outer Crop Ratio.
- Use Brightest Stars.

Backlash:

- Backlash In.
- Backlash Out.
- Backlash compensation mode.

Advanced:

- Autofocus Binning.
- Autofocus Timeout (seconds).
- R-Squared Threshold.
- Curve fitting strategy.
- Disable Guiding During Autofocus.

Device Settings:

- Set Position to Zero.

## 9. Component Guide: Mount

Use this section to monitor mount state, manually control movement, slew to targets, run TPPA, and configure meridian flip behavior.

### 9.1 Mount Tab

Top status panel shows:

- Park state (for example: Unparked).
- Tracking state (for example: Tracking is active).
- Slew state (for example: Not slewing).
- RA.
- Dec.
- Alt.
- Az.
- Time to Meridian Flip.
- Side of pier.

Main controls:

- Park.
- Unpark.
- Stop/Abort current mount action.

Tracking mode:

- Sidereal.
- Lunar.
- Solar.
- King.
- Stop tracking.

Axis direction:

- Primary reversed toggle.
- Secondary reverse toggle.

Manual control:

- Direction pad for north, south, east, and west movement.
- Center stop button to halt manual movement.

Slew rate:

- Presets: 4x, 16x, 32x, 62x.
- Numeric slew rate adjustment (deg/s).

### 9.2 Slew Tab

Search and target tools:

- Search field to find targets.
- Visible Star Search dropdown.
- Use NINA cache for target image toggle.

Slew and Center:

- Enter RA and Dec coordinates.
- Enter Alt and Az coordinates.
- Slew and Center action button.
- Settings button for slew/center options.
- set to sequence target action.

### 9.3 TPPA Tab

Three Point Polar Alignment panel includes:

- Settings button.
- Start Alignment.
- Stop Alignment.
- Altitude Error with correction direction.
- Azimuth Error with correction direction.
- Total Error indicator.

### 9.4 Meridian Flip Tab

Basic settings:

- Minutes after meridian.
- Max minutes after meridian.
- Pause time before meridian.

Behavior:

- Settle time (s).
- Recenter toggle.
- Use side of pier toggle.

Post-flip actions:

- Auto focus after flip toggle.
- Rotate image after flip toggle.

## 10. Component Guide: Dome

Use this section to monitor dome state and control shutter, parking, synchronization, and azimuth movement.

### 10.1 Dome Status Overview

Top status panel shows:

- Azimuth.
- Slewing state (for example: Slewing stopped).
- Following state (for example: Following stopped).
- Home state (for example: Not at home).
- Park state.
- Shutter state (for example: Shutter closed).
- Sync state (for example: Not sync).

### 10.2 Shutter and Dome State Controls

- Open shutter.
- Stop/Abort current shutter or dome action.
- Close shutter.
- Home.
- Park.
- Sync with telescope.

### 10.3 Follow and Slew Controls

- Dome follow telescope toggle.
- Slew to degrees numeric field with decrement/increment controls.
- Slew action button.
- Stop button for manual slew.

Recommended order for operation:

1. Ensure dome is unparked and at known state.
2. Sync with telescope.
3. Enable Dome follow telescope for automatic tracking.
4. Use manual Slew to degrees only when repositioning is needed.

## 11. Component Guide: Flats

Use this section to control the flat panel or dust cover device and configure trained flat exposure presets.

### 11.1 Flatpanel Tab

Top status panel shows:

- Device name.
- Cover State.
- Light State.
- Brightness.

Main controls:

- Light toggle.
- close cover.
- open cover.

Typical workflow:

1. Open cover when preparing light frames.
2. Close cover before flats when using a cover calibrator.
3. Enable Light and set Brightness as needed.
4. Disable Light when finished.

### 11.2 Settings Tab

Trained Flat Exposure Settings allow creating one or more profiles.

Each profile includes:

- Filter selection.
- Binning selection.
- Gain value (or default).
- Offset value (or default).
- Brightness.
- Time (s).

Profile management:

- Add profile with the plus button.
- Remove a profile with the delete icon.

Recommended usage:

1. Create one profile per filter and binning combination.
2. Keep gain/offset aligned with your imaging camera settings.
3. Store tested brightness and exposure time values for repeatable flats.

## 12. Component Guide: Switch

Use this section to monitor observatory safety/environment values and control power or variable-output switch channels.

### 12.1 Gauges

The Gauges panel provides live values for:

- Cover State.
- Parked.
- Cloud Cover (okta).
- Temperature (deg C).
- Humidity (%).
- Raining (rain monitor state).

Use these indicators before starting or continuing an imaging session.

### 12.2 Switch Controls

The Switch panel includes digital and variable controls.

Digital power switches:

- Power1 toggle (asynchronous switch).
- Power2 toggle (generic power switch).

Variable output channels:

- Light Box level control (0 to 100%).
- Flat Panel level control (0 to 255).

For variable channels, use minus/plus controls or numeric entry to set output value.

Recommended usage:

1. Verify weather and safety gauges first.
2. Enable only required power outputs.
3. Set Light Box or Flat Panel level gradually to avoid over-illumination.
4. Return outputs to safe default values when finishing the session.

## 13. Component Guide: Filter Wheel

Use this section to select active filters and configure per-position autofocus parameters.

### 13.1 Filter Wheel Tab

Top status panel shows:

- Current Filter.
- Available Filters (count).

Main control:

- Filter dropdown to select the active filter (for example: Red, Green).

When a filter is selected, the wheel rotates to that position.

### 13.2 Filter-Settings Tab

Use this tab to define filter wheel positions and autofocus behavior for each position.

Per-position settings:

- Position name.
- Focus Offset.
- AutoFocus Exposure Time.

Position management:

- Add a new position with the plus button.
- Remove a position with the delete icon.

Device Settings:

- Calibrate button.

Recommended setup:

1. Create one entry for each physical filter slot.
2. Name each position exactly as the installed filter.
3. Set focus offsets after measuring offsets between filters.
4. Set autofocus exposure time per filter based on throughput.
5. Run Calibrate after filter wheel changes or hardware maintenance.

## 14. Component Guide: Rotator

Use this section to monitor camera rotation state, move to a target angle, and configure mechanical range behavior.

### 14.1 Rotator Tab

Top status panel shows:

- Connection state (for example: Connected).
- Current Position (deg).
- Motion state (for example: Stationary).
- Step Size (deg).

Control panel:

- Rotator target angle numeric field (minus/plus and direct value entry).
- Move button.

### 14.2 Settings Tab

Basic Settings:

- Reverse toggle.
- Mechanical Range dropdown (for example: 360 deg).
- Mechanical Range Start value.

Recommended setup:

1. Confirm rotator reports Connected before issuing moves.
2. Set Mechanical Range and Mechanical Range Start to match hardware limits.
3. Use Reverse only if rotation direction is opposite to expected behavior.
4. Move in small angle steps first to verify orientation.

## 15. Component Guide: Sequence

Use this section to build and run sequences in the same style as the NINA Advanced Sequencer.

### 15.1 Sequence Layout

The sequence editor is organized into blocks:

- Global Trigger.
- Start.
- Targets.
- End.

Each block can contain one or more sequence items.

### 15.2 Building a Sequence

1. Add logic to Global Trigger for conditions that apply to the full sequence.
2. Add startup actions to Start.
3. Add one or more targets and target actions to Targets.
4. Add shutdown and cleanup actions to End.

This matches the overall workflow style of the NINA Advanced Sequencer.

### 15.3 Sequence Action Buttons

Bottom action buttons:

- Start sequence: Runs the current sequence.
- Reset sequence: Resets sequence run state.
- Clear all: Clears all sequence items.
- Load / save sequence: Opens sequence file actions.

Load / save behavior:

- Load existing sequence files.
- Save newly created or edited sequences.

Recommended usage:

1. Build or load the sequence first.
2. Validate items in Start, Targets, and End.
3. Save the sequence before running.
4. Start sequence.
5. Use Reset sequence before re-running after edits.

## 16. Component Guide: Flat Wizard

Use this section to automate flat and dark-flat acquisition in Single Mode or Multi Mode.

### 16.1 Common Controls

Available at the top of Flat Wizard:

- Speed/profile dropdown (for example: Fast).
- Slew to zenith button.

Use Slew to zenith before flat acquisition when your setup requires consistent sky position.

### 16.2 Single Mode

Single Mode is used to run one filter configuration at a time.

Mode and acquisition settings:

- Mode dropdown (for example: Auto exposure).
- Flats to take.
- Darks to take.
- Gain / ISO.
- Offset.
- Brightness.
- Min exposure time.
- Max exposure time.
- Histogram mean target.
- Mean tolerance.

Optical path settings:

- Filter selection.
- Binning selection.
- Keep flat panel closed toggle.

Run control:

- Start auto exposure button.

### 16.3 Multi Mode

Multi Mode is used to run multiple filters in one wizard execution.

Global settings:

- Mode dropdown.
- Keep flat panel closed toggle.
- Darks to take.

Filter list:

- Enable or disable each filter entry (for example: OIII, O3).

Run control:

- Start Multi Mode button.

Recommended workflow:

1. Set speed/profile and optionally Slew to zenith.
2. Choose Single Mode for one filter or Multi Mode for multiple filters.
3. Set darks count, gain/offset, and exposure constraints.
4. In Multi Mode, enable only the filters you want to process.
5. Start the wizard run and monitor results.

## 17. Optional Next Steps

- Enable Samba share to copy sequences from PC to Pi.
- Use VNC plugin to configure PHD2.