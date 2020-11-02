#SingleInstance force
#Persistent
#include Lib\AutoHotInterception.ahk

rm := new RollMouse(0x046D, 0xC531)
return

class RollMouse {
	; User configurable items
	; The speed at which you must move the mouse to be able to trigger a roll
	MoveThreshold := {x: 4, y: 4}
	; Good value for my mouse with FPS games: 4
	; Good value for my laptop trackpad: 3
	
	; The speed at which to move the mouse, can be decimals (eg 0.5)
	; X and Y do not need to be equal
	; Good value for my mouse with FPS games: x:2, y: 1 (don't need vertical roll so much)
	;~ MoveFactor := {x: 1, y: 1}
	MoveFactor := {x: 0.5, y: 0.25}
	; Good value for my laptop trackpad: 0.2
	
	; How fast (in ms) to send moves when rolling.
	; High values for this will cause rolls to appear jerky instead of smooth
	; if you halved this, double MoveFactor to get the same move amount, but at a faster frequency.
	RollFreq := 1
	
	; How long to wait after each move to decide whether a roll has taken place.
	TimeOutRate := 50
	
	; The amount that we are currently rolling by
	LastMove := {x: 0, y: 0}

	; The number of previous moves stored - used to calculate vector of a roll
	; Higher numbers = greater fidelity, but more CPU
	MOVE_BUFFER_SIZE := 5

	; Non user-configurable items
	STATE_UNDER_THRESH := 1
	STATE_OVER_THRESH := 2
	STATE_ROLLING := 3
	StateNames := ["UNDER THRESHOLD", "OVER THRESHOLD", "ROLLING"]
	
	State := 1
	
	TimeOutFunc := 0
	History := {}	; Movement history. The most recent item is first (Index 1), and old (high index) items get pruned off the end

	; Was an option in old RollMouse
	Friction := 0
	
	__New(vid := "", pid := ""){
		this.TimeOutFunc := this.DoRoll.Bind(this)
		this.AHI := new AutoHotInterception()
		if (vid == "" && pid == ""){
			this.mouseId := 11
		} else {
			this.mouseId := this.AHI.GetMouseId(vid, pid)
		}
		this.AHI.SubscribeMouseMove(this.mouseId, false, this.MouseMove.Bind(this))
	}
	
	MouseMove(x, y){
		static axes := {x: 1, y: 2}
		;~ ToolTip % x ", " y
		moved := {x: 0, y: 0}
		
		for axis, index in axes {
			obj := {}
			obj.delta_move := %axis%
			obj.abs_delta_move := abs(obj.delta_move)
			obj.sgn_move := (obj.abs_delta_move = obj.delta_move) ? 1 : -1

			if (obj.abs_delta_move >= this.MoveThreshold[axis]){
				moved[axis] := 1
			}
			
			this.UpdateHistory(axis, obj)
		}

		if (moved.x || moved.y){
			; A move over the threshold was detected.
			this.ChangeState(this.STATE_OVER_THRESH)
		} else {
			this.ChangeState(this.STATE_UNDER_THRESH)
		}
	}
	
	UpdateHistory(axis, obj){
		this.History[axis].InsertAt(1, obj)
		; Enforce max number of entries
		max := this.History[axis].Length()
		if (max > (this.MOVE_BUFFER_SIZE - 1)){
			this.History[axis].RemoveAt(max, max - this.MOVE_BUFFER_SIZE)
		}
	}
	
	DoRoll(){
		static axes := {x: 1, y: 2}
		
		;s := ""
		
		if (this.State != this.STATE_ROLLING){
			; If roll has just started, calculate roll vector from movement history
			this.LastMove := {x: 0, y: 0}
			
			for axis in axes {
				;s .= axis ": "
				trend := 0
				if (this.History[axis].Length() < this.MOVE_BUFFER_SIZE){
					; ignore gestures that are too short
					continue
				}
				Loop % this.History[axis].Length() {
					if (A_Index != 1){
						; Calculate the trend of the history.
						trend += (this.History[axis][A_Index].delta_move - this.History[axis][A_Index-1].delta_move)
					}
					this.LastMove[axis] += this.History[axis][A_Index].delta_move
					s .= this.History[axis][A_Index].delta_move ","
				}
				;s .= "(" trend ")`n"
				/*
				Disabled, as seems to break mouse trackpads.
				Also seems to stop MoveFactor being applied to both axes?
				if (sgn(trend) != sgn(this.History[axis][1].delta_move)){
					; downward trend of move speed detected - this is probably a normal stop of the mouse, not a lift
					continue
				}
				*/
				this.LastMove[axis] := round(this.LastMove[axis] * this.MoveFactor[axis])
			}
		}
		
		if (this.LastMove.x = 0 && this.LastMove.y = 0){
			return
		}
		this.ChangeState(this.STATE_ROLLING)

		;~ OutputDebug % "AHK| ROLL DETECTED: `n" s "Rolling x: " this.LastMove.x ", y: " this.LastMove.y "`n`n"
		fn := this.MoveFunc
		while (this.State == this.STATE_ROLLING){
			; Send output
			DllCall("user32.dll\mouse_event", "UInt", 0x0001, "UInt", this.LastMove.x, "UInt", this.LastMove.y, "UInt", 0, "UPtr", 0)
			if (this.Friction){
				this.LastMove.x := this.ApplyFriction(this.LastMove.x, this.Friction)
				this.LastMove.y := this.ApplyFriction(this.LastMove.y, this.Friction)
				if (this.LastMove.x == 0 && this.LastMove.y == 0){
					this.State := this.STATE_UNDER_THRESH
					break
				}
			}
			; Wait for a bit (allow real mouse movement to be detected, which will turn off roll)
			Sleep % this.RollFreq
		}
	}

	ChangeState(newstate){
		fn := this.TimeOutFunc
		if (this.State != newstate){
			;~ OutputDebug, % "AHK| Changing State to : " this.StateNames[newstate]
			this.State := newstate
		}
		
		; DO NOT return if this.State == newstate!
		; We need to reset the timer!
		
		if (this.State = this.STATE_UNDER_THRESH){
			; Kill the timer
			SetTimer % fn, Off
			; Clear the history
			this.InitHistory()
		} else if (this.State = this.STATE_OVER_THRESH){
			; Mouse is moving fast - start timer to detect sudden stop in messages (mouse was lifted in motion)
			SetTimer % fn, % this.TimeOutRate
		}
		/* else if (this.State = this.STATE_ROLLING){
			;this.LastMove := {x: 0, y: 0}
		}
		*/
	}
	
	InitHistory(){
		this.History := {x: [], y: []}
	}
	
}

^Esc::
	ExitApp