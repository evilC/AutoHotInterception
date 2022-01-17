# Fix issues with Numlock/Pause 
BlueChipps in AHI Discord has a proposed fix [here](https://github.com/bluechipps/AutoHotInterception/tree/pause_numlock_support)

# Poor coding of extended keys
Currently, there is an `.ignore` flag on `KeyboardState`
When an extended code comes in (eg extended Shift or Ctrl), then the key may be ignored and `block` could never be set 
so the extended modifier will always be passed through.  
Implement better system:  
* When an extended modifier is seen, add it to a buffer  
* When the next key comes in, check if it is subscribed and blocked
* If it is, then purge the buffer
* If it is not, play back the buffered extended key code and then play back the unblocked key

Del is a good example:
If Del (Above arrow keys, not on Numpad) is pressed while NumLock is off, it sends an Extended LCtrl, then the Pause Key

# Send before firing callback in Subscription Mode?
If trying to write a system like AHK's hotstrings, but using AHI, 
if you had subscriptions with block set to false, you would want the key to be sent before firing the callback. 
That way, if you intended to replace `btw` with `By the way`, when the callback fired for `w`, 
the `w` would already be in the text editor.  
As it stands, when the callback for `w` fired, the `w` would not bein the text editor, and after AHI sent the `w` 
(After firing the callback), you would have no chance to delete the `w` (So you would end up with `by the wayw` in the text editor)