extends Node2D

export var gr_scale_x = 600
export var gr_scale_y = 10
export var fr_on_graph = PoolIntArray()
export var fr_samples = PoolIntArray()

var font = preload("res://font.tres")

var points = PoolVector2Array()
var graph_x = PoolRealArray()
var graph_y = PoolIntArray()
var mouse_pos = Vector2(0, 0)
var frequency = 0
var decibels = 0
var target = PoolVector2Array()
var target_fr := Dictionary()
var eq_data = Dictionary()

const zoom_min = Vector2(1, 1)
const zoom_max = Vector2(.2, .2)
const zoom_spd = .05

enum GenItems {
	ADJUST,
	EXPORT,
	FROM_FILES,
	FROM_FILES_CSV,
}
const gen_str = PoolStringArray(["Adjust FR to target FR", "Export GraphicEQ", "Generate EQ from files", "Generate EQ from files - CSV"])

enum ToolsMenuItems {
	LOAD_IMG,
	SAVE,
	LOAD,
}
const tools_str = PoolStringArray(["Load background image", "Save current graph", "Load graph"])

enum OptionsMenuItems {
	SET_TARGET,
	SET_FREQUENCIES,
	SET_SAMPLES,
}
const options_str = PoolStringArray(["Set FR target", "Change graph frequencies", "Set frequency samples"])

class CustomSort:
	static func sort(a, b):
		if (a.x<b.x):
			return true
		return false

func _ready():
	
	auto_sample(695)
	
	_on_Clear_pressed()
	
	auto_sample(695)
	
	initialize_menubar()
	
	var f = File.new()
	if (f.file_exists("user://target.txt")):
		f.open("user://target.txt", File.READ)
		target_fr = f.get_var(true)
		status("Loaded target.txt")
	else:
		status("Missing target.txt!")
	f.close()
	
#	get_tree().get_root().set_transparent_background(true)
	
	var offset = -log10(fr_on_graph[0])*gr_scale_x+50
	
	for i in fr_on_graph:
		var x = log10(i)*gr_scale_x+offset
		graph_x.append(x)
		
	var i = 50
	offset = -40*gr_scale_y
	while(i<120):
		graph_y.append(i*gr_scale_y+offset)
		i+=5
	
	var aux = PoolStringArray()
	for k in fr_on_graph:
		if (k>=1000):
			aux.append(str(k/1000)+"k")
		else:
			aux.append(str(k))
	fr_on_graph = aux
	
	get_parent().get_node("UI/Smoothen").connect("button_down", self, "_on_Smoothen_pressed")
	get_parent().get_node("UI/Simplify").connect("button_down", self, "_on_Simplify_pressed")
	get_parent().get_node("UI/Clear").connect("button_down", self, "_on_Clear_pressed")
	
	status("Initialization succesful")
	
func _process(delta):
	mouse_pos = get_local_mouse_position()
	mouse_pos.x = clamp(mouse_pos.x, 50, 1850)
	mouse_pos.y = clamp(mouse_pos.y, 100, 750)
	
	frequency = round(pow(10, (mouse_pos.x+730.618)/gr_scale_x))
	
	for i in range(fr_samples.size()-1):
		if (frequency<=fr_samples[i+1]):
			if ((fr_samples[i] + fr_samples[i+1])/2 - frequency > 0):
				frequency = fr_samples[i]
			else:
				frequency = fr_samples[i+1]
			break
	
	#frequency = clamp(frequency, fr_samples[0], fr_samples[-1])
	
	decibels = stepify(170-(mouse_pos.y+400)/gr_scale_y, 0.1)
	
	var x_move = Input.get_action_strength("ui_right") - Input.get_action_strength("ui_left")
	var y_move = Input.get_action_strength("ui_down") - Input.get_action_strength("ui_up")
	var zoom = int(Input.is_action_just_released("wheel_down")) - int(Input.is_action_just_released("wheel_up"))
	
	if (Input.is_action_pressed("ctrl")):
		var incr = Vector2(x_move, y_move)
		if (Input.is_action_pressed("lshift")):
			incr/=10
		$bkg.rect_position+=incr
	
	if (Input.is_action_pressed("alt")):
		var incr = Vector2(x_move, y_move)
		if (Input.is_action_pressed("lshift")):
			incr/=10
		$bkg.rect_size+=incr
	
	if (Input.is_action_pressed("mb_left")):
		if (mouse_pos==get_local_mouse_position()):
			add_target_value()
	
	$Camera2D.zoom += Vector2(1, 1)*zoom*zoom_spd*$Camera2D.zoom
	
	update()

