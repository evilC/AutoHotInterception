/*
Script to show data flowing from Interception
*/
#SingleInstance force
#Persistent
#include <AutoHotInterception>

OutputDebug DBGVIEWCLEAR

global DeviceList := {}
filterMouseMove := 1
filterKeyPress := 0

;~ global Monitor := AutoHotInterception_Init("InterceptionMonitor")
MonitorWrapper := new AutoHotInterception("Monitor")
global Monitor := MonitorWrapper.Instance

DeviceList := MonitorWrapper.GetDeviceList()

start := 0

colWidth := 350
Gui, Add, Text, w%colWidth% Center Section, Keyboards
Loop 2 {
	isMouse := A_Index - 1
	Loop 10 {
		i := start + A_Index
		dev := DeviceList[i]
		if (!IsObject(dev)){
			continue
		}
		Gui, Add, Checkbox, % "hwndhCb w" colWidth, % "ID: " dev.id ", VID: 0x" FormatHex(dev.VID) ", PID: 0x" FormatHex(dev.PID) "`nHandle: " StrReplace(dev.Handle, "&", "&&")
		fn := Func("CheckboxChanged").Bind(dev.id)
		GuiControl, +g, % hCb, % fn
	}
	if (!IsMouse){
		Gui, Add, Text, x+5 ym w%colWidth% Center Section, Mice
		start := 10
	}
}

Gui, Add, CheckBox, w%colWidth% y+20 hwndhCbFilterMove Checked, Filter Movement (Warning: Turning off can cause crashes)
fn := Func("FilterMove")
GuiControl, +g, % hCbFilterMove, % fn
Gui, Add, CheckBox, w%colWidth% x10 y52 hwndhCbFilterPress, Show Presses instead of ups and downs
fnn := Func("FilterPress")
GuiControl, +g, % hCbFilterPress, % fnn
Gui, Add, Button, xm w%colWidth% Center gClearKeyboard, Clear
Gui, Add, Button, x+5 yp w%colWidth% gClearMouse Center, Clear

Gui, Add, ListView, xm w%colWidth% h400 hwndhLvKeyboard, ID|Code|State|Key Name|Info
LV_ModifyCol(4, 100)
LV_ModifyCol(5, 150)
Gui, Add, ListView, x+5 yp w%colWidth% h400 hwndhLvMouse, ID|Code|State|X|Y|Info
LV_ModifyCol(6, 200)
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
