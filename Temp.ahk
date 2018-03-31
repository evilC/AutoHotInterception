#SingleInstance force
#Persistent
#include Lib\AutoHotInterception.ahk

AHI := new AutoHotInterception()

;~ mouseId := AHI.GetMouseId(0x46D, 0xC531)
mouseId := AHI.GetMouseId(0x0B57, 0x9091)
;~ ToolTip % "MouseId: " mouseId
;~ AHI.SubscribeKey(keyboardId, GetKeySC("1"), true, Func("KeyEvent"))
AHI.SubscribeMouseMoveAbsolute(mouseId, false, Func("MouseEvent"))
return

F12::
	AHI.SendMouseButtonEventAbsolute(mouseId, 0, 1, 0, 20000)
	Sleep 10
	AHI.SendMouseButtonEventAbsolute(mouseId, 0, 0, 0, 20000)
	;~ AHI.SendMouseMoveAbsolute(mouseId, 0, 20000)
	;~ AHI.SendMouseMoveRelative(mouseId, 100, 0)
	;~ AHI.SendMouseButtonEvent(mouseId, 1, 1)
	;~ Sleep 10
	;~ AHI.SendMouseButtonEvent(mouseId, 1, 0)
	return

KeyEvent(state){
	ToolTip % "State: " state
}

MouseEvent(x, y){
	;~ ToolTip % "x:" x
}

^Esc::
	ExitApp