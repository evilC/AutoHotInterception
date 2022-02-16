/*
This code illustrates how to process drags (Press down pen and move) with a tablet
*/
#SingleInstance force
#Persistent
#include Lib\AutoHotInterception.ahk
Coordmode, Tooltip, Screen

; Initialize AHI
AHI := new AutoHotInterception()
mouseId := AHI.GetMouseId(0x0B57, 0x9091) ; Get VID/PID of your device from the Monitor app and paste it in here
; Subscribe to the "Left Mouse" (AKA pressing the pen on the tablet) event from the tablet
AHI.SubscribeMouseButton(mouseId, 0, true, Func("ProcessInput"))
; Subscribe to movement coming from the tablet
; We use .Bind("") here to pass "" to the newState parameter of the function
AHI.SubscribeMouseMoveAbsolute(mouseId, true, Func("ProcessInput").Bind(""))
return

ProcessInput(newState, nx := "", ny := ""){
	static state := 0
	static x := 0, y := 0
	if (newState != "" && newState != state){
		stateChanged := 1
		state := newState
	} else {
		stateChanged := 0
	}
	if (nx != ""){
		x := nx
		y := ny
	}
	; At this point, regardless of whether the input was a move or click, the following variables tell us what has happened:
	; x and y hold current x and current y
	; state is the current state of the button
    ; stateChanged indicates whether state just changed or not
	
	; ---- Start of your code ----
	static dragStartX, dragStartY
	tooltip % "Current Coords: " x ", " y, 0, 0, 1
	if (stateChanged){
		if (state){
			; Process start of drag here
			dragStartX := x
			dragStartY := y
		} else {
			; Process end of drag here
			tooltip % "NOT DRAGGING. Last drag was: x " x - dragStartX ", y " y - dragStartY, 0, 30, 2
		}
	} else if (state){
		; If you want to do something during a drag, you would do it here
		tooltip % "DRAGGING: dragged x " x - dragStartX ", y " y - dragStartY, 0, 30, 2
	}
}

^Esc::
	ExitApp
