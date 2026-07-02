# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## 1.1.45 - 2026-07-02
### Changed
- Updated NINA to 3.3.0.1048-nightly

## 1.1.44 - 2026-07-01
### Fixed
- INDI: Explicitly enable tracking after slew on AltAz mounts
- INDI: Fix first slew after connect being rejected as "below the horizon limit" (e.g. TPPA right after connecting) by awaiting the tracking-enable transition before the goto
- Fixes Cfitsio with byte array

## 1.1.43 - 2026-06-29
### Fixed
- Fixed Auto Restore Calibration option in phd2 guiding
- Fixed issue where max slew rate required mount reconnect

## 1.1.42 - 2026-06-24
### Added
- SequenceItem to slew to phd2 calibration position

## 1.1.41 - 2026-06-22
### Fixed
- INDI: Fix for direct USB connection

## 1.1.40 - 2026-06-17
### Added
- INDI: Control panel
### Fixed
- INDI: Supporting CONNECTION_HTTP on mounts
- INDI: Homing on some mounts

## 1.1.39 - 2026-06-15
### Fixed
- INDI: Geo sync issue with AM3/5/7 mounts

## 1.1.38 - 2026-06-11
### Added
- INDI Camera: Native INDI camera support, including a per-profile INDI camera driver setting and camera enumeration in the camera chooser
- INDI Camera: Broader driver compatibility by probing alternate property/element names for cooler power, offset, USB bandwidth, and dew heater control
- INDI Camera: Camera mode switches (low-noise/ultra, high-fullwell, tail-light LED) are applied from the profile on connect, deferring until the corresponding property arrives if it is not yet present
### Fixed
- INDI Mount: Send UTC time and UTC offset together as one atomic update; previously only the time was written, leaving a stale offset after power-on so the mount computed the wrong sidereal time and slews could fail
- INDI Mount: Alt/Az slew capability now reflects whether the mount actually accepts alt/az gotos; mounts that only publish alt/az read-only fall back to an equatorial slew instead of a silently-ignored command
- INDI: Defer device unregistration until a graceful disconnect is acknowledged, and recognize the idle-state disconnect acknowledgement, to avoid spurious disconnect timeouts
- INDI: A device-level property deletion (hot-unplug or driver shutdown) now removes the entire device so rescans no longer list stale devices
- INDI: Honor runtime element range changes (min/max/step) on number property updates
- INDI: The bounded server-ready wait is now performed inside device enumeration so a missing or broken INDI server cannot hang the device scan

## 1.1.37 - 2026-06-10
### Fixed
- LibRaw: Reverted from hardcoded struct offsets to libraw C API (libraw_raw2image, libraw_get_iwidth, libraw_get_iheight) for improved compatibility with system-installed libraw versions
- LibRaw: Fixed pixel layout handling for correct CFA sample extraction from multi-channel output
- libgphoto2: Wrapped constructor initialization in try-catch with proper cleanup; failures now throw instead of silently returning half-initialized cameras
- libgphoto2: Fixed unreliable shutterspeed property write for bulb mode; now refuses captures > 30s in Manual mode instead of truncating
- libgphoto2: Added shutter release guard to prevent multiple releases per bulb exposure
- libgphoto2: Added error checks for abilities list load/creation; gracefully skips device-type filtering if unavailable
- libgphoto2: Disabled CanShowLiveView (feature not implemented); property checks were unreliable
- libgphoto2: Use plain status checks instead of logging errors for optional properties like shutterspeed
- libgphoto2: Skip battery polling during active exposures to avoid disturbing captures
- libgphoto2: Fixed gp_port_info_list_lookup_path P/Invoke marshaling to use [MarshalAs(UnmanagedType.LPStr)] string
- libgphoto2: Catch per-camera initialization failures individually instead of aborting all enumeration
- Camera: AbortExposure now updates exposure state before broadcasting to ensure consumers see correct state

## 1.1.36 - 2026-06-09
### Fixed
- Fixed non-motorized INDI Flat panels

## 1.1.35 - 2026-06-08
### Fixed
- Retry mechanism for failing Nitecrawler serial comm
- Share invalid chars across platforms
- NULL char in DSLR image string

## 1.1.34 - 2026-06-02
### Added
- phd2 profile properties

## 1.1.33 - 2026-06-01
### Changed
- Updated NINA to 3.3.0.1046-nightly

## 1.1.32 - 2026-05-21
### Changed
- Updated NINA to 3.3.0.1043-nightly
- Logger colors (ERROR red, WARNING orange)

## 1.1.31 - 2026-05-20
### Changed
- Updated NINA to 3.3.0.1042-nightly
### Added
- GET/SET guide rate, if supported

