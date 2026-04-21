#include "GraphCurve.h"
#include "../Utilities/ArrayOperations.h"

#include <godot_cpp/variant/utility_functions.hpp>

using namespace godot;

void GraphCurve::_bind_methods()
{
	ClassDB::bind_method(D_METHOD("set_layer_axis_path", "p"), &GraphCurve::set_layer_axis_path);
	ClassDB::bind_method(D_METHOD("get_layer_axis_path"), &GraphCurve::get_layer_axis_path);
	ClassDB::bind_method(D_METHOD("set_graph_color", "c"), &GraphCurve::set_graph_color);
	ClassDB::bind_method(D_METHOD("get_graph_color"), &GraphCurve::get_graph_color);
	ClassDB::bind_method(D_METHOD("set_smoothing_factor", "f"), &GraphCurve::set_smoothing_factor);
	ClassDB::bind_method(D_METHOD("get_smoothing_factor"), &GraphCurve::get_smoothing_factor);
	ClassDB::bind_method(D_METHOD("set_active", "value"), &GraphCurve::set_active);
	ClassDB::bind_method(D_METHOD("get_active"), &GraphCurve::get_active);

	ClassDB::bind_method(D_METHOD("new_filter", "type", "frequency", "amplitude", "q"), &GraphCurve::new_filter);
	ClassDB::bind_method(D_METHOD("update_filter", "index", "frequency", "amplitude", "q"), &GraphCurve::update_filter);
	ClassDB::bind_method(D_METHOD("update_filter_frequency", "index"), &GraphCurve::update_filter_frequency);
	ClassDB::bind_method(D_METHOD("update_filter_amplitude", "index", "amplitude"), &GraphCurve::update_filter_amplitude);
	ClassDB::bind_method(D_METHOD("update_filter_q", "index", "q"), &GraphCurve::update_filter_q);
	ClassDB::bind_method(D_METHOD("update_filter_type", "index", "new_type"), &GraphCurve::update_filter_type);
	ClassDB::bind_method(D_METHOD("remove_filter", "index"), &GraphCurve::remove_filter);
	ClassDB::bind_method(D_METHOD("test"), &GraphCurve::test);
	ClassDB::bind_method(D_METHOD("graph_load", "fr", "db", "points"), &GraphCurve::graph_load);

	ClassDB::add_property("GraphCurve", PropertyInfo(Variant::NODE_PATH, "layer_axis_path"), "set_layer_axis_path", "get_layer_axis_path");
	ClassDB::add_property("GraphCurve", PropertyInfo(Variant::COLOR, "graph_color"), "set_graph_color", "get_graph_color");
	ClassDB::add_property("GraphCurve", PropertyInfo(Variant::FLOAT, "smoothing_factor"), "set_smoothing_factor", "get_smoothing_factor");
	ClassDB::add_property("GraphCurve", PropertyInfo(Variant::BOOL, "active"), "set_active", "get_active");


	ADD_SIGNAL(
		MethodInfo(
			"filter_changed",
			PropertyInfo(Variant::INT,"filter_index"),
			PropertyInfo(Variant::FLOAT, "frequency"),
			PropertyInfo(Variant::FLOAT, "amplitude"),
			PropertyInfo(Variant::FLOAT, "q")
		)
	);
	
	//ADD_SIGNAL(MethodInfo("filter_changed_frequency", PropertyInfo(Variant::INT, "filter_index"), PropertyInfo(Variant::FLOAT, "frequency")));
	//ADD_SIGNAL(MethodInfo("filter_changed_amplitude", PropertyInfo(Variant::INT, "filter_index"), PropertyInfo(Variant::FLOAT, "amplitude")));
	//ADD_SIGNAL(MethodInfo("filter_changed_q", PropertyInfo(Variant::INT, "filter_index"), PropertyInfo(Variant::FLOAT, "q")));
}

void GraphCurve::test()
{
	

}


