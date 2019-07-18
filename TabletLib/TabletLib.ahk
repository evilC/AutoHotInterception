/*
Common functions for Tablet Library
*/
#include %A_LineFile%\..\JSON.ahk ; JSON library is used to save / load data as it is easier than using INIRead / INIWrite

LoadBoxes(filename){
	global JSON
	boxes := {}
	FileRead, j, % filename
	if (ERRORLEVEL == 0){
		j := JSON.Load(j)
		for name, b in j {
			box := new Box(name)
			box.StartX := b.StartX
			box.StartY := b.StartY
			box.EndX := b.EndX
			box.EndY := b.EndY
			boxes[name] := box
		}
	}
	return boxes
}

FindBoxName(x, y, boxes){
	for name, box in boxes {
		if (x >= box.StartX && x <= box.EndX && y >= box.StartY && y <= box.EndY){
			return name
		}
	}
	return ""
}

Class Box {
	StartX := 0
	StartY := 0
	EndX := 0
	EndY := 0
	
	__New(name){
		this.BoxName := name
	}
}