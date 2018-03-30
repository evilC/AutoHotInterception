#include %A_LineFile%\..\CLR.ahk

class AutoHotInterception {
	_contextManagers := {}
	;_contextStates := {}
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
			this.Instance := asm.CreateInstance("AutoHotInterception." cls)
		}
		catch {
			MsgBox % dllName " failed to load`n`n" hintMessage
			ExitApp
		}
		if (this.Instance.OkCheck() != "OK"){
			MsgBox % dllName " loaded but check failed!`n`n" hintMessage
			ExitApp
		}
	}
	
	; --------------- Input Synthesis ----------------
	SendKeyEvent(id, code, state){
		this.Instance.SendKeyEvent(id, code, state)
	}
	
	SendMouseButtonEvent(id, btn, state){
		this.Instance.SendMouseButtonEvent(id, btn, state)
	}
	
	SendMouseMove(id, x, y){
		this.Instance.SendMouseMove(id, x, y)
	}
	
	SendMouseMoveRelative(id, x, y){
		this.Instance.SendMouseMoveRelative(id, x, y)
	}
	
	SendMouseMoveAbsolute(id, x, y){
		this.Instance.SendMouseMoveAbsolute(id, x, y)
	}
	*/
	; --------------- Querying ------------------------
	GetDeviceID(IsMouse, VID, PID, instance := 1){
		static devType := {0: "Keyboard", 1: "Mouse"}
		dev := this.Instance.GetDeviceId(IsMouse, VID, PID)
		if (dev == 0){
			MsgBox % "Could not get " devType[isMouse] " with VID " VID ", PID " PID ", Instance " instance
			ExitApp
		}
		return dev
	}
	
	GetKeyboardID(VID, PID, instance := 1){
		return this.GetDeviceId(false, VID, PID)
	}
	
	GetMouseID(VID, PID, instance := 1){
		return this.GetDeviceId(true, VID, PID)
	}
	
	GetDeviceList(){
		DeviceList := {}
		arr := this.Instance.GetDeviceList()
		for v in arr {
			DeviceList[v.id] := { ID: v.id, VID: v.vid, PID: v.pid, IsMouse: v.IsMouse }
		}
		return DeviceList
	}
	
	; ---------------------- Subscription Mode ----------------------
	SubscribeKey(id, code, block, callback){
		this.Instance.SubscribeKey(id, code, block, callback)
	}
	
	SubscribeMouseButton(id, btn, block, callback){
		this.Instance.SubscribeMouseButton(id, btn, block, callback)
	}
	
	SubscribeMouseMove(id, block, callback){
		this.Instance.SubscribeMouseMove(id, block, callback)
	}
	
	SubscribeMouseMoveRelative(id, block, callback){
		this.Instance.SubscribeMouseMoveRelative(id, block, callback)
	}
	
	SubscribeMouseMoveAbsolute(id, block, callback){
		this.Instance.SubscribeMouseMoveAbsolute(id, block, callback)
	}
	
	; ------------- Context Mode ----------------
	; Creates a context class to make it easy to turn on/off the hotkeys
	CreateContextManager(id){
		if (this._contextManagers.ContainsKey(id)){
			Msgbox % "ID " id " already has a Context Manager"
			ExitApp
		}
		cm := new this.ContextManager(this, id)
		this._contextManagers[id] := cm
		return cm
	}

	; Helper class for dealing with context mode
	class ContextManager {
		IsActive := 0
		__New(parent, id){
			this.parent := parent
			this.id := id
			
			result := this.parent.Instance.SetContextCallback(id, this.OnContextCallback.Bind(this))
		}
		
		OnContextCallback(state){
			Sleep 0
			this.IsActive := state
		}
	}
}