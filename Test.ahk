#SingleInstance force
;~ #Persistent
#include Lib\AutoHotInterception.ahk

;~ pk := GetKeySC("Pause")
;~ dk := GetKeySC("Delete")
;~ RCtrl := GetKeySC("RCtrl")
msgbox % GetKeySC("ScrollLock")
;~ return
;~ msgbox % Format("{:d}", GetKeySC("Pause"))
return

clipboard := ""
log := "`n// ============ DUPES ==================`n"
keys := {}

Loop 512 {
	hex := Format("{:x}", A_Index)
	name := GetKeyName("sc" hex)
	if (name == "")
		continue
	str := "{" A_Index " /*(0x" hex ")*/, " """" name """" "}"
	;~ if (A_Index == 86 || A_Index = 342)
		;~ break = true
	if (keys.HasKey(name)){
		log .= "// " str " (Also " keys[name] ")`n"
	} else {
		clipboard .= str ",`n"
		keys[name] := A_Index
	}
}
clipboard .= log
return
state := true
AHI := new AutoHotInterception()

;~ keyboardId := AHI.GetKeyboardId(0x04F2, 0x0112)
;~ AHI.SubscribeKey(keyboardId, GetKeySC("1"), true, Func("KeyEvent"))
mouseId := AHI.GetMouseId(0x046D, 0xC00C)
AHI.SubscribeMouseMove(mouseId, true, Func("OnMouseMove"))
return

KeyEvent(state){
	ToolTip % "State: " state
}

OnMouseMove(x, y){
	Tooltip % x ", " y, 0, 0
}

F1::
	state := !state
	AHI.SetState(state)
	Tooltip
	return

^Esc::
	ExitApp