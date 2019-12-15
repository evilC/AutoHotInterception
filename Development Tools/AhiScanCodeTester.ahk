#SingleInstance force
#Include ..\Lib\CLR.ahk

; REQUIRES AHK >= 1.1.32

/*
The purpose of this tool is to compare the keyboard events that AHK sees to the keyboard events that AHI sees  
To use it, set the vid and pid variables below to the VID and PID of a keyboard...
... then run this script and press keys ONLY ON THAT KEYBOARD
Pressing keys on another keyboard will break the script!

The ListView in the GUI will then be updated to show the events seen by AHK and AHI
Note that for some keys (eg Pause), AHI will see TWO key events for that key, hence the "AHI SC #1" and "AHI SC #2" columns

All ScanCodes are in Decimal

*/

vid := 0x04F2, pid := 0x0112 ; Wyse Keyboard

OutputDebug, DBGVIEWCLEAR
AhkKeyBuffer := []

asm := CLR_LoadLibrary("..\Lib\AutoHotInterception.dll")
sct := asm.CreateInstance("AutoHotInterception.ScanCodeChecker")
sct.Subscribe(vid, pid, Func("AhiKeyEvent"))

ih := InputHook()
ih.KeyOpt("{All}", "SN")
ih.OnKeyDown := Func("AhkKeyEvent").Bind(1)
ih.OnKeyUp := Func("AhkKeyEvent").Bind(0)
ih.Start()

Gui, Add, ListView, w600 h500, Key Name|AHK SC|AHK State|AHI SC #1|AHI State #1|AHI SC #2|AHI State #2
LV_ModifyCol(1, 100)
LV_ModifyCol(2, 80)
LV_ModifyCol(3, 80)
LV_ModifyCol(4, 80)
LV_ModifyCol(5, 80)
LV_ModifyCol(6, 80)
LV_ModifyCol(7, 80)
Gui, Add, Button, xm w600 gClear Centered, Clear
Gui, Show,, AHI ScanCode tester
return

Clear:
	LV_Delete()
	return

AhiKeyEvent(keyEvents){
	global AhkKeyBuffer
	ahkKeyEvent := AhkKeyBuffer[1]
	; Fix for NumLock InputHook issue: https://www.autohotkey.com/boards/viewtopic.php?f=14&t=70769
	if (ahkKeyEvent.Code == 325 && ahkKeyEvent.State == 1 && AhkKeyBuffer.Length() > 1){
		AhkKeyBuffer := [AhkKeyBuffer[1]]
	}
	; End fix
	if (AhkKeyBuffer.Length() != 1){
		msgbox % "Expecting exactly 1 event in AHK Key Buffer, found " AhkKeyBuffer.Length()
		ExitApp
	}
	numEvents := keyEvents.MaxIndex() + 1
	if (numEvents > 2){
		msgbox % "Expecting 1 or 2 AHI key events, but got " numEvents
		ExitApp
	}
	
	; Note that keyEvents is a ZERO-BASED array!
	
	ahiSc1 := keyEvents[0].Code
	ahiState1 := keyEvents[0].state
	
	if (numEvents == 2){
		ahiSc2 := keyEvents[1].Code
		ahiState2 := keyEvents[1].state
	}
	
	ahkSc1 := ahkKeyEvent.Code
	if (ahkSc1 > 256){
		ahkSc1 .= " (Ext " ahkSc1 - 256 ")"
	}
	ahkState1 := ahkKeyEvent.State
	
	row := LV_Add(, GetKeyName("SC" DecToHex(ahkKeyEvent.Code)), ahkSc1, ahkState1, ahiSc1, ahiState1, ahiSc2, ahiState2)
	LV_Modify(row, "Vis")
	
	AhkKeyBuffer := []
}

AhkKeyEvent(state, ih, vk, sc){
	global AhkKeyBuffer
	AhkKeyBuffer.Push({Code: sc, State: state})
}

Debug(str){
	OutputDebug % "AHK| " str
}

DecToHex(val){
	return Format("{:X}", val)
}

^Esc::
GuiClose:
	ExitApp