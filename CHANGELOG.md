# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
### Changed
### Deprecated
### Removed
### Fixed

## [0.9.1] - 2023-07-31
### Fixed
- Compiled script (FileInstall) fix for AHK v2

## [0.9.0] - 2023-03-21
### Added
- Added support for AHK v2

## [0.8.0] - 2022-02-20
### Added
- Add two SubscribeAbsolute example scripts which show how to process movement data coming from a tablet
### Changed
- Input is now processed even faster  
Since 0.6.0, there could have been a 10ms delay between processing one piece of input and the next  
Now it should be effectively instant
### Deprecated
### Removed
### Fixed
- Subscription / Context mode Extended keycodes fixed  
Previously, some keys were not properly subscribe-able (Pause / NumLock)  
Also, if any key was subscribed to with block enabled, and that key generated Extended Modifier keys when pressed  
(eg when you press Home with NumLock off, keyboards send LShift with a state of 2, then Home with a state of 2)  
then the Extended Modifier key (LShift with a state of 2 in the above example) was NOT blocked.
- SendKeyEvent() now sends exactly the same ScanCodes that would be sent if you really pressed it  
Similar to the above example, if you sent Home, previously, only Home would be sent with a state of 2  
LShift would not have been sent with a state of 2 as it should  
Also, Pause should send a state of 4, whereas before it sent a state of 2  
- AhiScanCodeTester.ahk in Development Tools now works again
- Bug introduced in 0.7.0 whereby unsubscribing whilst a callback is still running would lock up the script is now fixed  
(WorkerThreads now use Tasks and CancellationTokens)

## [0.7.0] - 2022-01-17
### Added
- Add RemoveContextManager() to remove a Context Manager
### Changed
- MAJOR changes to the code behind the scenes - the code is now organized way better, and key / button handling has been consolidated into the same code.  
Nothing should have changed apart from what is listed in this changelog
- If you SubscribeKey to subscribe to a specific key on a device, and use SubscribeKeyboard to subscribe to all keys on the same device 
Then SubscribeKey now takes precedence (SubscribeKey callback fires, and SubscribeKeyboard does not)
- AhiScanCodeTester.ahk in Development Tools is currently broken due to changes in AHI - it will be fixed in the next release
### Deprecated
### Removed
### Fixed
- If you had Context Mode enabled for a keyboard, and a SubscribeKey or SubscribeKeyboard subscription for the same keyboard 
then using UnsubscribeKey or UnsubscribeKeyboard would disable Context Mode
- CreateContextManager now correctly throws an error if one already exists
- When unsubscribing from a mouse movement which was not subscribed in Concurrent mode, the WorkerThread is now properly Disposed

## [0.6.0] - 2022-01-14
### Changed
- Input is now processed faster. Rather than checking for input every 1ms, we now use Interception's `WaitWithTimeout` function 
to process input as soon as it happens

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
