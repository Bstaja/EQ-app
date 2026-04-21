extends Node

var effect
var recording

func _ready():
	var idx = AudioServer.get_bus_index("AudioInput")
	effect = AudioServer.get_bus_effect(idx, 1)

func _process(_delta):
	pass
	#if (Input.is_action_just_pressed("ui_accept")):
	#	if effect.is_recording_active():
	#		recording = effect.get_recording()
	#		effect.set_recording_active(false)
	#		var save_path = "C:\\Users\\Marius\\Desktop\\test.wav"
	#		recording.save_to_wav(save_path)
	#		print("Saved WAV file to: %s\n(%s)" % [save_path, ProjectSettings.globalize_path(save_path)])
	#
	#	else:
	#		effect.set_recording_active(true)
			