GraphCurve::GraphCurve()
{
	input = Input::get_singleton();
	if (Engine::get_singleton()->is_editor_hint())
		set_process_mode(PROCESS_MODE_DISABLED);
}

GraphCurve::~GraphCurve()
{
	
}

void GraphCurve::_ready()
{
	std::cout << "\nObtaining LayerAxis instance reference...";
	if (!layer_axis_path.is_empty())
	{
		std::cout << "\nLayerAxis path is valid!";
		Node* n = get_node_or_null(layer_axis_path);
		if (n != NULL)
		{
			std::cout << "\nReference to LayerAxis node is valid!";
			layer_axis_instance = Object::cast_to<LayerAxis>(n);
			std::cout << "\nSuccess!\n";
		}
		else
		{
			std::cout << "\nERROR: Couldn't find LayerAxis node\n";
		}
	}
	else
	{
		std::cout << "\nNodePath for LayerAxis is invalid!\n";
	}
	graph_init_auto(1000);
	queue_redraw();
}

void GraphCurve::_process(double delta)
{
	if (!active) return;
	Vector2 mouse_pos = get_local_mouse_position();

	if (mouse_pos.x < layer_axis_instance->x1)
		mouse_pos.x = layer_axis_instance->x1;
	if (mouse_pos.x > layer_axis_instance->x_limit)
		mouse_pos.x = layer_axis_instance->x_limit;
	if (mouse_pos.y < layer_axis_instance->y1)
		mouse_pos.y = layer_axis_instance->y1;
	if (mouse_pos.y > layer_axis_instance->y_limit)
		mouse_pos.y = layer_axis_instance->y_limit;

	if (input->is_action_just_pressed("mb_left"))
	{

		for (int i = 0; i < filters.size(); i++)
		{
			Vector2 filter_pos = layer_axis_instance->graph_to_pixels(filters[i]->get_pos());
			if (mouse_pos.distance_to(filter_pos) < 10)
			{
				grabbed_filter = i;
				std::cout << "\nGrabbed filter " << grabbed_filter << ": " << filters[i]->get_pos().x << "Hz, " << filters[i]->get_pos().y << "dB" << "\n";
				input->warp_mouse(get_global_position() + filter_pos);

				break;
			}
		}
		
	}

	if (grabbed_filter != -1)
	{
		update_filter(grabbed_filter,
			layer_axis_instance->pixels_to_graph_frequency(mouse_pos.x),
			layer_axis_instance->pixels_to_graph_amplitude(mouse_pos.y),
			filters[grabbed_filter]->get_q());

		if (input->is_action_just_released("mb_left"))
		{
			std::cout << "\nReleased filter " << grabbed_filter << ": " << filters[grabbed_filter]->get_pos().x << "Hz, " << filters[grabbed_filter]->get_pos().y << "dB" << "\n";
			emit_signal("filter_changed", grabbed_filter, filters[grabbed_filter]->get_frequency(), filters[grabbed_filter]->get_amplitude(), filters[grabbed_filter]->get_q());
			grabbed_filter = -1;
		}
	}

}