func _draw():
	var f = 0
	for i in graph_x:
		draw_line(Vector2(i, 750), Vector2(i, 100), Color.aqua)
		draw_string(font, Vector2(i, 770), fr_on_graph[f])
		f+=1
	f = 120
	for i in graph_y:
		draw_line(Vector2(graph_x[0], i), Vector2(graph_x[0]+1800, i), Color.red)
		draw_string(font, Vector2(graph_x[0]-40, i), str(f))
		f-=5
	
	if (mouse_pos == get_local_mouse_position()):
		draw_line(Vector2(mouse_pos.x, 750), Vector2(mouse_pos.x, 100), Color.yellow)
		draw_circle(mouse_pos, 3, Color.yellow)
		var fr = str(frequency)+"Hz"
		var db = str(decibels)+"dB"
		draw_string(font, Vector2(mouse_pos.x, 50), fr)
		draw_string(font, Vector2(mouse_pos.x, 80), db)
	
	draw_polyline(target, Color.yellow)
	
	for i in target:
		draw_circle(i, 2, Color.blue)

func log10(value):
	return log(value) / log(10)

func status(text:String):
	var datetime = OS.get_datetime()
	var datetime_str = ("\n["+str(datetime["day"])+"/"+str(datetime["month"])+"/"+str(datetime["year"])+" - "
						+str(datetime["hour"])+":"+str(datetime["minute"])+":"+str(datetime["second"])+"]  ")
	get_parent().get_node("UI").get_node("Status").text += datetime_str+text

func add_target_value():
	var aux = Array(target)
	var point = get_graph_point(Vector2(frequency, decibels))
	var replace = has_point(aux, point.x)
	if (replace!=-1):
		if (target[replace].y!=point.y):
			target[replace] = point
			status("FR graph updated ("+str(frequency)+"Hz -> "+str(decibels)+"dB)")
			eq_data[frequency] = decibels
	else:
		aux.append(point)
		aux.sort_custom(CustomSort, "sort")
		target = PoolVector2Array(aux)
		status("FR graph updated ("+str(frequency)+"Hz -> "+str(decibels)+"dB)")
		eq_data[frequency] = decibels

func get_graph_point(point:Vector2):
	return Vector2(log10(point.x)*gr_scale_x-730.618, (170-point.y)*gr_scale_y-400)

func has_point(arr, x):
	var index = 0
	for i in arr:
		if (i.x == x):
			return index
		index+=1
	return -1

func initialize_menubar():
	
	for i in get_parent().get_node("UI").get_node("MenuBar").get_children():
		i.get_popup().add_font_override("font", font)
	
	for i in options_str:
		get_parent().get_node("UI").get_node("MenuBar/Options").get_popup().add_item(i)
	for i in tools_str:
		get_parent().get_node("UI").get_node("MenuBar/Tools").get_popup().add_item(i)
	for i in gen_str:
		get_parent().get_node("UI").get_node("MenuBar/GenerateEQ").get_popup().add_item(i)
	
	get_parent().get_node("UI").get_node("MenuBar/GenerateEQ").get_popup().connect("id_pressed", self, "option_geneq")
	get_parent().get_node("UI").get_node("MenuBar/Tools").get_popup().connect("id_pressed", self, "option_tools")
	get_parent().get_node("UI").get_node("MenuBar/Options").get_popup().connect("id_pressed", self, "option_options")

