#include %A_LineFile%\..\CLR.ahk

class AutoHotInterception {
	__New(cls := "Manager"){
		dllName := "AutoHotInterception.dll"
		dllFile := A_LineFile "\..\" dllName
		if (!FileExist(dllFile)){
			MsgBox % "Unable to find " dllFile ", exiting..."
			ExitApp
		}
		
		asm := CLR_LoadLibrary(dllFile)
		try {
			this.Interception := asm.CreateInstance("AutoHotInterception." cls)
		}
		catch {
			MsgBox % dllName " failed to load"
			ExitApp
		}
		if (this.Interception.OkCheck() != "OK"){
			MsgBox % dllName " loaded but check failed!`nTry right-clicking lib\" dllName ", select Properties, and if there is an 'Unblock' checkbox, tick it"
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