## 1.1.30 - 2026-05-19
### Added
- Added Manual flat device, in order to properly control dark flat generation

## 1.1.29 - 2026-05-19
### Fixed
- AddedFixed issue with dark flats when no flat panel was connected

## 1.1.28 - 2026-05-18
### Changed
- Updated NINA to 3.3.0.1040-nightly
### Fixed
- Fixed OnStep mounts running into timeout when homing

## 1.1.27 - 2026-05-16
### Fixed
- INDI Focuser no longer show wrong step sizes
- Fixed an issue, where duplicating sequencer items reset the iteration count

## 1.1.26 - 2026-05-15
### Changed
- Updated NINA to 3.3.0.1039-nightly

## 1.1.25 - 2026-05-12
### Added
- Support for HocusFocus 3.0.0.26 plugin
- Support for NightSummary v3.0.0 plugin
- Support for INDI Switch pre/post-connection delay (to fix SV241 Pro connection and mirror [Ekos rule required](Yup, maybe say "mirror the Ekos rule required on the svbony website https://www.svbony.com/blog/review-of-the-new-sv241pro-power-controller-from-svbony"))

## 1.1.24 - 2026-05-11
### Fixed
- Fixes gphoto2 related issues with Nikon
- Fixes issue in GPCamera class where disconnect was not called on connect failure

## 1.1.23 - 2026-05-07
### Fixed
- Fixes a buffer overflow with QHY Filterwheels
- Fixes a race condition in QHY SDK

## 1.1.21 - 2026-05-05
### Added
- Profile entry for the slot number for ToupTekAlike Filterwheels

## 1.1.20 - 2026-05-03
### Changed
- Updated NINA to 3.3.0.1036-nightly
### Fixed
- Delayed GC

## 1.1.19 - 2026-04-29
### Changed
- Updated NINA to 3.3.0.1034-nightly
### Fixed
- SVBonySDK type mismatch
- Filestreams flushes to disk now so the write truly completes before returning

## 1.1.16 - 2026-04-28
### Changed
- Throw error on null char in file save path

## 1.1.15 - 2026-04-27
### Changed
- Updated NINA to 3.3.0.1033-nightly

## 1.1.14 - 2026-04-24
### Changed
- Updated NINA to 3.3.0.1030-nightly

## 1.1.13 - 2026-04-23
### Changed
- Updated NINA to 3.3.0.1026-nightly

## 1.1.12 - 2026-04-22
### Changed
- Updated NINA to 3.3.0.1025-nightly

## 1.1.11 - 2026-04-21
### Added
- Support INDI safety monitors and INDI domes (experimental)

## 1.1.10 - 2026-04-20
### Changed
- Updated NINA to 3.3.0.1024-nightly

## 1.1.9 - 2026-04-17
### Changed
- Some more stubs (colors etc)
- Updated NINA

## 1.1.8 - 2026-04-13
### Changed
- Some stubs to support LiveStack 1.1.0.0

## 1.1.7 - 2026-04-09
### Changed
- Updated to NINA 3.3.0.1023-nightly
### Fixed
- Fixed Atik EFW native driver implementation

## 1.1.5 - 2026-04-08
### Fixed
- Prevent premature collection on CopyPixels compatibility function

## 1.1.4 - 2026-04-07
### Fixed
- SWCREATE tag adjusted
- Disabled QHY query to System.Management functionality

## 1.1.3 - 2026-04-05
### Fixed
- Fixed race condition in Touptek SDK wrapper
- Fixed unhandled exception in status update bar

## 1.1.1 - 2026-04-03
### Added
- Support for Atik devices (experimental)

## 1.1.0 - 2026-04-01
### Added
- Support for INDI powerboxes (switches)
- Support for multi-filter flatwizzard

## 1.0.7 - 2026-03-31
### Fixed
- More issues with shared driver fixed.

## 1.0.6 - 2026-03-30
### Fixed
- Fixed INDI shared driver
- Fixed issues with compatibility layer

## 1.0.5 - 2026-03-27
### Fixed
- Fixed DSLR issues with the Image History tab
- Fixed DSLR issues with longer exposures when in BULB mode

## 1.0.4 - 2026-03-26
### Added
- Public methods for filterwheel calibration added

## 1.0.3 - 2026-03-25
### Fixed
- Message boxes and progress state will now also show current state on TNS reconnect

## 1.0.2 - 2026-03-24
### Fixed
- MessageBoxItem was not sent to TNS
- Dialogs were sometimes closed too early

## 1.0.1 - 2026-03-24
### Changed
- Updated to NINA 3.3.0.1021-nightly

## 1.0.0 - 2026-03-23
### Fixed
- Fixed an issue where HocusFocus Autorun could cause segmentation fault
