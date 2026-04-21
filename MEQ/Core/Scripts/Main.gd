extends Control

var eq_menu = preload("res://UI/eq_side_menu.tscn")
var measurement_menu = preload("res://UI/measurement_side_menu.tscn")
var legend_item = preload("res://UI/LegendItem.tscn")
var layer_axis:Node
var tab_menus:Node
var layer_graphs:Node
var bottom_menu:Node
var graph_selector:OptionButton
var graph_selector_color:ColorRect

var selected_graph:int = -1
var measurement_graph:int = -1

var always_redraw:bool = false

func _ready():
	get_tree().get_root().connect("files_dropped", _on_files_dropped)
	DisplayServer.window_set_min_size(Vector2i(1280, 720))
	layer_axis = $PanelMain/GraphArea/LayerAxis
	layer_graphs = $PanelMain/GraphArea/LayerGraphs
	tab_menus = $PanelMain/TabMenus
	bottom_menu = $PanelBottom/HBoxContainer/ScrollContainer/HBoxContainer
	graph_selector = $PanelMain/GraphSelection/SelectGraph
	graph_selector_color = $PanelMain/GraphSelection/ColorRect

func _process(delta):
	
	if (Input.is_action_just_pressed("paste")):
		import_clipboard()
	
	if (always_redraw):
		for i in layer_graphs.get_children():
			i.queue_redraw()

func _on_toggle_side_menu_pressed():
	always_redraw = true
	if ($PanelMain/GraphArea/ToggleSideMenu.text == ">"):
		$AnimationPlayer.play("ShowSideMenu")
	else:
		$AnimationPlayer.play("HideSideMenu")
	
	await get_tree().create_timer(1.0).timeout
	always_redraw = false

func _on_new_measurement_pressed():
	if (measurement_graph != -1):
		return
	var rng = RandomNumberGenerator.new()
	rng.randomize()
	var curve:GraphMeasurement = GraphMeasurement.new()
	var side_menu = measurement_menu.instantiate()
	var legend = legend_item.instantiate()
	curve.smoothing_factor = 0.1
	curve.layer_axis_path = layer_axis.get_path()
	curve.graph_color = Color.from_hsv(rng.randf_range(0, 1), .8, .9)
	curve.layout_direction = Control.LAYOUT_DIRECTION_INHERITED
	curve.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	curve.size_flags_vertical = Control.SIZE_EXPAND_FILL
	side_menu.graph_list_node = layer_graphs.get_path()
	legend.graph_list_node = layer_graphs
	
	layer_graphs.add_child(curve)
	tab_menus.add_child(side_menu)
	bottom_menu.add_child(legend)
	graph_selector.add_item("Measurement")
	
	side_menu.get_node("Measurement/StartMeasurement").connect("pressed", $PanelMain/GraphArea/CountDown.start_counting)
	
	#var test_output = []
	#var f = FileAccess.open("test.bat", FileAccess.WRITE)
	#f.store_string("python -m autoeq --input-file \"D:\\Facultate\\Licenta\\TestStuff\\Audio-Technica ATH-M20x.csv\" --target \"D:\\Facultate\\Licenta\\TestStuff\\Harman over-ear 2018.csv\" --output-dir \"D:\\Facultate\\Licenta\\TestStuff\\OutputDIr\" --parametric-eq --parametric-eq-config \"8_PEAKING_WITH_SHELVES\" --fs 48000 --bit-depth 24 --standardize-input")
	#f.close()
	#
	#OS.execute("test.bat", [], test_output, true)
	#print(test_output)
	select_graph(curve.get_index())
	measurement_graph = selected_graph
	legend.get_node("HBoxContainer/Name").text = "Measurement"
	graph_selector.selected = selected_graph
	graph_selector_color.color = curve.graph_color
	return selected_graph

