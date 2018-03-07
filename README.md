# AutoHotInterception

AutoHotInterception(AHI) allows you to execute AutoHotkey code in response to keys on a *specific* keyboard, whilst (optionally) blocking the native functionality of that key.  
In other words, you can use a key on a second (or third, or fourth..) keyboard to trigger AHK code, and that key will not be seen by applications. You can use the *same key* on multiple keyboards for individual actions.  

AHI uses the Interception driver by Francisco Lopez  

# Setup
1. Download and install the [Interception Driver](http://www.oblita.com/interception)  
2. Download a zip from the releases page and extract it to a folder
3. Copy the `interception.dll` from the folder where you ran the interecption install into the `lib` folder  
(You can optionally place the contents of the `lib` folder in `My Documents\AutoHotkey\lib`
4. Edit the example script, enter the VID and PID of your keyboard
5. Run the example script
