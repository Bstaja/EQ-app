extends LayerAxis

func _ready():
	pass

func _process(_delta):
	if Input.is_action_just_pressed("ui_up"):
		range_amplitude += Vector2(5, 5)
	if Input.is_action_just_pressed("ui_down"):
		range_amplitude -= Vector2(5, 5)
	queue_redraw()
	
	for i in get_parent().get_node("LayerGraphs").get_children():
		i.queue_redraw()
	pass


func _on_mouse_exited():
	print("exit")
	active = false


func _on_mouse_entered():
	print("enter")
	active = true
