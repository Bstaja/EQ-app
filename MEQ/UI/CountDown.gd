extends Label

var is_counting:bool = false
var is_recording:bool = false
var time_passed:float = 0.0
var time:float = 3.0
signal finished_counting
signal finished_recording

func _process(delta):
	if (is_counting):
		time_passed += delta
		text = str(int(time - time_passed + 1))
		if (time_passed >= time):
			if (!is_recording):
				emit_signal("finished_counting")
			else:
				emit_signal("finished_recording")
			text = ""
			time_passed = 0.0
			is_counting = false
			is_recording = false

func start_counting(seconds:float = 3.0):
	time = seconds
	is_counting = true
	
func start_recording(seconds:float = 15.0):
	time = seconds
	is_counting = true
	is_recording = true