func _on_new_eq_pressed():
	var rng = RandomNumberGenerator.new()
	rng.randomize()
	var curve:GraphCurve = GraphCurve.new()
	var side_menu = eq_menu.instantiate()
	var legend = legend_item.instantiate()
	curve.layer_axis_path = layer_axis.get_path()
	curve.graph_color = Color.from_hsv(rng.randf_range(0, 1), .8, .9)
	curve.layout_direction = Control.LAYOUT_DIRECTION_INHERITED
	curve.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	curve.size_flags_vertical = Control.SIZE_EXPAND_FILL
	side_menu.graph_list_node = layer_graphs.get_path()
	legend.graph_list_node = layer_graphs
	
	layer_graphs.add_child(curve)
	tab_menus.add_child(side_menu)
	bottom_menu.add_child(legend)
	graph_selector.add_item("Parametric EQ")
	
	#var test_output = []
	#var f = FileAccess.open("test.bat", FileAccess.WRITE)
	#f.store_string("python -m autoeq --input-file \"D:\\Facultate\\Licenta\\TestStuff\\Audio-Technica ATH-M20x.csv\" --target \"D:\\Facultate\\Licenta\\TestStuff\\Harman over-ear 2018.csv\" --output-dir \"D:\\Facultate\\Licenta\\TestStuff\\OutputDIr\" --parametric-eq --parametric-eq-config \"8_PEAKING_WITH_SHELVES\" --fs 48000 --bit-depth 24 --standardize-input")
	#f.close()
	#
	#OS.execute("test.bat", [], test_output, true)
	#print(test_output)
	select_graph(curve.get_index())
	legend.get_node("HBoxContainer/Name").text = "Parametric EQ"
	graph_selector.selected = selected_graph
	graph_selector_color.color = curve.graph_color
	return selected_graph

func remove_graph(index:int):
	if (index >= 0 and index < layer_graphs.get_child_count()):
		if (selected_graph == measurement_graph):
			measurement_graph = -1
		select_graph(0)
		if (layer_graphs.get_child_count() == 1):
			selected_graph = -1
			graph_selector_color.color = Color.BLACK
		else:
			graph_selector_color.color = layer_graphs.get_child(selected_graph).graph_color
		layer_graphs.get_child(index).queue_free()
		tab_menus.get_child(index).queue_free()
		bottom_menu.get_child(index).queue_free()
		graph_selector.remove_item(index)
		graph_selector.selected = selected_graph
		

func import_clipboard():
	var data = DisplayServer.clipboard_get()
	var f = FileAccess.open("user://Clipboard.txt", FileAccess.WRITE)
	f.store_string(data)
	f.close()
	if (Input.is_action_pressed("lshift")):
		load_graph(OS.get_user_data_dir() + "\\Clipboard.txt", true)
	else:
		load_graph(OS.get_user_data_dir() + "\\Clipboard.txt")

func load_graph(path:String, merge:bool = false, invert_peq:bool = false):
	randomize()
	var f = FileAccess.open(path, FileAccess.READ)
	var data = ""
	while(!f.eof_reached()):
		data+=f.get_line()+"@"
	f.close()
	var g_name = path.split("\\", false)[-1].split(".", false)[0]
	var fr = PackedFloat64Array()
	var db = PackedFloat64Array()
	var peq = Array()
	var preamp
	var graph_index
	if (merge and selected_graph != -1):
		graph_index = selected_graph
	else:
		graph_index = _on_new_eq_pressed()
		bottom_menu.get_child(graph_index).get_node("HBoxContainer/Name").text = g_name
	if(data.begins_with("frequency,raw")):
		data = data.replace(data.left(data.find("@")+1), "")
		var sep_data = data.split("@", false)
		for i in sep_data:
			var point = i.split(",", false)
			fr.append(int(point[0]))
			db.append(snapped(float(point[1]), 0.01))
		layer_graphs.get_child(graph_index).graph_load(fr, db, fr.size())
		print("CSV loaded")
	elif (data.begins_with("GraphicEQ:") or data.begins_with("Filter") or data.begins_with("Preamp: ")):
		var sep_data = data.split("@", false)
		for i in sep_data:
			if (i.begins_with("GraphicEQ: ")):
				var geq = i.replace("GraphicEQ: ", "")
				var sep_geq_data = geq.split(";", false)
				for j in sep_geq_data:
					var point = j.split(" ", false)
					fr.append(int(point[0]))
					db.append(snapped(float(point[1]), 0.01))
			elif (i.begins_with("Filter") or i.begins_with("#Filter")):
				var enabled = true
				if (i.begins_with("#")):
					i.erase(0, 1)
					enabled = false
				var fil_str = i.replace(i.left(i.find("ON")+3), "").split(" ", false)
				var fil = [0, 0.0, 0.0, 0, enabled]
				
				match(fil_str[0]):
					"LPQ":
						fil[0] = int(fil_str[2])
						fil[2] = float(fil_str[5])
						fil[3] = FilterConstants.LOW_PASS
					"HPQ":
						fil[0] = int(fil_str[2])
						fil[2] = float(fil_str[5])
						fil[3] = FilterConstants.HIGH_PASS
					"BP":
						fil[0] = int(fil_str[2])
						fil[2] = float(fil_str[5])
						fil[3] = FilterConstants.BAND_PASS
					"NO":
						fil[0] = int(fil_str[2])
						fil[2] = float(fil_str[5])
						fil[3] = FilterConstants.NOTCH
					"AP":
						fil[0] = int(fil_str[2])
						fil[2] = float(fil_str[5])
						fil[3] = FilterConstants.ALL_PASS
					"LSC", "LS":
						fil[0] = int(fil_str[2])
						fil[1] = float(fil_str[5])
						fil[2] = float(fil_str[8])
						fil[3] = FilterConstants.LOW_SHELF
					"HSC", "HS":
						fil[0] = int(fil_str[2])
						fil[1] = float(fil_str[5])
						fil[2] = float(fil_str[8])
						fil[3] = FilterConstants.HIGH_SHELF
					_:
						fil[0] = int(fil_str[2])
						fil[1] = float(fil_str[5])
						fil[2] = float(fil_str[8])
						fil[3] = FilterConstants.PEAKING
				peq.append(fil)
			elif(i.begins_with("Preamp:")):
				preamp = float(i.split(" ", false)[1])
		if (peq.size() != 0 and fr.size() ==0):
			for p in peq:
				if (invert_peq):
					p[1]*=-1
				tab_menus.get_child(graph_index).create_filter(p[3], p[0], p[1], p[2], p[4])
		elif (peq.size() != 0  and fr.size() != 0):
			layer_graphs.get_child(graph_index).graph_load(fr, db, fr.size())
			for p in peq:
				tab_menus.get_child(graph_index).create_filter(p[3], p[0], p[1], p[2], p[4])
		else:
			layer_graphs.get_child(graph_index).graph_load(fr, db, fr.size())
		print("EqualizerAPO config loaded")
	else:
		print("File not compatible")

