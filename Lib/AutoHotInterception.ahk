#include %A_LineFile%\..\CLR.ahk

class AutoHotInterception {
	_contextManagers := {}

	__New(cls := "Manager") {
		bitness := A_PtrSize == 8 ? "x64" : "x86"
		dllName := "interception.dll"
		dllFile := A_LineFile "\..\" bitness "\" dllName
		if (!FileExist(dllFile)) {
			MsgBox % "Unable to find lib\" bitness "\" dllName ", exiting...`nYou should extract both x86 and x64 folders from the library folder in interception.zip into AHI's lib folder."
			ExitApp
		}

		hModule := DllCall("LoadLibrary", "Str", dllFile, "Ptr")
		if (hModule == 0) {
			this_bitness := A_PtrSize == 8 ? "64-bit" : "32-bit"
			other_bitness := A_PtrSize == 4 ? "64-bit" : "32-bit"
			MsgBox % "Bitness of " dllName " does not match bitness of AHK.`nAHK is " this_bitness ", but " dllName " is " other_bitness "."
			ExitApp
		}
		DllCall("FreeLibrary", "Ptr", hModule)

		dllName := "AutoHotInterception.dll"
		dllFile := A_LineFile "\..\" dllName
		hintMessage := "Try right-clicking lib\" dllName ", select Properties, and if there is an 'Unblock' checkbox, tick it`nAlternatively, running Unblocker.ps1 in the lib folder (ideally as admin) can do this for you."
		if (!FileExist(dllFile)) {
			MsgBox % "Unable to find lib\" dllName ", exiting..."
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
		if (this.Instance.OkCheck() != "OK") {
			MsgBox % dllName " loaded but check failed!`n`n" hintMessage
			ExitApp
		}
	}

	GetInstance() {
		return this.Instance
	}

	; --------------- Input Synthesis ----------------
	SendKeyEvent(id, code, state) {
		this.Instance.SendKeyEvent(id, code, state)
	}

	SendMouseButtonEvent(id, btn, state) {
		this.Instance.SendMouseButtonEvent(id, btn, state)
	}

	SendMouseButtonEventAbsolute(id, btn, state, x, y) {
		this.Instance.SendMouseButtonEventAbsolute(id, btn, state, x, y)
	}

	SendMouseMove(id, x, y) {
		this.Instance.SendMouseMove(id, x, y)
	}

	SendMouseMoveRelative(id, x, y) {
		this.Instance.SendMouseMoveRelative(id, x, y)
	}

	SendMouseMoveAbsolute(id, x, y) {
		this.Instance.SendMouseMoveAbsolute(id, x, y)
	}
	*/
	; --------------- Querying ------------------------
	GetDeviceId(IsMouse, VID, PID, instance := 1) {
		static devType := {0: "Keyboard", 1: "Mouse"}
		dev := this.Instance.GetDeviceId(IsMouse, VID, PID, instance)
		if (dev == 0) {
			MsgBox % "Could not get " devType[isMouse] " with VID " VID ", PID " PID ", Instance " instance
			ExitApp
		}
		return dev
	}

	GetDeviceIdFromHandle(isMouse, handle, instance := 1) {
		static devType := {0: "Keyboard", 1: "Mouse"}
		dev := this.Instance.GetDeviceIdFromHandle(IsMouse, handle, instance)
		if (dev == 0) {
			MsgBox % "Could not get " devType[isMouse] " with Handle " handle ", Instance " instance
			ExitApp
		}
		return dev
	}

	GetKeyboardId(VID, PID, instance := 1) {
		return this.GetDeviceId(false, VID, PID, instance)
	}

	GetMouseId(VID, PID, instance := 1) {
		return this.GetDeviceId(true, VID, PID, instance)
	}

	GetKeyboardIdFromHandle(handle, instance := 1) {
		return this.GetDeviceIdFromHandle(false, handle, instance)
	}

	GetMouseIdFromHandle(handle, instance := 1) {
		return this.GetDeviceIdFromHandle(true, handle, instance)
	}

	GetDeviceList() {
		DeviceList := {}
		arr := this.Instance.GetDeviceList()
		for v in arr {
			DeviceList[v.id] := { ID: v.id, VID: v.vid, PID: v.pid, IsMouse: v.IsMouse, Handle: v.Handle }
		}
		return DeviceList
	}

	; ---------------------- Subscription Mode ----------------------
	SubscribeKey(id, code, block, callback, concurrent := false) {
		this.Instance.SubscribeKey(id, code, block, callback, concurrent)
	}

	SubscribeMouseButton(id, btn, block, callback, concurrent := false) {
		this.Instance.SubscribeMouseButton(id, btn, block, callback, concurrent)
	}

	SubscribeMouseMove(id, block, callback, concurrent := false) {
		this.Instance.SubscribeMouseMove(id, block, callback, concurrent)
	}

	SubscribeMouseMoveRelative(id, block, callback, concurrent := false) {
		this.Instance.SubscribeMouseMoveRelative(id, block, callback, concurrent)
	}

	SubscribeMouseMoveAbsolute(id, block, callback, concurrent := false) {
		this.Instance.SubscribeMouseMoveAbsolute(id, block, callback, concurrent)
	}

	; ------------- Context Mode ----------------
	; Creates a context class to make it easy to turn on/off the hotkeys
	CreateContextManager(id) {
		if (this._contextManagers.ContainsKey(id)) {
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
		__New(parent, id) {
			this.parent := parent
			this.id := id
			result := this.parent.Instance.SetContextCallback(id, this.OnContextCallback.Bind(this))
		}
		
		OnContextCallback(state) {
			Sleep 0
			this.IsActive := state
		}
	}
}