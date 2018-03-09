#include %A_LineFile%\..\CLR.ahk

AutoHotInterception_Init(){
	dllFile := A_LineFile "\..\" "interception.dll"
	if (!FileExist(dllFile)){
		MsgBox % "Unable to find " dllFile ", exiting..."
		ExitApp
	}
	
	asm := CLR_LoadLibrary(dllFile)
	try {
		Interception := asm.CreateInstance("InterceptionWrapper")
	}
	catch {
		MsgBox Interception failed to load
		ExitApp
	}
	if (Interception.Test() != "OK"){
		MsgBox Interception Test failed
		ExitApp
	}
	return Interception
}
