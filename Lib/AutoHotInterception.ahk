#include %A_LineFile%\..\CLR.ahk

class AutoHotInterception {
	__New(cls := "Manager"){
		dllFile := A_LineFile "\..\" "AutoHotInterception.dll"
		if (!FileExist(dllFile)){
			MsgBox % "Unable to find " dllFile ", exiting..."
			ExitApp
		}
		
		asm := CLR_LoadLibrary(dllFile)
		try {
			this.Interception := asm.CreateInstance("AutoHotInterception." cls)
		}
		catch {
			MsgBox Interception failed to load
			ExitApp
		}
		if (this.Interception.OkCheck() != "OK"){
			MsgBox Interception Test failed
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