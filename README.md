# AutoHotInterception

AutoHotInterception(AHI) allows you to execute AutoHotkey code in response to events from a *specific* keyboard or mouse, whilst (optionally) blocking the native functionality (ie stopping Windows from seeing that keyboard or mouse event).  
In other words, you can use a key on a second (or third, or fourth..) keyboard to trigger AHK code, and that key will not be seen by applications. You can use the *same key* on multiple keyboards for individual actions.  
Keyboard Keys, Mouse Buttons and (Relative) Mouse movement are supported. Support for Absolute Mouse movement is planned.  

AHI uses the Interception driver by Francisco Lopez  

# WARNING
**TAKE CARE** when using this code. Because Interception is a driver, and sits below windows proper, blocking with Interception goes so deep that it can even block CTRL+ALT+DEL etc. As such, it is entirely possible to lock up all input, or at least make life a little difficult.  
In general, worst-case scenario would require use of the reset button.  
For example, using Subscription Mode with `block` enabled will **totally** block that key from working on that keyboard.
So if you block `Ctrl` on your only keyboard, you just blocked CTRL+ALT+DEL.  
This is less of an issue whilst AHI does not support mouse blocking (As you could probably kill the script with just the mouse), but if/when that happens, the potential is there.  
Be wary of making scripts using this code run on startup. Know how to enter "Safe Mode" in windows and disable startup of the scripts. Know mouse alternatives to emergency keyboard actions (Right click on clock for Task Manager!)    
As they say - ***With great power comes great responsibility***.  
If this all scares you and you don't really understand it, then TL/DR is you should probably stick to "Context Mode", it's safer.  

