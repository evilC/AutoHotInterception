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
As such, only Keyboard Keys and Mouse Buttons are supported in this mode. Mouse Movement is not supported.  
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
It is advised to **NOT** do anything in this callback that takes a significant amount of time. Do not wait for key presses or releases and such.  
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
<<<<<<< HEAD
All forms of input are supported in Subscription Mode.  
=======
Subscription Mode overrides Context Mode - that is, if a key on a keyboard has been subscribed to with Subscription Mode, then Context Mode will not fire for that key on that keyboard.  
>>>>>>> 4a0266aabbb14983a6ecc1fd662d6d645ad0b34e

#### Keyboard
Subscribe to a key on a specific keyboard
`SubscribeKey(<scanCode>, <block>, <callback>, <VID>, <PID>)`
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

<<<<<<< HEAD
#### Mouse Buttons
`SubscribeMouseButton(<button>, <block>, <callback>, <VID>, <PID>)`  
Where `button` is one of:  
```
0: Left Mouse
1: Right Mouse
2: Middle Mouse
3: Side Button 1
4: Side Button 2
```  
Otherwise, usage is identical to `SubscribeKey`  

#### Mouse Movement
`SubscribeMouseMove(<block>, <callback>, <VID>, <PID>)`  
For Mouse Movement, the callback is passed two ints - x and y.  
```
VID := 0x04F2, PID := 0x0112
Interception.SubscribeMouseMove(false, Func("MouseEvent"), VID, PID)

MouseEvent(x, y){
	[...]
}
```
=======
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
>>>>>>> 4a0266aabbb14983a6ecc1fd662d6d645ad0b34e
