![GitHub release](https://img.shields.io/github/release/evilc/autohotinterception.svg)
![Github All Releases](https://img.shields.io/github/downloads/evilc/autohotinterception/total.svg)
[![Discord](https://img.shields.io/discord/330423308103319562.svg)](https://discord.gg/9d3DNN4)
![Github commits (since latest release)](https://img.shields.io/github/commits-since/evilc/autohotinterception/latest.svg)


# AutoHotInterception

AutoHotInterception (AHI) allows you to execute AutoHotkey code in response to events from a *specific* keyboard or mouse, whilst (optionally) blocking the native functionality (i.e. stopping Windows from seeing that keyboard or mouse event).  
In other words, you can use a key on a second (or third, or fourth...) keyboard to trigger AHK code, and that key will not be seen by applications. You can use the *same key* on multiple keyboards for individual actions.  
Keyboard Keys, Mouse Buttons and Mouse movement (Both Relative and Absolute modes) are supported.

AHI uses the Interception driver by Francisco Lopez

# Getting Help  
## [AHI Discussion Thread on the AHK forums](https://autohotkey.com/boards/viewtopic.php?f=6&t=45307)  
## [Discord Channel](https://discord.gg/sFPMv86)

------

# WARNING
**TAKE CARE** when using this code. Because Interception is a driver, and sits below windows proper, blocking with Interception goes so deep that it can even block CTRL+ALT+DEL etc. As such, it is entirely possible to lock up all input, or at least make life a little difficult.  
In general, worst-case scenario would require use of the reset button.  
For example, using Subscription Mode with `block` enabled will **totally** block that key from working on that keyboard.
So if you block `Ctrl` on your only keyboard, you just blocked CTRL+ALT+DEL.  
The best insurance policy is to have another keyboard or mouse handy, one that you don't block.    
Be wary of making scripts using this code run on startup. Know how to enter "Safe Mode" in windows and disable startup of the scripts. Know mouse alternatives to emergency keyboard actions (Right click on clock for Task Manager!)    
As they say - ***With great power comes great responsibility***.  
If this all scares you and you don't really understand it, then TL/DR is you should probably stick to "Context Mode", it's safer.  

------

# Device IDs / VIDs PIDs etc

Interception identifies unique devices by an ID. This is a number from 1..20.  
Devices 1-10 are always keyboards  
Devices 11-20 are always mice  
This ID scheme is totally unique to Interception, and IDs may change as you plug / unplug devices etc.  
On PC, devices are often identified by VendorID (VID) and ProductID (PID). These are identifiers baked into the hardware at time of manufacture, and are identical for all devices of the same make / model.  
Most AHI functions (eg to Subscribe to a key etc) use an Interception ID, so some handy functions are provided to allow you to find the (current) Interception ID of your device, given a VID / PID.  
If you are unsure of what the VID / PID of your device is (or even if Interception can see it), you can use the included Monitor script to find it.  

You will need to know the VID / PID of at least one of your devices in order to do anything with AHI.  

## Monitor App
This handy tool allows you to check if AHI is working, and also to find the VID/PID or DeviceHandle of your devices.  
You can use the handy "Copy" buttons to copy the VID/PID or DeviceHandle of the device to the clipboard.  
When using the monitor app, **DO NOT** tick all devices at once, as if it crashes, it will lock up all devices.
Instead, tick one at a time and see if it your device.  
![](https://github.com/evilC/AutoHotInterception/blob/master/Monitor.png)  

------

# Known Issues
If you unplug / replug a device, or go into hibernate and resume, the Interception ID of a device will increase by 1.  
If the ID of a device goes above 10 (For keyboards) or 20 (For Mice), **The device will completely cease to function until the next reboot** - not only in AutoHotInterception, but in Windows also.  
There is nothing I can do to fix this issue, it is a limitation of the Interception driver

------

# Setup

1. Download and install the [Interception Driver](http://www.oblita.com/interception)  
Note that you **must** run `install-interception.exe` at an admin command prompt (**Not double-click it**) - once you do so, it will instruct you to execute `install-interception.exe /install` to actually perform the install.  
Here is a GIF showing the process:  
![](https://github.com/evilC/AutoHotInterception/blob/master/InterceptionInstall.gif)
2. Download an AHI release from the [releases page](https://github.com/evilC/AutoHotInterception/releases) and extract it to a folder.  
DO NOT use the "Clone or Download" link on the main page.  
This is the folder where (at least initially) you will be running scripts from.  
It contains a number of sample `.ahk` scripts and a `lib` folder, which contains all the AHI libraries.  
3. In the Interception installer zip, there is a `library` folder containing `x86` and `x64` folders.  
Copy both of these folders into the AHI `lib` folder that you created in step (3) - the folder structure should end up looking like:  
```
AHI Root Folder
	Monitor.ahk
	etc...
	Lib
		AutoHotInterception.ahk
		AutoHotInterception.dll
		CLR.ahk
		Unblocker.ps1
		etc..
		x86
			interception.dll
		x64
			interception.dll
```
4. Right-click `Unblocker.ps1` in the lib folder and select `Run as Admin`.  
This is because downloaded DLLs are often blocked and will not work.  
This can be done manually by right clicking the DLLs, selecting Properties, and checking a "Block" box if it exists.  
5. Edit one of the example remapping scripts, replacing the VID/PID(s) with that of your device (Use the Monitor app to find it) and run it to make sure it works.  
6. (Optional) The contents of the `lib` folder can actually be placed in one of the AutoHotkey lib folders (eg `My Documents\AutoHotkey\lib` - make it if it does not exist), and the `#include` lines of the sample scripts changed to `#include <AutoHotInterception>`, to enable your AHI scripts to be in any folder, without each needing it's own copy of the library files.  

------

# Usage
## Initializing the Library
Include the library
```
#Persistent ; (Interception hotkeys do not stop AHK from exiting, so use this)
#include Lib\AutoHotInterception.ahk
```

Initialize the library
```
global AHI := new AutoHotInterception()
```

*Note*  
The `AHI` variable is an AHK class that makes it easy to interact with the AutoHotInterception DLL (Which itself then interacts with the Interception dll). For example, it wraps `GetDeviceList()` to make it return a normal AHK array. Most of the time you will not need it.  
For advanced users, if you wish to directly communicate with the AHI DLL (eg for best possible performance), you can call `AHI.Instance` instead of `AHI` for most functions (eg when sending of synthesized input using `SendMouseMove`).  
```
AHI := new AutoHotInterception()
AHI.Instance.SendMouseMove(...)
```

## Misc Commands
### SetState
`SetState(true|false)`  
Turns on or off all subscriptions (Starts on)  
Where `true` is on, `false` is off.  
eg `AHI.SetState(false)`  

## Finding Device IDs  
### USB Devices
In most cases, you will want to hard-wire a script to a specific VID/PID - in this instance, use one of the following methods.    
For all these methods, if you have multiple identical VID/PID devices, you can specify an `instance` (Starts from 1).  

#### GetDeviceId
`AHI.GetDeviceId(<isMouse>, <VID>, <PID> [,<instance = 1>] )`  
Where `isMouse` is `true` if you wish to find a mouse, or `false` if you wish to find a keyboard.  
eg `AHI.GetDeviceId(false, 0x04F2, 0x0112)`  to find a keyboard with VID 0x04F2 and PID 0x0112  

#### GetKeyboardId
`AHI.GetKeyboardId(<VID>, <PID> [,<instance = 1>] )`  

#### GetMouseId
`AHI.GetMouseId(<VID>, <PID> [,<instance = 1>] )`  

### PS/2 and other Legacy devices (Can also apply to Laptops)  
Some devices (eg older machines with PS/2 interfaces, or some laptops) may not use USB, so these will not have a VID and PID.  
In this case, use the monitor app (Or `GetDeviceList()`) to findle out the "Handle" of your device, and get it's ID from that.

#### GetDeviceIdFromHandle  
`AHI.GetDeviceIdFromHandle(<isMouse>, <handle> [,<instance = 1>] )`  
This works in the same way as `GetDeviceId` above, except you pass a string containing the handle.  
eg `AHI.GetDeviceIdFromHandle(false, "ACPI\PNP0303")`  to find a keyboard with the handle `ACPI\PNP0303`  

#### GetKeyboardIdFromHandle  
`AHI.GetKeyboardIdFromHandle(<handle> [,<instance = 1>] )`  

#### GetMouseIdFromHandle  
`AHI.GetMouseIdFromHandle(<handle> [,<instance = 1>] )`  

### Getting a list of devices
If you wish to get a list of all available devices, you can call `AHI.GetDeviceList()`, which will return an array of `DeviceInfo` objects, each of which has the following properties:  
```
Id
isMouse
Vid
Pid
Handle
```

## Input Detection
AHI has two input detection modes - *Context Mode* and *Subscription Mode*, and both can be used simultaneously.  

### Context mode
Context mode is so named as it takes advantage of AutoHotkey's [Context Sensitive Hotkeys](https://autohotkey.com/docs/Hotkeys.htm#Context).  
As such, only Keyboard Keys and Mouse Buttons are supported in this mode. Mouse Movement is not supported.  

In context mode, you create a *Context Manager* object which turns on/off a set of AHK hotkeys for you.  
You wrap your hotkeys in an #if block which is controlled by the manager.  

Create a Context Manager for the keyboard or mouse, pass it the Interception ID of the device.  
Then Create your hotkeys, wrapped in an `#if` block that checks the `.IsActive` property of your Context Manager  

(Complete, working script)  
```
#include Lib\AutoHotInterception.ahk

keyboard1Id := AHI.GetKeyboardId(0x04F2, 0x0112)
cm1 := AHI.CreateContextManager(keyboard1Id)

#if cm1.IsActive	; Start the #if block
::aaa::JACKPOT
1:: 
	ToolTip % "KEY DOWN EVENT @ " A_TickCount
	return
	
1 up::
	ToolTip % "KEY UP EVENT @ " A_TickCount
	return
#if			; Close the #if block
```

### Subscription mode
In Subscription mode, you bypass AHK's hotkey system completely, and Interception notifies you of key events via callbacks.  
All forms of input are supported in Subscription Mode.  
Subscription Mode overrides Context Mode - that is, if a key on a keyboard has been subscribed to with Subscription Mode, then Context Mode will not fire for that key on that keyboard.  
Each Subscribe endpont also has a corresponding Unsubscribe endpoint, which removes the subscription and any block associated with it.  

#### Subscribing to Keyboard keys
##### Subscribe to a specific key on a specific keyboard  
`SubscribeKey(<deviceId>, <scanCode>, <block>, <callback>, <concurrent>)`  
`UnsubscribeKey(<deviceId>, <scanCode>)`
eg  
`AHI.SubscribeKey(keyboardId, GetKeySC("1"), true, Func("KeyEvent"))`

Callback function is passed state `0` (released) or `1` (pressed)
```
KeyEvent(state){
	ToolTip % "State: " state
}
```
Parameter `<concurrent>` is optional and is <b>false</b> by default meaning that all the events raised for that key will be handled sequentially (i.e. callback function will be called on a single thread). If set to <b>true</b>, a new thread will be created for each event and the callback function will be called on it.

##### Subscribe to all keys on a specific keyboard  
`SubscribeKeyboard(<deviceId>, <block>, <callback>, <concurrent>)`  
eg  
`AHI.SubscribeKeyboard(keyboardId, true, Func("KeyEvent"))`  

Callback function is passed scancode of pressed key and state  
```
KeyEvent(code, state){
	ToolTip % "Keyboard Key - Code: " code ", State: " state
}
```

#### Subscribing to Mouse Buttons
##### Subscribing to a specific button on a specific mouse
`SubscribeMouseButton(<deviceId>, <button>, <block>, <callback>, <concurrent>)`  
`UnsubscribeMouseButton(<deviceId>, <button>)`  
Where `button` is one of:  
```
0: Left Mouse
1: Right Mouse
2: Middle Mouse
3: Side Button 1
4: Side Button 2
5: Mouse Wheel (Vertical)
6: Mouse Wheel (Horizontal)
```
For Mouse Wheel events, the `<state>` parameter will be `1` for Wheel Up / Right and `-1` for Wheel Down / Left

Otherwise, usage is identical to `SubscribeKey`  

##### Subscribing to all buttons on a specific mouse
`SubscribeMouseButtons(<deviceId>, <block>, <callback>, <concurrent>)`  
eg  
`AHI.SubscribeMouseButtons(mouseId, true, Func("MouseButtonEvent"))`  

Callback function is passed ID (See above) of pressed button and state  
```
MouseButtonEvent(code, state){
	ToolTip % "Mouse Button - Code: " code ", State: " state	
}
```

#### Subscribing to Mouse Movement  
**Warning!** When Subscribing to mouse movement, you will get **LOTS** of callbacks.  
Note the CPU usage of the demo Monitor app.  
AutoHotkey is *not good* for handling heavy processing in each callback (eg updating a GUI, like the monitor app does).  
Keep your callbacks **short and efficient** in this mode if you wish to avoid high CPU usage.  

##### Relative Mode  
Relative mode is for normal mice and most trackpads.  
Coordinates will be delta (change)  
Each endpoint has two naming variants for convenience, they both do the same.  

`SubscribeMouseMove(<deviceId>, <block>, <callback>, <concurrent>)`  
`SubscribeMouseMoveRelative(<deviceId>, <block>, <callback>, <concurrent>)`  
`UnsubscribeMouseMove(<deviceId>)`  
`UnsubscribeMouseMoveRelative(<deviceId>)`  
For Mouse Movement, the callback is passed two ints - x and y.  
```
AHI.SubscribeMouseMove(mouseId, false, Func("MouseEvent"))

MouseEvent(x, y){
	[...]
}
```

##### Absolute Mode
Absolute mode is used for Graphics Tablets, Light Guns etc.  
Coordinates will be in the range 0..65535  
`SubscribeMouseMoveAbsolute(<deviceId>, <block>, <callback>, <concurrent>)`  
`UnsubscribeMouseMoveAbsolute(<deviceId>)`  
Again, the callback is passed two ints - x and y.  
```
AHI.SubscribeMouseMoveAbsolute(mouseId, false, Func("MouseEvent"))

MouseEvent(x, y){
	[...]
}
```
## Synthesizing Output
Note that these commands will work in both Context and Subscription modes  
Also note that you can send as any device, regardless of whether you have subscribed to it in some way or not. 

### Sending Keyboard Keys
You can send keys as a specific keyboard using the `SendKeyEvent` method.  
`AHI.SendKeyEvent(<keyboardId>, <scanCode>, <state>)`  
scanCode = the Scan Code of the key  
state = 1 for press, 0 for release  
keyboardId = The Interception ID of the keyboard

```
AHI.SendKeyEvent(keyboardId, GetKeySC("a"), 1)
```

If you subscribe to a key using Subscription mode with the `block` parameter set to true, then send a different key using `SendKeyEvent`, you are transforming that key in a way which is totally invisible to windows (And all apps running on it), and it will respond as appropriate. For example, AHK `$` prefixed hotkeys **will not** be able to tell that this is synthetic input, and will respond to it.

### Sending Mouse Buttons
You can send clicks and other mouse button events with:  
`AHI.SendMouseButtonEvent(<mouseId>, <button>, <state>)`  
Where `button` is the button index, as used in `SubscribeMouseButton`  

When Sending Mouse Wheel events, set `<state>` to `1` for Wheel Up / Right and `-1` for Wheel Down / Left.

If you are working in Absolute mode (eg with a graphics tablet or light guns), you can send mouse button events at specific coordinates using:  
`AHI.SendMouseButtonEventAbsolute(<mouseId>, <button>, <state>, <x>, <y>)`  

### Sending Mouse Movement
#### Relative
To send Relative (Normal) mouse movement, use:  
`AHI.SendMouseMove(<mouseId>, <x>, <y>)`  
X and Y are **not** setting the absolute cursor position, they are **altering current position**  
Note that x and y are **not** in pixels, they are in "Mickeys"  


#### Absolute
To sent Absolute mouse movement, use:  
`AHI.SendMouseMoveAbsolute(<mouseId>, <x>, <y>)`  
Note that Absolute mode will probably not work with FPS style mouse-aim games.  
Note that Absolute mouse move uses coordinates in the range 0..65535 which are NOT screen coordinates. If, for example, you have one 1920x1080 monitor, then divide 65535 by 1920 to find the x position on your screen. This 65535 coordinate space maps to **all** your screens however, so if you have multiple monitors, further maths will be required.

#### Moving the Mouse Cursor
To move the mouse cursor to a specific screen or window coordinate, use:  
`AHI.MoveCursor(<x>, <y> [, <coordMode>, <mouseId>])`  
`coordMode` is optional and is the [CoordMode](https://www.autohotkey.com/docs/commands/CoordMode.htm) to use (Will switch back to current CoordMode after) - Defaults to "Screen".  
`mouseId` is optional and the ID of the mouse to use (Defaults to ID 11 - the first mouse)  
eg  
`AHI.MoveCursor(100, 200)` - move to 100, 200 Screen position using mouse ID 11  
`AHI.MoveCursor(100, 200, "Window")` - move to 100, 200 Window position using mouse ID 11  
`AHI.MoveCursor(100, 200, , 12)` - move to 100, 200 Screen position using mouse ID 12  

## Compiling scripts
AHI scripts can be compiled (Right click the script and select "Compile")  
All required DLLs will be packed inside the EXE, so only the EXE needs to be distributed  
When the EXE is run, the `Lib` folder will be created with the required DLLs  
