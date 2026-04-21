#ifndef CURVE_H
#define CURVE_H

#include "LayerAxis.h"
#include "../EQ/BiquadFilter.h"

#include <godot_cpp/classes/control.hpp>
#include <godot_cpp/classes/input.hpp>
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/variant/signal.hpp>

namespace godot
{
	class GraphCurve: public Control
	{
		GDCLASS(GraphCurve, Control);
	protected:
		static void _bind_methods();

	//private:
		std::vector<float> graph_data_frequency;
		std::vector<float> graph_data_amplitude;
		std::vector<float> graph_data_filtered_frequency;
		std::vector<float> graph_data_filtered_amplitude;
		PackedVector2Array graph_points;
		NodePath layer_axis_path;
		LayerAxis* layer_axis_instance;
		std::vector<BiquadFilter*> filters;
		int grabbed_filter = -1;

		Input *input;

		int graph_frequency_points_nr = 100;
		float* graph_frequency_points;

		int graph_frequency_points_count = 0;

		Color graph_color = Color(1.0f, 1.0f, 1.0f);

		float smoothing_factor = .5f;

		bool active = false;
		

	public:
		void test();

		void new_filter(int type, float frequency, float amplitude, float q);
		void remove_filter(int index);
		void update_filter(int index, float frequency, float amplitude, float q);
		void update_filter_type(int index, int new_type);
		void update_filter_frequency(int index, float frequency);
		void update_filter_amplitude(int index, float amplitude);
		void update_filter_q(int index, float q);

		void graph_apply_filters();
		void graph_update_screen_points();
		void graph_init_auto(int nr_points);
		void graph_load(PackedFloat64Array fr, PackedFloat64Array amp, int points);

		void set_layer_axis_path(const NodePath p);
		NodePath get_layer_axis_path() const;
		void set_graph_color(const Color c);
		Color get_graph_color() const;
		void set_smoothing_factor(float smoothing_factor);
		float get_smoothing_factor();
		void set_active(const bool value);
		bool get_active() const;

		void _ready() override;
		void _draw() override;
		void _process(double delta) override;

		GraphCurve();
		~GraphCurve();
	};
}

#endif
