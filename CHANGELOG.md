# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
### Changed
### Deprecated
### Removed
### Fixed

## [0.4.1] - 2019-05-15
### Fixed
- Button state is no longer inverted as it was in 0.4.0

## [0.4.0] - 2019-05-14
### Added
- Concurrency switch for executing subscription callback functions. Was implicitly executing on a new thread from the pool, now there is an option to execute each callback on a single thread (one worker per subscription).
- UnsubscribeKey, UnsubscribeMouseButton, UnsubscribeMouseMove, UnsubscribeMouseMoveRelative, UnsubscribeMouseMoveAbsolute methods added to Subscription Mode
- "Unsubscription Example.ahk" to demo Subscribe / Unsubscribe
### Changed
- By default the new concurrency switch will be set to false meaning that for every subscription there will be only a single worker thread and callbacks will be run sequentially.
- Monitor now outputs data as would be seen in Subscription mode, rather than as it comes raw from Interception
- Monitor now shows key names
### Deprecated
### Removed
- Interception DLLs are no longer bundled with AHI
### Fixed
- SubscribeMouseMove endpoint fixed to not return bool (Fix "Can not implicitly convert type Void to object" error)
- Pause button now works
- UnsubscribeMouseButton now correctly checks if the device is a mouse

## [0.3.7] - 2019-02-10
### Added
- UnsubscribeKey, UnsubscribeMouseButton, UnsubscribeMouseMove, UnsubscribeMouseMoveRelative, UnsubscribeMouseMoveAbsolute methods added to Subscription Mode
- Added "Unsubscription Example.ahk" to demo Subscribe / Unsubscribe
### Changed 
- Fixed Build Event to copy Interception dll
### Fixed
- SubscribeMouseMove endpoint fixed to not return bool (Fix "Can not implicitly convert type Void to object" error)
