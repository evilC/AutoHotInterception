; ==========================================================
;                  .NET Framework Interop
;   https://www.autohotkey.com/boards/viewtopic.php?t=4633
; ==========================================================
;
;   Author:     Lexikos
;   Version:    2.0
;   Requires:	AutoHotkey v2.0-beta.1
;

CLR_LoadLibrary(AssemblyName, AppDomain:=0)
{
	if !AppDomain
		AppDomain := CLR_GetDefaultDomain()
	try
		return AppDomain.Load_2(AssemblyName)
	static null := ComValue(13,0)
	args := ComObjArray(0xC, 1),  args[0] := AssemblyName
	typeofAssembly := AppDomain.GetType().Assembly.GetType()
	try
		return typeofAssembly.InvokeMember_3("LoadWithPartialName", 0x158, null, null, args)
	catch
		return typeofAssembly.InvokeMember_3("LoadFrom", 0x158, null, null, args)
}

CLR_CreateObject(Assembly, TypeName, Args*)
{
	if !(argCount := Args.Length)
		return Assembly.CreateInstance_2(TypeName, true)
	
	vargs := ComObjArray(0xC, argCount)
	Loop argCount
		vargs[A_Index-1] := Args[A_Index]
	
	static Array_Empty := ComObjArray(0xC,0), null := ComValue(13,0)
	
	return Assembly.CreateInstance_3(TypeName, true, 0, null, vargs, null, Array_Empty)
}

CLR_CompileCS(Code, References:="", AppDomain:=0, FileName:="", CompilerOptions:="")
{
	return CLR_CompileAssembly(Code, References, "System", "Microsoft.CSharp.CSharpCodeProvider", AppDomain, FileName, CompilerOptions)
}

CLR_CompileVB(Code, References:="", AppDomain:=0, FileName:="", CompilerOptions:="")
{
	return CLR_CompileAssembly(Code, References, "System", "Microsoft.VisualBasic.VBCodeProvider", AppDomain, FileName, CompilerOptions)
}

CLR_StartDomain(&AppDomain, BaseDirectory:="")
{
	static null := ComValue(13,0)
	args := ComObjArray(0xC, 5), args[0] := "", args[2] := BaseDirectory, args[4] := ComValue(0xB,false)
	AppDomain := CLR_GetDefaultDomain().GetType().InvokeMember_3("CreateDomain", 0x158, null, null, args)
}

; ICorRuntimeHost::UnloadDomain
CLR_StopDomain(AppDomain) => ComCall(20, CLR_Start(), "ptr", ComObjValue(AppDomain))

; NOTE: IT IS NOT NECESSARY TO CALL THIS FUNCTION unless you need to load a specific version.
CLR_Start(Version:="") ; returns ICorRuntimeHost*
{
	static RtHst := 0
	; The simple method gives no control over versioning, and seems to load .NET v2 even when v4 is present:
	; return RtHst ? RtHst : (RtHst:=COM_CreateObject("CLRMetaData.CorRuntimeHost","{CB2F6722-AB3A-11D2-9C40-00C04FA30A3E}"), DllCall(NumGet(NumGet(RtHst+0)+40),"uint",RtHst))
	if RtHst
		return RtHst
	if Version = ""
		Loop Files EnvGet("SystemRoot") "\Microsoft.NET\Framework" (A_PtrSize=8?"64":"") "\*","D"
			if (FileExist(A_LoopFilePath "\mscorlib.dll") && StrCompare(A_LoopFileName, Version) > 0)
				Version := A_LoopFileName
	static CLSID_CorRuntimeHost := CLR_GUID("{CB2F6723-AB3A-11D2-9C40-00C04FA30A3E}")
	static IID_ICorRuntimeHost  := CLR_GUID("{CB2F6722-AB3A-11D2-9C40-00C04FA30A3E}")
	DllCall("mscoree\CorBindToRuntimeEx", "wstr", Version, "ptr", 0, "uint", 0
		, "ptr", CLSID_CorRuntimeHost, "ptr", IID_ICorRuntimeHost
		, "ptr*", &RtHst:=0, "hresult")
	ComCall(10, RtHst) ; Start
	return RtHst
}

;
; INTERNAL FUNCTIONS
;

CLR_GetDefaultDomain()
{
	; ICorRuntimeHost::GetDefaultDomain
	static defaultDomain := (
		ComCall(13, CLR_Start(), "ptr*", &p:=0),
		ComObjFromPtr(p)
	)
	return defaultDomain
}

CLR_CompileAssembly(Code, References, ProviderAssembly, ProviderType, AppDomain:=0, FileName:="", CompilerOptions:="")
{
	if !AppDomain
		AppDomain := CLR_GetDefaultDomain()
	
	asmProvider := CLR_LoadLibrary(ProviderAssembly, AppDomain)
	codeProvider := asmProvider.CreateInstance(ProviderType)
	codeCompiler := codeProvider.CreateCompiler()

	asmSystem := (ProviderAssembly="System") ? asmProvider : CLR_LoadLibrary("System", AppDomain)

	; Convert | delimited list of references into an array.
	Refs := References is String ? StrSplit(References, "|", " `t") : References
	aRefs := ComObjArray(8, Refs.Length)
	Loop Refs.Length
		aRefs[A_Index-1] := Refs[A_Index]
	
	; Set parameters for compiler.
	prms := CLR_CreateObject(asmSystem, "System.CodeDom.Compiler.CompilerParameters", aRefs)
	, prms.OutputAssembly          := FileName
	, prms.GenerateInMemory        := FileName=""
	, prms.GenerateExecutable      := SubStr(FileName,-4)=".exe"
	, prms.CompilerOptions         := CompilerOptions
	, prms.IncludeDebugInformation := true
	
	; Compile!
	compilerRes := codeCompiler.CompileAssemblyFromSource(prms, Code)
	
	if error_count := (errors := compilerRes.Errors).Count
	{
		error_text := ""
		Loop error_count
			error_text .= ((e := errors.Item[A_Index-1]).IsWarning ? "Warning " : "Error ") . e.ErrorNumber " on line " e.Line ": " e.ErrorText "`n`n"
		throw Error("Compilation failed",, "`n" error_text)
	}
	; Success. Return Assembly object or path.
	return FileName="" ? compilerRes.CompiledAssembly : compilerRes.PathToAssembly
}

; Usage 1: pGUID := CLR_GUID(&GUID, "{...}")
; Usage 2: GUID := CLR_GUID("{...}"), pGUID := GUID.Ptr
CLR_GUID(a, b:=unset)
{
	DllCall("ole32\IIDFromString"
		, "wstr", sGUID := IsSet(b) ? b : a
		, "ptr", GUID := Buffer(16,0), "hresult")
	return IsSet(b) ? GUID.Ptr : GUID
}