void GraphCurve::_draw()
{
	if (graph_points.size() > 5)
	{
		graph_update_screen_points();

		for (int i = 0; i < filters.size(); i++)
		{
			Vector2 pos = layer_axis_instance->graph_to_pixels(Vector2(filters[i]->get_frequency(), filters[i]->get_amplitude()));

			/*
			if (pos.distance_to(get_local_mouse_position()) < 10)
			{
				float w = 120.0f;
				float h = 60.0f;
				draw_rect(Rect2(pos.x - w/2.0, pos.y + h/2.0, w, h), Color(.1f, .1f, .1f, .5f));
				String str = String(GraphConstants::FILTER_STRINGS[i]);
				str += " filter";

				//UtilityFunctions::print(str);

			}*/

			if (active) draw_circle(pos, 7.0f, graph_color);

		}

		draw_polyline(graph_points, graph_color, 1.5f, true);
	}

	if (!active)	return;

	draw_rect(Rect2(0.0f, 0.0f, layer_axis_instance->rect_size.x, layer_axis_instance->y1_offset-1.0f), Color(0.0f, 0.0f, 0.0f));
	draw_rect(Rect2(0.0f, 0.0f, layer_axis_instance->x1_offset - 1.0f, layer_axis_instance->rect_size.y), Color(0.0f, 0.0f, 0.0f));
	draw_rect(Rect2(layer_axis_instance->x_limit, 0.0f, layer_axis_instance->rect_size.x, layer_axis_instance->rect_size.y), Color(0.0f, 0.0f, 0.0f));
	draw_rect(Rect2(0.0f, layer_axis_instance->y_limit + 1.0f, layer_axis_instance->rect_size.x, layer_axis_instance->rect_size.y), Color(0.0f, 0.0f, 0.0f));



	for (int i = layer_axis_instance->amplitude_lines_nr - 1; i >= 1; i--)
	{
		double y = layer_axis_instance->y1_offset + i * layer_axis_instance->amplitude_lines_distance;
		String txt = Variant(layer_axis_instance->range_amplitude.y - (i)*layer_axis_instance->amplitude_axis_step).stringify();
		Vector2 txt_size = layer_axis_instance->text_font->get_string_size(txt);
		Vector2 txt_offset = Vector2(-txt_size.x - 2.0f, txt_size.y / 2.0f - 2.0f);
		draw_string(layer_axis_instance->text_font, Vector2(layer_axis_instance->x1, y) + txt_offset, txt);
	}

	for (int i = 0; i < 17; i++)
	{
		float xpos = (float)layer_axis_instance->x1_offset + ((log10f(layer_axis_instance->frequency_axis_def[i])
					- log10f(layer_axis_instance->range_frequency.x)) / layer_axis_instance->range) * (float)(layer_axis_instance->x2 - layer_axis_instance->x1);
		Vector2 txt_size = layer_axis_instance->text_font->get_string_size(layer_axis_instance->frequency_axis_str[i]);
		Vector2 txt_offset = Vector2(-txt_size.x / 2, 16.0f + layer_axis_instance->y1_offset);
		draw_string(layer_axis_instance->text_font, Vector2(xpos, layer_axis_instance->y2) + txt_offset, layer_axis_instance->frequency_axis_str[i]);
	}
		
}


void GraphCurve::graph_update_screen_points()
{
	for (int k = 0; k < graph_data_filtered_frequency.size(); k++)
	{
		float fr = layer_axis_instance->graph_to_pixels_frequency(graph_data_filtered_frequency[k]);
		float db = layer_axis_instance->graph_to_pixels_amplitude(graph_data_filtered_amplitude[k]);
		graph_points[k].x = fr;
		graph_points[k].y = db;
	}
	
}

void GraphCurve::graph_apply_filters()
{
	for (int k = 0; k < graph_data_filtered_frequency.size(); k++)
	{
		float sum = 0.0f;
		for (int i = 0; i < filters.size(); i++)
		{
			sum += filters[i]->get_amplitude_at(graph_data_filtered_frequency[k]);
		}

		graph_data_filtered_amplitude[k] = graph_data_amplitude[k] + sum;
	}
	queue_redraw();
}

