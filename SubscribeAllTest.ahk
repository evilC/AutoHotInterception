#SingleInstance force
#Persistent
#include Lib\AutoHotInterception.ahk

AHI := new AutoHotInterception()

keyboardId := AHI.GetKeyboardId(0x04F2, 0x0112)
AHI.SubscribeKeyboard(keyboardId, true, Func("KeyEvent"))
return

KeyEvent(code, state){
	ToolTip % "Code: " code ", State: " state
}

^Esc::
	ExitApp