# Setup
1. Download and install the [Interception Driver](http://www.oblita.com/interception)  
2. Download an AHI release from the [releases page](https://github.com/evilC/AutoHotInterception/releases) and extract it to a folder.  
DO NOT use the "Clone or Download" link on the main page.  
This is the folder where (at least initially) you will be running scripts from.  
It contains a number of sample `.ahk` scripts and a `lib` folder, which contains all the libraries and files needed for AHI.  
3. Copy the `interception.dll` from the folder where you ran the interecption install into the `lib` folder that was created in step (2)  
4. Right-click `Unblocker.ps1` in the lib folder and select `Run as Admin`.  
This is because downloaded DLLs are often blocked and will not work.  
This can be done manually by right clicking the DLLs, selecting Properties, and checking a "Block" box if it exists.  
5. Edit the example script, enter the VID and PID of your keyboard  
6. Run one of the sample scripts  
7. (Optional) The contents of the `lib` folder can actually be placed in one of the AutoHotkey lib folders (eg `My Documents\AutoHotkey\lib` - make it if it does not exist), and the `#include` lines of the sample scripts changed to `#include <AutoHotInterception>`, to enable your AHI scripts to be in any folder, without each needing it's own copy of the library files.  

# Device IDs / VIDs PIDs etc  
Interception identifies unique devices by an ID. This is a number from 1..21.  
Devices 1-10 are always keyboards  
Devices 11-21 are always mice  
This ID scheme is totally unique to Interception, and IDs may change as you plug / unplug devices etc.  
On PC, devices are often identified by VendorID (VID) and ProductID (PID). These are identifiers baked into the hardware at time of manufacture, and are identical for all devices of the same make / model.  
Most AHI functions (eg to Subscribe to a key etc) use an Interception ID, so some handy functions are provided to allow you to find the (current) Interception ID of your device, given a VID / PID.  
If you are unsure of what the VID / PID of your device is (or even if Interception can see it), you can use the included Monitor script to find it.  

# Usage
## Initializing the Library
Include the library
```
#Persistent ; (Interception hotkeys do not stop AHK from exiting, so use this)
#include Lib\AutoHotInterception.ahk
```

Initialize the library
```
global AHI := InterceptionWrapper()
``` 
In this case, `AHI` is actually an AHK class - it wraps the AHI DLL and provides some extra functionality.  
The majority of the documented commands can be called directly on the DLL itself by calling them on `AHI.Instance` instead of `AHI`.  

## Finding Device IDs  
### Finding a specific device  
In most cases, you will want to hard-wire a script to a specific VID/PID - in this instance, use this method:  
`AHI.GetDeviceId(<isMouse>, <VID>, <PID> [,<instance = 1>] )`  
Where `isMouse` is `true` if you wish to find a mouse, or `false` if you wish to find a keyboard.  
eg `AHI.GetDeviceId(false, 0x04F2, 0x0112)`  to find a keyboard with VID 0x04F2 and PID 0x0112  
If you have multiple identical VID/PID devices, specify an `instance` (Starts from 1).  

### Getting a list of devices
If you wish to get a list of all available devices, you can call `AHI.GetDeviceList()`, which will return an array of `DeviceInfo` objects, each of which has the following properties:  
```
Id
isMouse
Vid
Pid
```

## Modes
There are two modes of operation for AHI, and both can be used simultaneously.  

### Context mode
Context mode is so named as it takes advantage of AutoHotkey's [Context Sensitive Hotkeys](https://autohotkey.com/docs/Hotkeys.htm#Context).  
As such, only Keyboard Keys and Mouse Buttons are supported in this mode. Mouse Movement is not supported.  
In AHK, you can wrap your hotkeys in a block like so:
```
#if myVariable == 1
F1::Msgbox You Pressed F1
#if
```
This hotkey would only fire if the `myVariable` was 1.  
In context mode, you create a "Context Manager" object which turns on/off a set of AHK hotkeys for you.  
You wrap your hotkeys in an #if block which is controlled by the manager.  

#### Step 1
Create a Context Manager for the keyboard
```
keyboard1Id := AHI.GetKeyboardId(0x04F2, 0x0112)
cm1 := AHI.CreateContextManager(keyboard1Id)
```

#### Step 2
Create your hotkeys, wrapped in an `#if` block that checks the `.IsActive` property of the Context Manager  
```
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

#### Subscribing to Keyboard keys
Subscribe to a key on a specific keyboard
`SubscribeKey(<deviceId>, <scanCode>, <block>, <callback>)`
```
Interception.SubscribeKey(keyboardId, GetKeySC("1"), true, Func("KeyEvent"))
return
```

Callback function is passed state `0` (released) or `1` (pressed)
```
KeyEvent(state){
	ToolTip % "State: " state
}
```

#### Subscribing to Mouse Buttons
`SubscribeMouseButton(<deviceId>, <button>, <block>, <callback>)`  
Where `button` is one of:  
```
0: Left Mouse
1: Right Mouse
2: Middle Mouse
3: Side Button 1
4: Side Button 2
```  
Otherwise, usage is identical to `SubscribeKey`  

#### Subscribing to Mouse Movement  
**Warning!** When Subscribing to mouse movement, you will get **LOTS** of callbacks.  
Note the CPU usage of the demo Monitor app.  
AutoHotkey is *not good* for handling heavy processing in each callback (eg updating a GUI, like the monitor app does).  
Keep your callbacks **short and efficient** in this mode if you wish to avoid high CPU usage.  

##### Relative Mode  
Relative mode is for normal mice and most trackpads.  
Coordinates will be delta (change)
`SubscribeMouseMove(<deviceId>, <block>, <callback>)`  
For Mouse Movement, the callback is passed two ints - x and y.  
```
Interception.SubscribeMouseMove(mouseId, false, Func("MouseEvent"))

MouseEvent(x, y){
	[...]
}
```

##### Absolute Mode
Absolute mode is used for Graphics Tablets, Light Guns etc.  
Coordinates will be in the range 0..65535  
```
Interception.SubscribeMouseMoveAbsolute(mouseId, false, Func("MouseEvent"))

MouseEvent(x, y){
	[...]
}
```
## Synthesizing Output
Note that these commands will work in both Context and Subscription modes  
Also note that you can send as any device, regardless of whether you have subscribed to it in some way or not. 

### Sending Keyboard Keys
You can send keys as a specific keyboard using the `SendKeyEvent` method.  
`Interception.SendKeyEvent(<keyboardId>, <scanCode>, <state>)`  
scanCode = the Scan Code of the key  
state = 1 for press, 0 for release  
keyboardId = The Interception ID of the keyboard

```
Interception.SendKeyEvent(keyboardId, GetKeySC("a"), 1)
```

If you subscribe to a key using Subscription mode with the `block` parameter set to true, then send a different key using `SendKeyEvent`, you are transforming that key in a way which is totally invisible to windows (And all apps running on it), and it will respond as appropriate. For example, AHK `$` prefixed hotkeys **will not** be able to tell that this is synthetic input, and will respond to it.

### Sending Mouse Buttons
`Interception.SendMouseButtonEvent(<mouseId>, <button>, <state>)`  
Where `button` is the button index, as used in `SubscribeMouseButton`  

### Sending Mouse Movement
#### Relative
`Interception.SendMouseMove(<mouseId>, <x>, <y>)`  

#### Absolute
`Interception.SendMouseMoveAbsolute(<mouseId>, <x>, <y>)`  

## Monitor App
ToDo: Add recording of monitor app
