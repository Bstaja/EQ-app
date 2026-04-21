extends TabContainer

@export var audio_recorder:AudioStreamPlayer
@export var graph_list_node:NodePath

enum {
	SPECTRUM_ANALYZER = 0,
	INPUT_BUS = 2,
	OUTPUT_BUS = 1,
}

var fft_str = PackedStringArray(["256", "512", "1024", "2048", "4096", "8192", "16K", "32K", "64K", "128K", "256K"])

func _ready():
	var input_d = $Measurement/ScrollContainer/HBoxContainer/VBoxContainer/InputDevice/OptionButton
	var output_d = $Measurement/ScrollContainer/HBoxContainer/VBoxContainer/OutputDevice/OptionButton
	var test_signal = $Measurement/ScrollContainer/HBoxContainer/VBoxContainer/TestSignal/OptionButton
	var fft_buffer = $Measurement/ScrollContainer/HBoxContainer/VBoxContainer/FFTSize/OptionButton
	
	var output_dev_list = AudioServer.get_output_device_list()
	var input_dev_list = AudioServer.get_input_device_list()
	
	for i in output_dev_list:
		output_d.add_item(i)
	
	for i in input_dev_list:
		input_d.add_item(i)
	
	test_signal.add_item("White Noise")
	
	for i in fft_str:
		fft_buffer.add_item(i)
	
	fft_buffer.selected = 8
	
func _on_option_button_item_selected(index):
	pass
	
	
