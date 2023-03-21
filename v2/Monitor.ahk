/*
Script to show data flowing from Interception
*/
#SingleInstance force
Persistent
#include Lib\AutoHotInterception.ahk

OutputDebug("DBGVIEWCLEAR")

monitorGui := Gui("", "AutoHotInterception Monitor")
monitorGui.MarginX := 0
monitorGui.MarginY := 0
monitorGui.OnEvent("Close", GuiClosed)

DeviceList := {}
filterMouseMove := 1
filterKeyPress := 0

AHI := AutoHotInterception()

; Device List
DeviceList := AHI.GetDeviceList()

marginX := 10
marginY := 10
idW := 50		; Width of the ID text
vhOff := 7		; Width to space VIDPID / Handle above/below ID row
copyW := 40		; Width of the Copy buttons
outputH := 350	; Height of the Output boxes
rowH := 35		; Distance between each row of devices

maxWidths := Map("K", 0, "M", 0)					; Max Width of device entries for each column
totalWidths := Map("K", 0, "M", 0)					; Total Width of each column
tW := Map("K", 0, "M", 0)
devTypes := ["K", "M"]								; Lookup table for device type
starts := Map("K", 0, "M", 10)						; Start IDs for each device type
columnTitles := Map("K", "Keyboards", "M", "Mice")	; Column Titles
columnX := Map("K", 0, "M", 0)


Loop 2 {
	strings := Map()
	devType := devTypes[A_Index]
	columnX[devType] := GetColX(devType)

	start := starts[devType]
	UpdateWidth(0, 1) ; Reset max width
	
	; Add device entries
	Loop 10 {
		i := start + A_Index
		if (!DeviceList.Has(i)){
			continue
		}
		dev := DeviceList[i]
		rowY := (marginY * 3) + ((A_Index - 1) * rowH)

		chkDevice := monitorGui.Add("Checkbox", "x" columnX[devType] " y" rowY " w" idW, "ID: " dev.id)
		chkDevice.OnEvent("Click", CheckboxChanged.Bind(dev.id))

		lowest := UpdateLowest(chkDevice)
		strings[A_index] := {vid:FormatHex(dev.VID), pid: FormatHex(dev.PID), handle: dev.Handle}

		textVidPid := monitorGui.Add("Text", "x" columnX[devType] + idW " y" rowY - vhOff, "VID / PID:`t0x" strings[A_index].vid ", 0x" strings[A_index].pid)
		maxWidths[devType] := UpdateWidth(textVidPid)

		textHandle := monitorGui.Add("Text", "x" columnX[devType] + idW " y" rowY + vhOff, "Handle:`t`t" StrReplace(strings[A_index].Handle, "&", "&&"))
		maxWidths[devType] := UpdateWidth(textHandle)
	}

	; Add copy buttons
	Loop 10 {
		i := start + A_Index
		if (!DeviceList.Has(i)){
			continue
		}
		dev := DeviceList[i]
		rowY := (marginY * 3) + ((A_Index - 1) * rowH)
		xpos := columnX[devType] + idW + maxWidths[devType]

		btnCopyVidPid := monitorGui.Add("Button", "x" xpos " y" rowY - vhOff " h14 w" copyW, "Copy")
		btnCopyVidPid.OnEvent("Click", CopyClipboard.Bind("0x" strings[A_index].vid ", 0x" strings[A_index].pid))
		
		btnCopyHandle := monitorGui.Add("Button", "x" xpos " y" rowY + vhOff " h14 w" copyW, "Copy")
		btnCopyHandle.OnEvent("Click", CopyClipboard.Bind(strings[A_index].handle))
	}

	totalWidths[devType] := idW + maxWidths[devType] + copyW
	monitorGui.Add("Text", "x" columnX[devType] " y5 w" totalWidths[devType] " Center", columnTitles[devType])
}

lowest += 2 * MarginY

; Options
chkFilterPress := monitorGui.Add("CheckBox", "x" columnX["K"] " y" lowest, "Only show key releases")
chkFilterPress.OnEvent("Click", FilterPress)

