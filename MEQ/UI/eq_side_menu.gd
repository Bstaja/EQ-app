extends TabContainer

@export var graph_list_node:NodePath
var peq_settings_item := preload("res://UI/peq_item.tscn")

func _ready():
	var graph:GraphCurve = get_node(graph_list_node).get_child(get_index())
	graph.connect("filter_changed", update_filter_settings)

func _on_btn_new_filter_button_down():
	create_filter(FilterConstants.PEAKING, 500.0, 0.0, 1.0)
	

func create_filter(type:int, frequency:float, amplitude:float, q:float, active:bool = true):
	var graph:GraphCurve = get_node(graph_list_node).get_child(get_index())
	var peq_settings:Panel = peq_settings_item.instantiate()
	
	peq_settings.get_node("HSplitContainer/Frequency").value = frequency
	peq_settings.get_node("HSplitContainer/Amplitude").value = amplitude
	peq_settings.get_node("HSplitContainer/Q").value = q
	
	peq_settings.get_node("FilterSettings/Type").selected = type
	peq_settings.get_node("FilterSettings/Type").connect("item_selected", update_filter_type.bind(peq_settings))
	peq_settings.get_node("FilterSettings/Active").button_pressed = active
	
	peq_settings.get_node("HSplitContainer/Frequency").connect("value_changed", update_filter_frequency.bind(peq_settings))
	peq_settings.get_node("HSplitContainer/Amplitude").connect("value_changed", update_filter_amplitude.bind(peq_settings))
	peq_settings.get_node("HSplitContainer/Q").connect("value_changed", update_filter_q.bind(peq_settings))
	peq_settings.get_node("HSplitContainer/bDelete").connect("pressed", remove_filter.bind(peq_settings))
	
	$Filters/Filters/List.add_child(peq_settings)
	
	graph.new_filter(type, frequency, amplitude, q)
	#graph.queue_redraw()

func update_filter_type(new_type:int, settings_menu:Panel):
	var graph:GraphCurve = get_node(graph_list_node).get_child(get_index())
	graph.update_filter_type(settings_menu.get_index(), new_type)
	#graph.queue_redraw();

func update_filter_frequency(value:float, settings_menu:Panel):
	var graph:GraphCurve = get_node(graph_list_node).get_child(get_index())
	graph.update_filter_frequency(settings_menu.get_index(), value)
	#graph.queue_redraw()
	
func update_filter_amplitude(value:float, settings_menu:Panel):
	var graph:GraphCurve = get_node(graph_list_node).get_child(get_index())
	graph.update_filter_amplitude(settings_menu.get_index(), value)
	#graph.queue_redraw()
	
func update_filter_q(value:float, settings_menu:Panel):
	var graph:GraphCurve = get_node(graph_list_node).get_child(get_index())
	graph.update_filter_q(settings_menu.get_index(), value)
	#graph.queue_redraw()
	
func remove_filter(settings_menu:Panel):
	var graph:GraphCurve = get_node(graph_list_node).get_child(get_index())
	graph.remove_filter(settings_menu.get_index())
	get_node("Filters/Filters/List").remove_child(settings_menu)
	settings_menu.queue_free()
	#graph.queue_redraw()

func update_filter_settings(index:int, frequency:float, amplitude:float, q:float):
	var settings_menu:Panel = get_node("Filters/Filters/List").get_child(index)
	settings_menu.get_node("FilterSettings/Type").grab_focus()
	settings_menu.get_node("HSplitContainer/Frequency").value = frequency
	settings_menu.get_node("HSplitContainer/Amplitude").value = amplitude
	settings_menu.get_node("HSplitContainer/Q").value = q
