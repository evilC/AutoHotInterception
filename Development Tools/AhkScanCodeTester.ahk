#SingleInstance force

/*
Tool for checking what InputHook reports in AHK
*/

Sleep 100 ; When hitting F5 to run script in SciTE, release of F5 appears in list. Suppress
ih := InputHook()
ih.KeyOpt("{All}", "SN")
ih.OnKeyDown := Func("AhkKeyEvent").Bind(1)
ih.OnKeyUp := Func("AhkKeyEvent").Bind(0)
ih.Start()

Gui, Add, ListView, h200, Key Name|SC|State
LV_ModifyCol(1, 100)
LV_ModifyCol(2, 50)
LV_ModifyCol(3, 50)
Gui, Show,, AHK ScanCode tester
return

AhkKeyEvent(state, ih, vk, sc){
	row := LV_Add(, GetKeyName("SC" DecToHex(sc)), sc, state)
	LV_Modify(row, "Vis")
}

DecToHex(val){
	return Format("{:X}", val)
}

GuiClose:
	ExitApp