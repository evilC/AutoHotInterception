# AutoHotInterception

AutoHotInterception(AHI) allows you to execute AutoHotkey code in response to keys on a *specific* keyboard, whilst (optionally) blocking the native functionality of that key.  
In other words, you can use a key on a second (or third, or fourth..) keyboard to trigger AHK code, and that key will not be seen by applications. You can use the *same key* on multiple keyboards for individual actions.  

AHI uses the Interception driver by Francisco Lopez  

# WARNING
**TAKE CARE** when using this code. Because Interception is a driver, and sits below windows proper, blocking with Interception goes so deep that it can even block CTRL+ALT+DEL etc. It is entirely possible to lock up all input, requiring use of the reset button.  
For example, using Subscription Mode with `block` enabled will **totally** block that key from working on that keyboard.
So if you block `Ctrl` on your only keyboard, you just blocked CTRL+ALT+DEL.  
Be wary of making scripts using this code run on startup. Know how to enter "Safe Mode" in windows and disable startup of the scripts.  
As they say - ***With great power comes great responsibility***.  

# Setup
1. Download and install the [Interception Driver](http://www.oblita.com/interception)  
2. Download a zip from the releases page and extract it to a folder
3. Copy the `interception.dll` from the folder where you ran the interecption install into the `lib` folder  
(You can optionally place the contents of the `lib` folder in `My Documents\AutoHotkey\lib`
4. Edit the example script, enter the VID and PID of your keyboard
5. Run the example script

# Usage
## Initializing the Library
Include the library
```
#Persistent ; (Interception hotkeys do not stop AHK from exiting, so use this)
#include Lib\AutoHotInterception.ahk
```

Initialize the library
```
Interception := AutoHotInterception_Init()
``` 

## Modes
There are two modes of operation for AHI, and both can be used simultaneously.  

### Context mode
Context mode is so named as it takes advantage of AutoHotkey's [Context Sensitive Hotkeys](https://autohotkey.com/docs/Hotkeys.htm#Context).  
In AHK, you can wrap your hotkeys in a block like so:
```
#if myVariable == 1
F1::Msgbox You Pressed F1
#if
```
This hotkey would only fire if the `myVariable` was 1.  
In context mode, you subscribe to a keyboard, and any time events for that keyboard are just about to happen, AHI fires your callback and passes it `1`. Your code then sets the context variable to `1` which enables the hotkey.  
AHI then sends the key, which triggers your hotkey.  
AHI then fires the callback once more, passing `0` and the context variable gets set back to `0`, disabling the hotkey.  

#### Step 1
Register your callback with AHI  
```
VID := 0x04F2, PID := 0x0112
Interception.SetContextCallback(VID, PID, Func("SetKb1Context"))
```

#### Step 2
Create your callback function, and set the context variable to the value of `state`
```
SetKb1Context(state){
	global isKeyboard1Active
	Sleep 0		; We seem to need this for hotstrings to work, not sure why
	isKeyboard1Active := state
}
```

#### Step 3
Create your hotkeys, wrapped in an `#if` block for that context variable
```
#if isKeyboard1Active
::aaa::JACKPOT
1:: 
	ToolTip % "KEY DOWN EVENT @ " A_TickCount
	return
	
1 up::
	ToolTip % "KEY UP EVENT @ " A_TickCount
	return
#if
```

### Subscription mode
In Subscription mode, you bypass AHK's hotkey system completely, and Interception notifies you of key events via callbacks.  
Subscription Mode overrides Context Mode - that is, if a key on a keyboard has been subscribed to with Subscription Mode, then Context Mode will not fire for that key on that keyboard.  

Subscribe to a key on a specific keyboard
`SubscribeKey(<scanCode>, <block>, <callback>, <VID>, <PID>`
```
VID := 0x04F2, PID := 0x0112
Interception.SubscribeKey(GetKeySC("1"), true, Func("KeyEvent"), VID, PID)
return
```

Callback function is passed state `0` (released) or `1` (pressed)
```
KeyEvent(state){
	ToolTip % "State: " state
}
```

## Sending Keys
You can send keys as a specific keyboard using the `SendKeyEvent` method.  
`Interception.SendKeyEvent(<scanCode>, <state> [, <keyboardId = 1>])`  
scanCode = the Scan Code of the key  
state = 1 for press, 0 for release  
keyboardId = The Interception ID of the keyboard (Leave blank to use 1st keyboard)  

```
Interception.SendKeyEvent(GetKeySC("a"), 1)
```

```
VID := 0x04F2, PID := 0x0112
keyboardId := Interception.GetDeviceId(VID, PID)
Interception.SendKeyEvent(GetKeySC("a"), 1, keyboardId)
```
If you subscribe to a key using Subscription mode with the `block` parameter set to true, then send a different key using `SendKeyEvent`, you are transforming that key in a way which is totally invisible to windows (And all apps running on it), and it will respond as appropriate. For example, AHK `$` prefixed hotkeys **will not** be able to tell that this is synthetic input, and will respond to it.
