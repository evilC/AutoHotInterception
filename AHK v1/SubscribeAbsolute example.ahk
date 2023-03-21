/*
This code illustrates how to detect movement and mouse event information coming from a Tablet
Bear in mind that when AHI receives input from a tablet...
... the "packet" of information always includes movement AND button state information
When AHI fires the callbacks, it always fires movement callbacks before button callbacks...
... so you can be sure that when ButtonEvent() fires, it always has the latest coordinates
*/
#SingleInstance force
#Persistent ; If you have no hotkeys or GUI in the script, you need this else the script will instantly exit
#include Lib\AutoHotInterception.ahk
Coordmode, Tooltip, Screen

; Initialize AHI
AHI := new AutoHotInterception()
mouseId := AHI.GetMouseId(0x0B57, 0x9091) ; Get VID/PID of your device from the Monitor app and paste it in here
; Subscribe to the "Left Mouse" (AKA pressing the pen on the tablet) event from the tablet
AHI.SubscribeMouseButton(mouseId, 0, true, Func("ButtonEvent"))
; Subscribe to movement coming from the tablet
AHI.SubscribeMouseMoveAbsolute(mouseId, true, Func("OnMouseMove"))
return

ButtonEvent(state){
	global lastX, lastY
	tooltip % "LMB State: " state " ( Event was at " lastX ", " lastY " )", 0, 30, 2
}

; Store coordinates on move, so they can be used in ButtonEvent
OnMouseMove(x, y){
	global lastX, lastY
	lastX := x
	lastY := y
	tooltip % "Coords: " x ", " y, 0, 0, 1
}

^Esc::
	ExitApp