func option_geneq(id):
	match(id):
		GenItems.ADJUST:
			if (target_fr.empty()):
				status("FR target not set")
			else:
				for i in eq_data.keys():
					if (target_fr.has(i) and eq_data.has(i)):
						eq_data[i] = 90 + (target_fr[i] - eq_data[i])
				update_graph_form_eqdata()
				status("Updated current FR graph to match target FR")
		GenItems.EXPORT:
			var data = "GraphicEQ: "
			var file = File.new()
			file.open(OS.get_system_dir(OS.SYSTEM_DIR_DESKTOP)+"\\GraphicEQ.txt", File.WRITE)
			for i in eq_data.keys():
				data+=str(i)+" "+str(eq_data[i]-100)+"; "
			file.store_string(data)
			file.close()
			status("Exported current FR graph to Desktop\\GraphicEQ.txt")
		GenItems.FROM_FILES:
			gen_eq_files()
		GenItems.FROM_FILES_CSV:
			gen_eq_files_csv()
			

func option_tools(id):
	match(id):
		ToolsMenuItems.LOAD:
			status("Load graph")
		ToolsMenuItems.SAVE:
			status("Save graph")
		ToolsMenuItems.LOAD_IMG:
			var file = File.new()
			if (file.file_exists(OS.get_system_dir(OS.SYSTEM_DIR_DESKTOP)+"\\img.png")):
				var img = Image.new()
				var err = img.load(OS.get_system_dir(OS.SYSTEM_DIR_DESKTOP)+"\\img.png")
				if (err != OK):
					status("Failed to load image - Desktop\\img.png")
					return
				var texture  = ImageTexture.new()
				texture.create_from_image(img)
				$bkg.texture = texture
				status("Background image loaded!")
			else:
				status("Image not found - Desktop\\img.png")

func option_options(id):
	match(id):
		OptionsMenuItems.SET_FREQUENCIES:
			status("Set frequencies")
		OptionsMenuItems.SET_SAMPLES:
			status("Set frequency samples")
		OptionsMenuItems.SET_TARGET:
			var file = File.new()
			file.open("user://target.txt", File.WRITE)
			file.store_var(eq_data, true)
			file.close()
			target_fr = eq_data
			status("Target FR updated")


func _on_Clear_pressed():
	target = PoolVector2Array()
	decibels = 90
	
	for i in fr_samples:
		eq_data[i] = 90
		frequency = i
		add_target_value()
	get_parent().get_node("UI/Status").text = ""
	status("Graph cleared")


func _on_Smoothen_pressed():
	for i in range(0, target.size()-1):
		target[i].y = lerp(target[i].y, (target[i-1].y+target[i+1].y)/2, .5)
		eq_data[int(round(pow(10, (target[i].x+730.618)/gr_scale_x)))] = stepify(170-(target[i].y+400)/gr_scale_y, 0.1)
	status("Graph smoothened")
		

func _on_Simplify_pressed():
	var i = 1
	var f = target.size()
	
	if (f>15):
		while(i<f):
			target[i] = Vector2.ZERO
			i+=2
		status("Graph simplified ("+str(f)+" -> "+str(int(f/2))+")")
	else:
		status("Cannot simplify (points_nr < 16)")
	
	var aux = Array(target)
	while(aux.has(Vector2.ZERO)):
		aux.erase(Vector2.ZERO)
	
	target = PoolVector2Array(aux)

func auto_sample(nr:int, _range:Vector2 = Vector2(20, 20000)):
	_range = Vector2(log10(_range.x), log10(_range.y))
	var s = (_range.y - _range.x)/nr
	var i = _range.x
	fr_samples = PoolIntArray()
	
	while (i<_range.y):
		var f = int(pow(10, i))
		if (f>=20 and f<=20000):
			fr_samples.append(f)
		i+=s
	
	status("Frequency samples updated (auto -> "+str(nr)+" samples)")

func update_graph_form_eqdata():
	target = PoolVector2Array()
	for i in eq_data.keys():
		target.append(get_graph_point(Vector2(i, eq_data[i])))

