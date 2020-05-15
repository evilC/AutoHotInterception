# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
### Changed
### Deprecated
### Removed
### Fixed

## [0.5.3] - 2020-05-14
### Fixed
- Fix Issue 65: UnsubscribeMouseMoveRelative when subscribed with blocking enabled did not restore mouse movement

## [0.5.2] - 2020-04-29
- Compiled scripts now pack the required DLLs inside the EXE and unpack them on run
Both the x86 and the x64 DLL will always be packed in the EXE, but on run, only the required DLLs will be unpacked

## [0.5.1] - 2020-04-10
### Added
- AHI now supports scripts being compiled.
You will still need the DLLs from the Lib folder, but not the AHK scripts
### Fixed
- Fix issue 62 - x=0/y=0 never reported for Absolute mode mouse move

## [0.5.0] - 2019-12-15
### Added
- SubscribeKeyboard and SubscribeMouseButtons can now be used to subscribe to all keys / buttons on a device
- Added ScanCodeTester Development Tool to investigate differences in AHI / AHK ScanCodes
### Changed
- Monitor.ahk now uses SubscribeKeyboard and SubscribeMouseButtons

## [0.4.10] - 2019-10-13
### Changed
- Devices now polled at 1ms using Multimedia Timers (Was ~15ms)

## [0.4.9] - 2019-08-24
### Fixed
- The clipboard now correctly has & instead of && when using the Copy button for Handle in the Monitor

## [0.4.8] - 2019-08-23
### Added
- Monitor script now has Copy buttons to copy VID/PID or handle
### Fixed
- GetDeviceIdFromHandle now works again

## [0.4.6] - 2019-08-15
### Added
- Added MoveCursor method to move the cursor to a specific position
### Fixed
- NumLock is now bindable

## [0.4.5] - 2019-07-09
### Added
- Added TabletButtons demo for converting a graphics tablet into a button box
### Fixed
- Absolute Mode mouse movement subscriptions now work again  

## [0.4.4] - 2019-07-09
### Added
- Added SetState to allow toggling on/off of bindings

## [0.4.3] - 2019-06-10 **EXPERIMENTAL TEST RELEASE**
### Fixed
- Fixed issue #39
Almost complete rewrite of mouse polling code  
Multiple event types (Movement, mouse button events) supported per update ("stroke") of the mouse  
It is now possible to block a button or movement, but leave unblocked events unblocked  
Previously, a stroke was either blocked or not - if any one part of the stroke was blocked, it was all blocked  
### Changed
- [Monitor script] GUI layout made more robust
- [Monitor script] Add option to filter key presses and only show key releases

## [0.4.2] - 2019-06-08
### Fixed
- Fixed issue #37  
When multiple mouse buttons changed state in one update of the mouse, only one event would be fired for one of the buttons

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
