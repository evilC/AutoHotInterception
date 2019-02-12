#SingleInstance force
#Persistent
#include Lib\AutoHotInterception.ahk

; Demonstrates Subscribe / Unsubscribe (Turn on/off block) dependent on active window
; Block is active in Notepad, inactive otherwise

AHI := new AutoHotInterception()

keyboardId := AHI.GetKeyboardId(0x04F2, 0x0112)

SetTimer, WatchWin, -0
return

KeyEvent(state){
	ToolTip % "State: " state
}

DoSub(state){
	global AHI, keyboardId
	if (state){
		AHI.SubscribeKey(keyboardId, GetKeySC("1"), true, Func("KeyEvent"))
	} else {
		AHI.UnsubscribeKey(keyboardId, GetKeySC("1"))
	}
}

WatchWin:
	Loop {
		WinWaitActive, ahk_class Notepad
		DoSub(true)
		WinWaitNotActive, ahk_class Notepad
		DoSub(false)
	}
	return

^Esc::
	ExitApp