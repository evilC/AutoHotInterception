#SingleInstance force
#Persistent
#include Lib\AutoHotInterception.ahk

VID := 0x04F2, PID := 0x0112

InterceptionWrapper := new AutoHotInterception()
global Interception := InterceptionWrapper.GetInstance()

devices := InterceptionWrapper.GetDeviceList()
if (!devices.Length()){
	msgbox Device List Check failed
	ExitApp
}

keyboardId := Interception.GetDeviceId(false, VID, PID)

result := Interception.SubscribeKey(keyboardId, GetKeySC("1"), true, Func("KeyEvent"))

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