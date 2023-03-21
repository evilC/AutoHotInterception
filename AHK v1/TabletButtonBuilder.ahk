/*
Script to build INI file with database of button names / coordinates for TabletButtons.ahk to read
For use with an Absolute Mode pointing device (eg a Wacom graphics tablet)

Usage:
1) Find the VID/PID of your tablet using the Monitor.ahk demo and paste it where indicated below
2) Mark up your tablet device with some buttons (eg tape a piece of paper to it, draw some boxes on it with names)
3) Run this script
4) Click "Create Box", give it a name, draw a line from top left to bottom right of one of the boxes
5) Repeat (4) as needed
6) Click "Save" at the bottom of the GUI to savce button coordinates to the INI file
7) Close the GUI of this script, edit TabletButtons.ahk as appropriate and run it
*/
#SingleInstance force
#include Lib\AutoHotInterception.ahk
#include TabletLib\TabletLib.ahk

; Initialize AHI
AHI := new AutoHotInterception()
mouseId := AHI.GetMouseId(0x0B57, 0x9091) ; Get VID/PID of your device from the Monitor app and paste it in here
AHI.SubscribeMouseButton(mouseId, 0, true, Func("ButtonEvent"))
AHI.SubscribeMouseMoveAbsolute(mouseId, true, Func("OnMouseMove"))
AHI.SetState(false)

; Build GUI
guiW := 400
Menu, BoxMenu, Add, Delete, DeleteBoxClicked
Menu, BoxMenu, Add, Edit, EditBoxClicked
Gui, Add, Button, % "w" guiW " Center gCreateBox", Create Box
Gui, Add, Text, % "w" guiW " Center gCreateBox", Defined Boxes (Right Click to edit)
Gui, Add, ListView, % "w" guiW " h400", Name|Start X|Start Y|End X|End Y
LV_ModifyCol(1, 200)
LV_ModifyCol(2, 45)
LV_ModifyCol(3, 45)
LV_ModifyCol(4, 45)
LV_ModifyCol(5, 45)
Gui, Add, Button, % "w" guiW " Center gSave", Save to disk
Gui, Show, , Tablet button builder tool

boxArr := {}

; Load data from existing INI file
bA := LoadBoxes("TabletButtons.ini")
for name, box in bA {
	AddBox(box)
}
return

ButtonEvent(state){
	global boxMode, newBox
	if (state && boxMode == 1){
		boxMode := 2	; Waiting for release
	} else if (!state && boxMode == 2){
		GoSub, EndBox
	}
}

OnMouseMove(x, y){
	global boxMode, newBox
	if (boxMode == 1){
		newBox.StartX := x
		newBox.StartY := y
	} else if (boxMode == 2) {
		newBox.EndX := x
		newBox.EndY := y
	}
}

CreateBox:
	InputBox, boxName, Create Box, Enter box name, , , 120
	if (ERRORLEVEL = 0){
		if (boxArr.HasKey(boxName)){
			msgbox % "Box " boxName " already exists"
		} else {
			StartCreateBox(new Box(boxName))
		}
	}
	return

StartCreateBox(box){
	global AHI, boxMode, newBox
	boxMode := 1 ; waiting for press
	AHI.SetState(true)
	newBox := box
}

EndBox:
	AHI.SetState(false)
	AddBox(newBox)
	boxMode := 0
	return

GuiContextMenu: ; This is automatically called when the GUI is rightclicked
	If (A_EventInfo) { ; This will be the row number selected when right-clicked, if right clicking outside the list view will be 0 (False)
		rightClickedRow := A_EventInfo ; Save the selected row
		Menu, BoxMenu, Show ; Show the Rightclick menu
	}
	Return

DeleteBoxClicked(){
	global rightClickedRow
	DeleteBox(rightClickedRow)
}

EditBoxClicked(){
	global rightClickedRow, boxArr
	newBox := boxArr[GetBoxName(rightClickedRow)].Clone()
	DeleteBox(rightClickedRow)
	StartCreateBox(newBox)
}

Save(){
	global JSON, boxArr
	FileDelete, TabletButtons.ini
	FileAppend, % JSON.Dump(boxArr, ,true), TabletButtons.ini
}

GuiClose:
	ExitApp

AddBox(box){
	global boxArr
	; ToDo: Validate if new box overlaps with any existing boxes
	LV_Add(, box.BoxName, box.StartX, box.StartY, box.EndX, box.EndY)
	boxArr[box.BoxName] := box.Clone()
	LV_ModifyCol(1, "Sort")
}

DeleteBox(rowNum){
	global boxArr
	boxName := GetBoxName(rowNum)
	LV_Delete(rowNum)
	boxArr.Delete(boxName)
}

GetBoxName(rowNum){
	LV_GetText(boxName, rowNum, 1)
	return boxName
}