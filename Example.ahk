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

clipboard := devices
MsgBox % devices

; Param 1: Scan Code for key
; Param 2: Block (true/false)
; Param 3: Callback
result := Interception.SubscribeKey(GetKeySC("1"), true, Func("KeyEvent"), VID, PID)
if (result != -1){
	msgbox Subscribe failed
	ExitApp
}
return

KeyEvent(state){
	ToolTip % "State: " state
}

^Esc::
	ExitApp