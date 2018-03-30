#include %A_LineFile%\..\CLR.ahk

class AutoHotInterception {
	__New(cls := "Manager"){
		dllName := "AutoHotInterception.dll"
		dllFile := A_LineFile "\..\" dllName
		hintMessage := "Try right-clicking lib\" dllName ", select Properties, and if there is an 'Unblock' checkbox, tick it`nAlternatively, running Unblocker.ps1 in the lib folder (ideally as admin) can do this for you."
		if (!FileExist(dllFile)){
			MsgBox % "Unable to find " dllName ", exiting..."
			ExitApp
		}
		
		asm := CLR_LoadLibrary(dllFile)
		try {
			this.Interception := asm.CreateInstance("AutoHotInterception." cls)
		}
		catch {
			MsgBox % dllName " failed to load`n`n" hintMessage
			ExitApp
		}
		if (this.Interception.OkCheck() != "OK"){
			MsgBox % dllName " loaded but check failed!`n`n" hintMessage
			ExitApp
		}
	}
	
	GetInstance(){
		return this.Interception
	}
	
	GetDeviceList(){
		DeviceList := {}
		arr := this.Interception.GetDeviceList()
		for v in arr {
			DeviceList[v.id] := { ID: v.id, VID: v.vid, PID: v.pid, IsMouse: v.IsMouse }
		}
		return DeviceList
	}
}