extends Control

@export var border_offset = 16
@export var border_left_offset = 32
@export var font:Font

var width := 0
var height := 0

var x1 := 0
var x2 := 0
var y1 := 0
var y2 := 0 

func _process(_delta):
	
	calc_dimensions()
	queue_redraw()
	

func calc_dimensions():
	width = get_rect().size.x
	height = get_rect().size.y
	x1 = border_left_offset
	x2 = width - border_offset - border_left_offset
	y1 = border_offset
	y2 = height - border_offset*2

func _draw():
	draw_rect(Rect2(x1, y1, x2, y2), Color.WHITE, false, 1)