func select_graph(idx:int):
	if (selected_graph != -1):
		layer_graphs.get_child(selected_graph).active = false
		tab_menus.get_child(selected_graph).visible = false
	selected_graph = idx
	layer_graphs.get_child(selected_graph).active = true
	tab_menus.get_child(selected_graph).visible = true
	graph_selector_color.color = layer_graphs.get_child(selected_graph).graph_color

func _on_files_dropped(files):
	load_graph(files[0])


func _on_select_graph_item_selected(index):
	select_graph(index)

func _on_count_down_finished_counting():
	var gr:GraphMeasurement = layer_graphs.get_child(selected_graph)
	await get_tree().create_timer(1.0).timeout
	gr.start_measurement(15.0)
	$PanelMain/GraphArea/CountDown.start_recording(15.0)
	
	$TestStuff/AudioPlayer.stream = AudioLoader.new().loadfile("D:\\Facultate\\Licenta\\TestStuff\\whitenoise.wav")
	$TestStuff/AudioPlayer.play()


func _on_count_down_finished_recording():
	var gr:GraphMeasurement = layer_graphs.get_child(selected_graph)
	var results = gr.end_measurement()
	var graph_index = _on_new_eq_pressed()
	var data = "frequency,raw\n";
	bottom_menu.get_child(graph_index).get_node("HBoxContainer/Name").text = "Measurement result"
	graph_selector.set_item_text(graph_index, "Measurement result")
	
	for i in range(results[0].size()):
		data += str(results[0][i]) + "," + str(results[1][i]) + "\n"
	
	var f = FileAccess.open("D:\\Facultate\\Licenta\\TestStuff\\Calibration.csv", FileAccess.WRITE)
	f.store_string(data)
	f.close()
	
	var test_output = []
	f = FileAccess.open("test.bat", FileAccess.WRITE)
	f.store_string("python -m autoeq --input-file \"D:\\Facultate\\Licenta\\TestStuff\\Calibration.csv\" --target \"D:\\Facultate\\Licenta\\TestStuff\\Reference.csv\" --output-dir \"D:\\Facultate\\Licenta\\TestStuff\\OutputDIr\" --parametric-eq --parametric-eq-config \"8_PEAKING_WITH_SHELVES\" --fs 48000 --bit-depth 16 --max-gain 30")
	f.close()
	
	OS.execute("test.bat", [], test_output, true)
	
	load_graph("D:\\Facultate\\Licenta\\TestStuff\\Reference.csv")
	graph_selector.set_item_text(selected_graph, "Reference")
	load_graph("D:\\Facultate\\Licenta\\TestStuff\\OutputDIr\\Calibration\\Calibration ParametricEQ.txt")
	graph_selector.set_item_text(selected_graph, "Calibration ParametricEQ")
	layer_graphs.get_child(graph_index).graph_load(results[0], results[1], results[0].size())
	
