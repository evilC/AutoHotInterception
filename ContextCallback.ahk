#SingleInstance force
#Persistent
#include Lib\AutoHotInterception.ahk

VID := 0x04F2, PID := 0x0112

global Interception := AutoHotInterception_Init()

devices := Interception.GetDeviceList()
if (!devices){
	msgbox Device List Check failed
	ExitApp
}

;~ clipboard := devices
;~ MsgBox % devices

result := Interception.SetContextCallback(VID, PID, Func("SetKb1Context"))
if (result != -1){
	msgbox Subscribe failed
	ExitApp
}
return

SetKb1Context(state){
	global isKeyboard1Active
	Sleep 0
	isKeyboard1Active := state
}
#if isKeyboard1Active
::aaa::JACKPOT
1:: 
	ToolTip % "KEY DOWN EVENT @ " A_TickCount
	return
	
1 up::
	ToolTip % "KEY UP EVENT @ " A_TickCount
	return
#if

^Esc::
	ExitApp