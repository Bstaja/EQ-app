extends Camera2D

var pos_mouse
var pos_cam

func _process(delta):
	if (Input.is_action_just_pressed("mb_right")):
		pos_mouse = get_global_mouse_position()
		pos_cam = position
	if (Input.is_action_pressed("mb_right")):
		var position_new = pos_cam - get_global_mouse_position() + pos_mouse
		pos_mouse = pos_mouse+(position_new-position)
		position = position_new
