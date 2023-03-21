/*
Script to show data flowing from Interception
*/
#SingleInstance force
#Persistent
#include Lib\AutoHotInterception.ahk

OutputDebug DBGVIEWCLEAR

Gui, Margin, 0, 0
DeviceList := {}
filterMouseMove := 1
filterKeyPress := 0

AHI := new AutoHotInterception()

; Device List
DeviceList := AHI.GetDeviceList()

marginX := 10
marginY := 10
idW := 50		; Width of the ID text
vhOff := 7		; Width to space VIDPID / Handle above/below ID row
copyW := 40		; Width of the Copy buttons
outputH := 350	; Height of the Output boxes
rowH := 35		; Distance between each row of devices

maxWidths := {K: 0, M: 0}						; Max Width of device entries for each column
totalWidths := {K: 0, M: 0}						; Total Width of each column
devTypes := ["K", "M"]							; Lookup table for device type
starts := {K: 0, M: 10}							; Start IDs for each device type
columnTitles := {K: "Keyboards", M: "Mice"}		; Column Titles
columnX := {K: 0, M: 0}

Loop 2 {
	strings := {}
	devType := devTypes[A_Index]
	columnX[devType] := GetColX(devType)

	start := starts[devType]
	UpdateWidth(0, 1) ; Reset max width
	
	; Add device entries
	Loop 10 {
		i := start + A_Index
		dev := DeviceList[i]
		if (!IsObject(dev)){
			continue
		}
		rowY := (marginY * 3) + ((A_Index - 1) * rowH)
		Gui, Add, Checkbox, % "hwndhwnd x" columnX[devType] " y" rowY " w" idW, % "ID: " dev.id
		fn := Func("CheckboxChanged").Bind(dev.id)
		GuiControl, +g, % hwnd, % fn
		lowest := UpdateLowest(hwnd)
		strings[A_index] := {vid:FormatHex(dev.VID), pid: FormatHex(dev.PID), handle: dev.Handle}
		Gui, Add, Text, % "hwndhwnd x" columnX[devType] + idW " y" rowY - vhOff, % "VID / PID:`t0x" strings[A_index].vid ", 0x" strings[A_index].pid
		maxWidths[devType] := UpdateWidth(hwnd)
		Gui, Add, Text, % "hwndhwnd x" columnX[devType] + idW " y" rowY + vhOff, % "Handle:`t`t" StrReplace(strings[A_index].Handle, "&", "&&")
		maxWidths[devType] := UpdateWidth(hwnd)
	}

	; Add copy buttons
	Loop 10 {
		i := start + A_Index
		dev := DeviceList[i]
		if (!IsObject(dev)){
			continue
		}
		rowY := (marginY * 3) + ((A_Index - 1) * rowH)
		fn := Func("CopyClipboard").Bind("0x" strings[A_index].vid ", 0x" strings[A_index].pid)
		xpos := columnX[devType] + idW + maxWidths[devType]
		Gui, Add, Button, % "x" xpos " y" rowY - vhOff " h14 w" copyW " hwndhwnd", Copy
		GuiControl, +g, % hwnd, % fn
		
		fn := Func("CopyClipboard").Bind(strings[A_index].handle)
		Gui, Add, Button, % "x" xpos " y" rowY + vhOff " h14 w" copyW " hwndhwnd", Copy
		GuiControl, +g, % hwnd, % fn
	}

	totalWidths[devType] := idW + maxWidths[devType] + copyW
	Gui, Add, Text, % "x" columnX[devType] " y5 w" totalWidths[devType] " Center", % columnTitles[devType]
}

lowest += 2 * MarginY

; Options
Gui, Add, CheckBox, % "x" columnX.K " y" lowest " hwndhCbFilterPress", Only show key releases
fn := Func("FilterPress")
GuiControl, +g, % hCbFilterPress, % fn

