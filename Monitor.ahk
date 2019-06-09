/*
Script to show data flowing from Interception
*/
#SingleInstance force
#Persistent
#include Lib\AutoHotInterception.ahk

OutputDebug DBGVIEWCLEAR

DeviceList := {}
filterMouseMove := 1
filterKeyPress := 0

MonitorWrapper := new AutoHotInterception("Monitor")
Monitor := MonitorWrapper.Instance

; Device List
DeviceList := MonitorWrapper.GetDeviceList()

colWidth := 350
Gui, Add, Text, w%colWidth% Center Section, Keyboards
Loop 10 {
	i := A_Index
	dev := DeviceList[i]
	if (!IsObject(dev)){
		continue
	}
	Gui, Add, Checkbox, % "hwndhCb w" colWidth, % "ID: " dev.id ", VID: 0x" FormatHex(dev.VID) ", PID: 0x" FormatHex(dev.PID) "`nHandle: " StrReplace(dev.Handle, "&", "&&")
	fn := Func("CheckboxChanged").Bind(dev.id)
	GuiControl, +g, % hCb, % fn
	lowest := UpdateLowest(hCb)
}

Gui, Add, Text, x+5 ym w%colWidth% Center Section, Mice
Loop 10 {
	i := A_Index + 10
	dev := DeviceList[i]
	if (!IsObject(dev)){
		continue
	}
	Gui, Add, Checkbox, % "hwndhCb w" colWidth, % "ID: " dev.id ", VID: 0x" FormatHex(dev.VID) ", PID: 0x" FormatHex(dev.PID) "`nHandle: " StrReplace(dev.Handle, "&", "&&")
	fn := Func("CheckboxChanged").Bind(dev.id)
	GuiControl, +g, % hCb, % fn
	lowest := UpdateLowest(hCb)
}

; Options
Gui, Add, CheckBox, % "w" colWidth " x10 y" lowest + 20 " hwndhCbFilterPress", Only show key releases
fn := Func("FilterPress")
GuiControl, +g, % hCbFilterPress, % fn

Gui, Add, CheckBox, % "x+5 w" colWidth " y" lowest + 20 " hwndhCbFilterMove Checked", Filter Movement (Warning: Turning off can cause crashes)
fn := Func("FilterMove")
GuiControl, +g, % hCbFilterMove, % fn

Gui, Add, Button, xm w%colWidth% Center gClearKeyboard, Clear
Gui, Add, Button, x+5 yp w%colWidth% gClearMouse Center, Clear

; Output
Gui, Add, ListView, xm w%colWidth% h400 hwndhLvKeyboard, ID|Code|State|Key Name|Info
LV_ModifyCol(4, 100)
LV_ModifyCol(5, 150)
Gui, Add, ListView, x+5 yp w%colWidth% h400 hwndhLvMouse, ID|Code|State|X|Y|Info
LV_ModifyCol(6, 200)
Gui, Show


Monitor.Subscribe(Func("KeyboardEvent"), Func("MouseEvent"))
return

UpdateLowest(hwnd){
	static lowest := 0
	GuiControlGet, cp, pos, % hwnd
	low := cpY + cpH
	if (low > lowest){
		lowest := low
	}
	return lowest
}

CheckboxChanged(id, hwnd){
	global Monitor
	GuiControlGet, state, , % hwnd
	ret := Monitor.SetDeviceFilterState(id, state)
	;~ ToolTip % "Changed " id " to " state ". Return value: " ret
}

FilterMove(hwnd){
	global filterMouseMove
	GuiControlGet, state, , % hwnd
	filterMouseMove := state
}

FilterPress(hwnd){
	global filterKeyPress
	GuiControlGet, state, , % hwnd
	filterKeyPress := state
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

KeyboardEvent(id, code, state, info){
	global hLvKeyboard, filterKeyPress
    if (filterKeyPress && state)
        return
	Gui, ListView, % hLvKeyboard
	scanCode := Format("{:x}", code)
	keyName := GetKeyName("SC" scanCode)
	row := LV_Add(, id, code, state, keyName, info)
	LV_Modify(row, "Vis")
	;~ ToolTip % "Keybd: " id "`nState: " state ", Code: " code
}

MouseEvent(id, code, state, x, y, info){
	global hLvMouse, filterMouseMove
	if (filterMouseMove && (x != 0 || y != 0))
		return
	Gui, ListView, % hLvMouse
	row := LV_Add(, id, code, state, x, y, info)
	LV_Modify(row, "Vis")
	;~ ToolTip % "Mouse: " id "`nX: " x ", Y: " y
}

^Esc::
GuiClose:
	ExitApp
