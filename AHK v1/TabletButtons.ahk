/*
For use with an Absolute Mode pointing device (eg a Wacom graphics tablet)
Turns a Absolute Mode device into a button box

To Use:
1) Use the TabletButtonBuilder.ahk script to build a TabletButtons.ini file containing names and coordinates for your buttons
2) Find the VID/PID of your tablet using the Monitor.ahk demo and paste it where indicated below
3) Run this script
*/

#SingleInstance force
#Persistent ; If you have no hotkeys or GUI in the script, you need this else the script will instantly exit
#include Lib\AutoHotInterception.ahk
#include TabletLib\TabletLib.ahk

; Initialize AHI
AHI := new AutoHotInterception()
mouseId := AHI.GetMouseId(0x0B57, 0x9091) ; Get VID/PID of your device from the Monitor app and paste it in here
AHI.SubscribeMouseButton(mouseId, 0, true, Func("ButtonEvent"))
AHI.SubscribeMouseMoveAbsolute(mouseId, true, Func("OnMouseMove"))

boxArr := LoadBoxes("TabletButtons.ini")
return

ButtonEvent(state){
	global lastX, lastY, boxArr
	if (state){ ; On button press...
		; Find name of box that was clicked (If any)
		name := FindBoxName(lastX, lastY, boxArr)
		if (name != ""){
			; Your code here to decide what action to take depending on which box was selected
			Tooltip % "You selected box " name
		}
	}
}

; Store coordinates on move, so they can be used in ButtonEvent
OnMouseMove(x, y){
	global lastX, lastY
	lastX := x
	lastY := y
}

^Esc::
	ExitApp