func gen_eq_files_csv():
	var file = File.new()
	if (file.file_exists(OS.get_system_dir(OS.SYSTEM_DIR_DESKTOP)+"/GraphicEQt.txt") and file.file_exists(OS.get_system_dir(OS.SYSTEM_DIR_DESKTOP)+"/GraphicEQs.txt")):
		file.open(OS.get_system_dir(OS.SYSTEM_DIR_DESKTOP)+"/GraphicEQt.txt", File.READ)
		var g1 = ""
		while (!file.eof_reached()):
			var line = file.get_line()
			g1+= line+"_"
		file.close()
		if (!g1.begins_with("frequency,raw")):
			status("Invalid CSV file - Desktop\\GraphicEQt.txt")
			return
		var g1_sep = g1.replace("frequency,raw", "").split("_", false)
		g1 = Dictionary()
		for i in g1_sep:
			var s = i.split(",", false)
			g1[int(s[0])] = float(s[1])
		
		file.open(OS.get_system_dir(OS.SYSTEM_DIR_DESKTOP)+"/GraphicEQs.txt", File.READ)
		var g2 = ""
		while (!file.eof_reached()):
			var line = file.get_line()
			g2+= line+"_"
		file.close()
		if (!g2.begins_with("frequency,raw")):
			status("Invalid CSV file - Desktop\\GraphicEQs.txt")
			return
		var g2_sep = g2.replace("frequency,raw", "").split("_", false)
		g2 = Dictionary()
		for i in g2_sep:
			var s = i.split(",", false)
			g2[int(s[0])] = float(s[1])
		
		var data = "GraphicEQ: "
		var result = Dictionary()
		file.open(OS.get_system_dir(OS.SYSTEM_DIR_DESKTOP)+"\\GraphicEQ_result.txt", File.WRITE)
		for i in g1.keys():
			result[i] = 90 + (g1[i] - g2[i])
		eq_data = result
		update_graph_form_eqdata()
		for i in g1.keys():
			data+=str(i)+" "+str(result[i]-100)+"; "
		file.store_string(data)
		file.close()
		status("Exported current FR graph to Desktop\\GraphicEQ_result.txt")

func gen_eq_files():
	var file = File.new()
	if (file.file_exists(OS.get_system_dir(OS.SYSTEM_DIR_DESKTOP)+"/GraphicEQt.txt") and file.file_exists(OS.get_system_dir(OS.SYSTEM_DIR_DESKTOP)+"/GraphicEQs.txt")):
		file.open(OS.get_system_dir(OS.SYSTEM_DIR_DESKTOP)+"/GraphicEQt.txt", File.READ)
		var g1 = ""
		while (!file.eof_reached()):
			var line = file.get_line()
			g1+= line
		file.close()
		if (!g1.begins_with("GraphicEQ:")):
			status("Invalid GraphicEQ file - Desktop\\GraphicEQt.txt")
			return
		g1 = g1.replace("GraphicEQ:", "")
		var g1_sep = g1.split(";", false)
		g1 = Dictionary()
		for i in g1_sep:
			var s = i.split(" ", false)
			if (s.size() == 2):
				g1[int(s[0])] = float(s[1])
		
		file.open(OS.get_system_dir(OS.SYSTEM_DIR_DESKTOP)+"/GraphicEQs.txt", File.READ)
		var g2 = ""
		while (!file.eof_reached()):
			var line = file.get_line()
			g2+= line
		file.close()
		if (!g2.begins_with("GraphicEQ:")):
			status("Invalid GraphicEQ file - Desktop\\GraphicEQs.txt")
			return
		g2 = g2.replace("GraphicEQ:", "")
		var g2_sep = g2.split(";", false)
		g2 = Dictionary()
		for i in g2_sep:
			var s = i.split(" ", false)
			if (s.size() == 2):
				g2[int(s[0])] = float(s[1])
		
		
		var data = "GraphicEQ: "
		var result = Dictionary()
		file.open(OS.get_system_dir(OS.SYSTEM_DIR_DESKTOP)+"\\GraphicEQ_result.txt", File.WRITE)
		for i in g1.keys():
			result[i] = 90 + (g1[i] - g2[i])
		eq_data = result
		update_graph_form_eqdata()
		for i in g1.keys():
			data+=str(i)+" "+str(result[i]-100)+"; "
		file.store_string(data)
		file.close()
		status("Exported current FR graph to Desktop\\GraphicEQ_result.txt")
		
	else:
		status("Missing GraphicEQt.txt or GraphicEQs.txt from DESKTOP")
