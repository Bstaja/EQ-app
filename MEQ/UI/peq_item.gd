extends Panel

func _ready():
	$FilterSettings/Type.add_item("Peaking Filter", FilterConstants.PEAKING)
	$FilterSettings/Type.add_item("Low Pass Filter", FilterConstants.LOW_PASS)
	$FilterSettings/Type.add_item("High Pass Filter", FilterConstants.HIGH_PASS)
	$FilterSettings/Type.add_item("Band Pass Filter", FilterConstants.BAND_PASS)
	$FilterSettings/Type.add_item("Notch Filter", FilterConstants.NOTCH)
	$FilterSettings/Type.add_item("All Pass Filter", FilterConstants.ALL_PASS)
	$FilterSettings/Type.add_item("Low Shelf Filter", FilterConstants.LOW_SHELF)
	$FilterSettings/Type.add_item("High Shelf Filter", FilterConstants.HIGH_SHELF)

func _on_bDelete_button_down():
	queue_free()

