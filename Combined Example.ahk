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
if (keyboardId == 0){
	MsgBox % "Could not find keyboard with VID " VID " PID " PID
	ExitApp
}

result := Interception.SubscribeKey(keyboardId, GetKeySC("2"), true, Func("KeyEvent"))
if (result != -1){
	msgbox Subscribe failed
	ExitApp
}

result := Interception.SetContextCallback(keyboardId, Func("SetKb1Context"))
if (result != -1){
	msgbox Context request failed
	ExitApp
}
return

KeyEvent(state){
	static ctrlCode := GetKeySC("Ctrl")
	global keyboardId
	;~ Interception.SendKeyEvent(keyboardId, ctrlCode, state)
	ToolTip % "State: " state
}

SetKb1Context(state){
	global isKeyboard1Active
	Sleep 0		; We seem to need this for hotstrings to work, not sure why
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