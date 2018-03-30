#SingleInstance force
#Persistent
#include Lib\AutoHotInterception.ahk

AHI := new AutoHotInterception()
id1 := AHI.GetKeyboardId(0x04F2, 0x0112, 1)
cm1 := AHI.CreateContextManager(id1)
return

#if cm1.IsActive
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