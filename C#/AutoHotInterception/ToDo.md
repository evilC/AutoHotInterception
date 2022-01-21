# Fix issues with Numlock/Pause 
BlueChipps in AHI Discord has a proposed fix [here](https://github.com/bluechipps/AutoHotInterception/tree/pause_numlock_support)

# Send before firing callback in Subscription Mode?
If trying to write a system like AHK's hotstrings, but using AHI, 
if you had subscriptions with block set to false, you would want the key to be sent before firing the callback. 
That way, if you intended to replace `btw` with `By the way`, when the callback fired for `w`, 
the `w` would already be in the text editor.  
As it stands, when the callback for `w` fired, the `w` would not bein the text editor, and after AHI sent the `w` 
(After firing the callback), you would have no chance to delete the `w` (So you would end up with `by the wayw` in the text editor)

# Disablling filter whilst sub or unsub is happening
When a Subscribe or Unsubscribe call is made, the filter is turned off before the sub/unsub, and then turned on again afterwards.  
This seems un-necesarry, and could result in some blocked input "leaking" through.  
Consider removing this