Gui, Add, CheckBox, % "x" columnX.M " w" totalWidths[devType] " yp hwndhCbFilterMove Checked", Filter Movement (Warning: Turning off can cause crashes)
fn := Func("FilterMove")
GuiControl, +g, % hCbFilterMove, % fn

lowest += 2 * MarginY

Gui, Add, Button, % "x" columnX.K " y" lowest " w" totalWidths.K " Center gClearKeyboard", Clear
Gui, Add, Button, % "x" columnX.M " yp w" totalWidths.M " gClearMouse Center", Clear

lowest += 30

; Output
Gui, Add, ListView, % "x" columnX.K " y" lowest " w" totalWidths.K " h" outputH " hwndhLvKeyboard", ID|Code|State|Key Name
LV_ModifyCol(4, 100)
Gui, Add, ListView, % "x" columnX.M " yp w" totalWidths.M " h" outputH " hwndhLvMouse", ID|Code|State|X|Y|Info
LV_ModifyCol(6, 200)

lowest += outputH

Gui, Show, % "w" (marginX * 3) + totalWidths.K + totalWidths.M " h" marginY + lowest, AutoHotInterception Monitor


;~ Monitor.Subscribe(Func("KeyboardEvent"), Func("MouseEvent"))

return

GetColX(devType){
	global marginX, idW, maxWidths, copyW
	if (devType == "K")
		return marginX
	else
		return (marginX * 2) + idW + maxWidths["K"] + copyW
}

UpdateLowest(hwnd){
	static max := 0
	GuiControlGet, cp, pos, % hwnd
	pos := cpY + cpH
	if (pos > max){
		max := pos
	}
	return max
}

UpdateWidth(hwnd, reset := 0){
	static max := 0
	if (reset){
		max := 0
		return
	}
	GuiControlGet, cp, pos, % hwnd
	if (cpW > max){
		max := cpW
	}
	return max
}

CheckboxChanged(id, hwnd){
	global AHI
	GuiControlGet, state, , % hwnd
	if (state){
		if (id < 11){
			AHI.SubscribeKeyboard(id, false, Func("KeyboardEvent").Bind(id))
		} else {
			AHI.SubscribeMouseButtons(id, false, Func("MouseButtonEvent").Bind(id))
			AHI.SubscribeMouseMoveRelative(id, false, Func("MouseAxisEvent").Bind(id, "Relative Move"))
			AHI.SubscribeMouseMoveAbsolute(id, false, Func("MouseAxisEvent").Bind(id, "Absolute Move"))
		}
	} else {
		if (id < 11){
			AHI.UnsubscribeKeyboard(id)
		} else {
			AHI.UnsubscribeMouseButtons(id)
			AHI.UnsubscribeMouseMoveRelative(id)
			AHI.UnsubscribeMouseMoveAbsolute(id)
		}
	}
	;~ ToolTip % "Changed " id " to " state ". Return value: " ret
}

Foo(code, state){
	Tooltip % code ", " state
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

KeyboardEvent(id, code, state){
	global hLvKeyboard, filterKeyPress
    if (filterKeyPress && state)
        return
	Gui, ListView, % hLvKeyboard
	scanCode := Format("{:x}", code)
	keyName := GetKeyName("SC" scanCode)
	row := LV_Add(, id, code, state, keyName)
	LV_Modify(row, "Vis")
}

MouseButtonEvent(id, code, state){
	global hLvMouse
	Gui, ListView, % hLvMouse
	row := LV_Add(, id, code, state, "", "", "Button")
	LV_Modify(row, "Vis")
}

MouseAxisEvent(id, info, x, y){
	global hLvMouse, filterMouseMove
	if (filterMouseMove)
		return
	Gui, ListView, % hLvMouse
	row := LV_Add(, id, "", "", x, y, info)
	LV_Modify(row, "Vis")
}

CopyClipboard(str){
	Clipboard := str
	Tooltip Copied to Clipboard
	SetTimer, ClearTooltip, 1000
}

ClearTooltip:
	ToolTip
	return

^Esc::
GuiClose:
	ExitApp