void GraphCurve::graph_init_auto(int points_nr)
{
	graph_points.clear();
	graph_data_frequency.clear();
	graph_data_amplitude.clear();
	graph_data_filtered_frequency.clear();
	graph_data_filtered_amplitude.clear();
	graph_frequency_points_count = 0;
	std::cout << "\nGraph init (auto)... " << points_nr << " samples";
	graph_frequency_points_nr = points_nr;
	graph_frequency_points = new float[points_nr];
	std::cout << "\nMemory for samples allocated successfully!";
	Vector2 range_frequency = Vector2(20, 20000);
	std::cout << "\nSet frequency range (" << range_frequency.x << ", " << range_frequency.y << ")";
	range_frequency = Vector2(layer_axis_instance->graph_to_pixels_frequency(range_frequency.x), layer_axis_instance->graph_to_pixels_frequency(range_frequency.y));
	std::cout << "\nConverted frequency range to screen coords (" << range_frequency.x << ", " << range_frequency.y << ")";
	
	float i = range_frequency.x;
	float s = (range_frequency.y - range_frequency.x) / points_nr;
	float pdb = layer_axis_instance->graph_to_pixels_amplitude(0.0f);

	while (i <= range_frequency.y)
	{
		float f = layer_axis_instance->pixels_to_graph_frequency(i);
		graph_data_frequency.push_back(f);
		graph_data_amplitude.push_back(0.0f);
		graph_data_filtered_frequency.push_back(f);
		graph_data_filtered_amplitude.push_back(0.0f);
		graph_frequency_points_count++;
		graph_points.append(Vector2(i, layer_axis_instance->graph_to_pixels_amplitude(0.0f)));
		i += s;
	}

	std::cout << "\nAllocated graph data successfully!\nGraph init complete!\n";

}

void GraphCurve::graph_load(PackedFloat64Array fr, PackedFloat64Array db, int points)
{
	graph_points.clear();
	graph_data_frequency.clear();
	graph_data_amplitude.clear();
	graph_data_filtered_frequency.clear();
	graph_data_filtered_amplitude.clear();
	graph_frequency_points_count = points;
	std::cout << "\nGraph init (load)... " << points << " samples";
	graph_frequency_points_nr = points;
	graph_frequency_points = new float[points];
	std::cout << "\nMemory for samples allocated successfully!";
	for (int i = 0; i < points; i++)
	{
		graph_data_frequency.push_back(fr[i]);
		graph_data_amplitude.push_back(db[i]);
		graph_data_filtered_frequency.push_back(fr[i]);
		graph_data_filtered_amplitude.push_back(db[i]);
		graph_points.append(Vector2(layer_axis_instance->graph_to_pixels_frequency(fr[i]), layer_axis_instance->graph_to_pixels_amplitude(db[i])));
	}
	std::cout << "\nLoaded graph data successfully!\nGraph init complete!\n";
}

void GraphCurve::new_filter(int type, float frequency, float amplitude, float q)
{
	std::cout << "\nAdding filter (" << frequency << ", " << amplitude << ", " << q << ");";
	BiquadFilter *filter;
	switch (type)
	{
	case(GraphConstants::PEAKING):
		filter = new Peaking(frequency, amplitude, q);
		break;
	case(GraphConstants::LOW_PASS):
		filter = new LowPass(frequency, amplitude, q);
		break;
	case(GraphConstants::HIGH_PASS):
		filter = new HighPass(frequency, amplitude, q);
		break;
	case(GraphConstants::NOTCH):
		filter = new Notch(frequency, amplitude, q);
		break;
	case(GraphConstants::BAND_PASS):
		filter = new BandPass(frequency, amplitude, q);
		break;
	case(GraphConstants::LOW_SHELF):
		filter = new LowShelf(frequency, amplitude, q);
		break;
	case(GraphConstants::HIGH_SHELF):
		filter = new HighShelf(frequency, amplitude, q);
		break;
	case(GraphConstants::ALL_PASS):
		filter = new AllPass(frequency, amplitude, q);
		break;
	default:
		std::cout << "\nError: Filter type invalid (GraphDraw/GraphCurve.cpp/new_filter)";
		return;
	}

	
	filters.push_back(filter);

	graph_apply_filters();

}

