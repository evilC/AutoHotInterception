#Include Lib\AutoHotInterception.ahk

AHI := AutoHotInterception()
enabled := true
; Wooting Two: HID\VID_03EB&PID_FF02&REV_0090&MI_03
; G604: 0x046D, 0xC539
; keyboardId := AHI.GetKeyboardIdFromHandle("HID\VID_03EB&PID_FF02&REV_0090&MI_03")
;mouseId := AHI.GetMouseId(0x046D, 0xC539)
keyboardId := AHI.GetKeyboardId(0x04F2, 0x0112)

; AHI.SubscribeKey(keyboardId, GetKeySC("a"), false, KbSubscribeTest)
; AHI.SubscribeKeyboard(keyboardId, false, KbSubscribeAllTest)
; l := AHI.GetDeviceList()
; AHI.SubscribeMouseButton(mouseId, 1, false, MBSubscribeTest)
; AHI.SubscribeMouseMove(mouseId, false, MASubscribeTest)

cm1 := AHI.CreateContextManager(keyboardId)
return

KbSubscribeTest(state){
    global AHI, keyboardId
    ToolTip("KBSubscribe`nState: " state)
    AHI.SendKeyEvent(keyboardId, GetKeySC("b"), state)
}

KbSubscribeAllTest(code, state){
    ToolTip("KbSubscribeAll`ncode: " code "`nstate: " state)
}

MBSubscribeTest(state){
    ToolTip("MBSubscribe`nstate: " state)
}

MASubscribeTest(x, y){
    ToolTip("MASubscribe`nx: " x "`ny: " y)
}

x::
{
    global enabled
    enabled := !enabled
    AHI.SetState(enabled)
}

#HotIf cm1.IsActive
::aaa::JACKPOT
1::
{
	ToolTip("KEY DOWN EVENT @ " A_TickCount)
	return
}

1 up::
{
	ToolTip("KEY UP EVENT @ " A_TickCount)
	return
}
#HotIf


^Esc::ExitApp