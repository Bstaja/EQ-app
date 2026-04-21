extends Panel

var graph_list_node:Node

func _ready():
	$HBoxContainer/ChangeColor.color = graph_list_node.get_child(get_index()).graph_color
	$HBoxContainer/Name.text = "Unnamed"


func _on_change_color_color_changed(color):
	graph_list_node.get_child(get_index()).graph_color = color
	get_tree().root.get_child(0).graph_selector_color.color = color


func _on_b_visible_toggled(toggled_on):
	graph_list_node.get_child(get_index()).visible = toggled_on


func _on_b_delete_pressed():
	get_tree().root.get_child(0).remove_graph(get_index())


func _on_name_text_changed(new_text):
	get_tree().root.get_child(0).graph_selector.set_item_text(get_index(), new_text)