void GraphCurve::remove_filter(int index)
{
	if (index > filters.size() + 1)
	{
		std::cout << "\nCannot remove filter, invalid index!\n";
		return;
	}
	std::cout << "\nRemoving filter with index " << index << "...";
	delete filters[index];
	filters.erase(filters.begin() + index);
	std::cout << " OK";
	std::cout << "\nUpdating filtered graph...";
	graph_apply_filters();

}

void GraphCurve::update_filter(int index, float frequency, float amplitude, float q)
{
	if (index > filters.size() + 1)
	{
		std::cout << "\nCannot update filter, invalid index " << index << "!\n";
		return;
	}

	//std::cout << "\nUpdating filter with index " << index << "...";

	filters[index]->update_filter(frequency, amplitude, q);

	//std::cout << " OK\n";

	graph_apply_filters();

}

void GraphCurve::update_filter_type(int index, int new_type)
{
	std::cout << "\nChanging filter type: " << GraphConstants::FILTER_STRINGS[filters[index]->get_type()] << " -> " << GraphConstants::FILTER_STRINGS[new_type] << "... ";
	BiquadFilter* filter;
	float frequency = filters[index]->get_frequency();
	float amplitude = filters[index]->get_amplitude();
	float q = filters[index]->get_q();
	switch (new_type)
	{
	case(GraphConstants::PEAKING):
		filter = new Peaking(frequency, amplitude, q);
		break;
	case(GraphConstants::LOW_PASS):
		filter = new LowPass(frequency, amplitude, q);
		break;
	case(GraphConstants::HIGH_PASS):
		filter = new HighPass(frequency, amplitude, q);
		break;
	case(GraphConstants::NOTCH):
		filter = new Notch(frequency, amplitude, q);
		break;
	case(GraphConstants::BAND_PASS):
		filter = new BandPass(frequency, amplitude, q);
		break;
	case(GraphConstants::LOW_SHELF):
		filter = new LowShelf(frequency, amplitude, q);
		break;
	case(GraphConstants::HIGH_SHELF):
		filter = new HighShelf(frequency, amplitude, q);
		break;
	case(GraphConstants::ALL_PASS):
		filter = new AllPass(frequency, amplitude, q);
		break;
	default:
		std::cout << "\nError: Filter type invalid (GraphDraw/GraphCurve.cpp/update_filter_type)" << new_type;
		return;
	}


	filters[index] = filter;

	graph_apply_filters();
}

void GraphCurve::update_filter_frequency(int index, float frequency)
{
	if (index > filters.size() + 1)
	{
		std::cout << "\nCannot update filter, invalid index!\n";
		return;
	}

	filters[index]->update_filter_frequency(frequency);

	graph_apply_filters();
}

void GraphCurve::update_filter_amplitude(int index, float amplitude)
{
	if (index > filters.size() + 1)
	{
		std::cout << "\nCannot update filter, invalid index!\n";
		return;
	}

	filters[index]->update_filter_amplitude(amplitude);

	graph_apply_filters();
}

void GraphCurve::update_filter_q(int index, float q)
{
	if (index > filters.size() + 1)
	{
		std::cout << "\nCannot update filter, invalid index!\n";
		return;
	}

	filters[index]->update_filter_q(q);

	graph_apply_filters();
}


void GraphCurve::set_layer_axis_path(const NodePath p)
{
	layer_axis_path = p;
}

NodePath GraphCurve::get_layer_axis_path() const
{
	return layer_axis_path;
}

void GraphCurve::set_graph_color(const Color c)
{
	graph_color = c;
	queue_redraw();
}

Color GraphCurve::get_graph_color() const
{
	return graph_color;
}

void GraphCurve::set_smoothing_factor(float f)
{
	smoothing_factor = f;
	queue_redraw();
}

float GraphCurve::get_smoothing_factor()
{
	return smoothing_factor;
}

void GraphCurve::set_active(const bool value)
{
	active = value;
	queue_redraw();
}

bool GraphCurve::get_active() const
{
	return active;
}
