/*
Script to show data flowing from Interception
*/
#SingleInstance force
#Persistent
#include Lib\AutoHotInterception.ahk

OutputDebug DBGVIEWCLEAR

global DeviceList := {}
filterMouseMove := 1

;~ global Monitor := AutoHotInterception_Init("InterceptionMonitor")
MonitorWrapper := new AutoHotInterception("Monitor")
global Monitor := MonitorWrapper.GetInstance()

DeviceList := MonitorWrapper.GetDeviceList()

start := 1

Gui, Add, Text, w300 Center Section, Keyboards
Loop 2 {
	isMouse := A_Index - 1
	Loop 10 {
		i := start + A_Index
		dev := DeviceList[i]
		if (!IsObject(dev)){
			continue
		}
		Gui, Add, Checkbox, % "hwndhCb w300", % "ID: " dev.id ", VID: 0x" FormatHex(dev.VID) ", PID: 0x" FormatHex(dev.PID)
		fn := Func("CheckboxChanged").Bind(dev.id)
		GuiControl, +g, % hCb, % fn
	}
	if (!IsMouse){
		Gui, Add, Text, x+5 ym w300 Center Section, Mice
		start := 11
	}
}

Gui, Add, CheckBox, w300 y+20 hwndhCbFilterMove Checked, Filter Movement (Warning: Turning off can cause crashes)
fn := Func("FilterMove")
GuiControl, +g, % hCbFilterMove, % fn
Gui, Add, Button, xm w300 Center gClearKeyboard, Clear
Gui, Add, Button, x+5 yp w300 gClearMouse Center, Clear

Gui, Add, ListView, xm w300 h400 hwndhLvKeyboard, ID|State|Code|Info
Gui, Add, ListView, x+5 yp w300 h400 hwndhLvMouse, ID|State|Flags|Rolling|X|Y|Info
LV_ModifyCol(5, 50)
LV_ModifyCol(6, 50)
Gui, Show


Monitor.Subscribe(Func("KeyboardEvent"), Func("MouseEvent"))
return

CheckboxChanged(id, hwnd){
	GuiControlGet, state, , % hwnd
	ret := Monitor.SetDeviceFilterState(id, state)
	;~ ToolTip % "Changed " id " to " state ". Return value: " ret
}

FilterMove(hwnd){
	global filterMouseMove
	GuiControlGet, state, , % hwnd
	filterMouseMove := state
}

ClearKeyboard:
	Gui, ListView, % hLvKeyboard
	LV_Delete()
	return

ClearMouse:
	Gui, ListView, % hLvMouse
	LV_Delete()
	return

FormatHex(num){
	return Format("{:04X}", num)
}

KeyboardEvent(id, state, code, info){
	global hLvKeyboard
	Gui, ListView, % hLvKeyboard
	row := LV_Add(, id, state, code, info)
	LV_Modify(row, "Vis")
	;~ ToolTip % "Keybd: " id "`nState: " state ", Code: " code
}

MouseEvent(id, state, flags, rolling, x, y, info){
	global hLvMouse, filterMouseMove
	if (filterMouseMove && !state)
		return
	Gui, ListView, % hLvMouse
	row := LV_Add(, id, state, flags, rolling, x, y, info)
	LV_Modify(row, "Vis")
	;~ ToolTip % "Mouse: " id "`nX: " x ", Y: " y
}

^Esc::
GuiClose:
	ExitApp