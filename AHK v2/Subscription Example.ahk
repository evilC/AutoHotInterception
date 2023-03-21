#SingleInstance force
Persistent
#include Lib\AutoHotInterception.ahk

AHI := AutoHotInterception()

keyboardId := AHI.GetKeyboardId(0x04F2, 0x0112)
AHI.SubscribeKey(keyboardId, GetKeySC("1"), true, KeyEvent)
return

KeyEvent(state){
	ToolTip("State: " state)
}

^Esc::
{
	ExitApp
}