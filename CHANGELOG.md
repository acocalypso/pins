# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## 1.1.32 - 2026-05-21
### Changed
- Updated NINA to 3.3.0.1043-nightly

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