chkFilterMove :=  monitorGui.Add("CheckBox", "x" columnX["M"] " w" totalWidths[devType] " yp Checked", "Filter Movement (Warning: Turning off can cause crashes)")
chkFilterMove.OnEvent("Click", FilterMove)

lowest += 2 * MarginY

btnClearKeyboard := monitorGui.Add("Button", "x" columnX["K"] " y" lowest " w" totalWidths["K"] " Center", "Clear")
btnClearKeyboard.OnEvent("Click", ClearKeyboard)

btnClearMouse := monitorGui.Add("Button", "x" columnX["M"] " yp w" totalWidths["M"] " Center", "Clear")
btnClearMouse.OnEvent("Click", ClearMouse)

lowest += 30

; Output
lvKeyboard := monitorGui.Add("ListView", "x" columnX["K"] " y" lowest " w" totalWidths["K"] " h" outputH, ["ID", "Code", "State", "Key Name"])
lvKeyboard.ModifyCol(4, 100)

lvMouse := monitorGui.Add("ListView", "x" columnX["M"] " yp w" totalWidths["M"] " h" outputH, ["ID", "Code", "State", "X", "Y", "Info"])
lvMouse.ModifyCol(6, 200)

lowest += outputH

monitorGui.Show("w" (marginX * 3) + totalWidths["K"] + totalWidths["M"] " h" marginY + lowest)
return


GetColX(devType){
	global marginX, idW, maxWidths, copyW
	if (devType == "K")
		return marginX
	else
		return (marginX * 2) + idW + maxWidths["K"] + copyW
}

UpdateLowest(ctrl){
	static max := 0
	ctrl.GetPos(&cpX, &cpY, &cpW, &cpH)
	pos := cpY + cpH
	if (pos > max){
		max := pos
	}
	return max
}

UpdateWidth(ctrl, reset := 0){
	static max := 0
	if (reset){
		max := 0
		return
	}
	ctrl.GetPos(&cpX, &cpY, &cpW, &cpH)
	if (cpW > max){
		max := cpW
	}
	return max
}

CheckboxChanged(id, ctrl, info){
	global AHI
	if (ctrl.Value){
		if (id < 11){
			AHI.SubscribeKeyboard(id, false, KeyboardEvent.Bind(id))
		} else {
			AHI.SubscribeMouseButtons(id, false, MouseButtonEvent.Bind(id))
			AHI.SubscribeMouseMoveRelative(id, false, MouseAxisEvent.Bind(id, "Relative Move"))
			AHI.SubscribeMouseMoveAbsolute(id, false, MouseAxisEvent.Bind(id, "Absolute Move"))
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
}

FilterMove(ctrl, info){
	global filterMouseMove
	filterMouseMove := ctrl.Value
}

FilterPress(ctrl, info){
	global filterKeyPress
	filterKeyPress := ctrl.Value
}

ClearKeyboard(ctrl, info){
	global lvKeyboard
	lvKeyboard.Delete()
}

ClearMouse(ctrl, info){
	global lvMouse
	lvMouse.Delete()
}

FormatHex(num){
	return Format("{:04X}", num)
}


KeyboardEvent(id, code, state){
	global lvKeyboard, filterKeyPress
    if (filterKeyPress && state)
        return
	scanCode := Format("{:x}", code)
	keyName := GetKeyName("SC" scanCode)
	row := lvKeyboard.Add(, id, code, state, keyName)
	lvKeyboard.Modify(row, "Vis")
}

MouseButtonEvent(id, code, state){
	global lvMouse
	row := lvMouse.Add(, id, code, state, "", "", "Button")
	lvMouse.Modify(row, "Vis")
}

MouseAxisEvent(id, info, x, y){
	global lvMouse, filterMouseMove
	if (filterMouseMove)
		return
	row := lvMouse.Add(, id, "", "", x, y, info)
	lvMouse.Modify(row, "Vis")
}

CopyClipboard(str, ctrl, info){
	A_Clipboard := str
	Tooltip("Copied to Clipboard")
	SetTimer(ClearTooltip, 1000)
}

ClearTooltip(){
	ToolTip
}

GuiClosed(gui){
	ExitApp
}

^Esc::
{
	ExitApp
}
