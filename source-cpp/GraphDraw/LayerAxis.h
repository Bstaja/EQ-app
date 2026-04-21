#ifndef LAYERAXIS_H
#define LAYERAXIS_H

#include <godot_cpp/classes/control.hpp>
#include <godot_cpp/classes/font.hpp>

namespace godot
{
    class LayerAxis : public Control
    {
        GDCLASS(LayerAxis, Control)

    protected:
        static void _bind_methods();
        

    public:
        //float x1 = 32;
        float x2 = 100;
        //float y1 = 16;
        float y2 = 100;
        float x1_offset = 32;
        float x2_offset = 16;
        float y1_offset = 16;
        float y2_offset = 16;
        
        int amplitude_axis_step = 5;
        Vector2 range_frequency = Vector2(15, 20000);
        Vector2 range_amplitude = Vector2(-30, 30);
        Vector2 rect_size = get_rect().size;

        int amplitude_lines_nr = (range_amplitude.y - range_amplitude.x) / amplitude_axis_step;
        double amplitude_lines_distance = (rect_size.y - y1_offset - y2_offset) / amplitude_lines_nr;

        Vector2 scale = Vector2(1, 1);
        Vector2 offset = Vector2(0, 0);
        Vector2 max_offset = Vector2(0, 0);
        
        float range = log10f(range_frequency.y) - log10f(range_frequency.x);
        const int frequency_axis_def[17] = { 20, 30, 40, 50, 100, 200, 300, 400, 500, 1000, 2000, 3000, 4000, 5000, 10000, 15000, 20000 };
        const String frequency_axis_str[17] = { "20", "30", "40", "50", "100", "200", "300", "400", "500", "1k", "2k", "3k", "4k", "5k", "10k", "15k", "20k" };

    
        bool active = true;

        float x_limit = x2 + x1_offset;
        float y_limit = y2 + y1_offset;
        float x1 = 32;
        float y1 = 16;
        Ref<Font> text_font;
        LayerAxis();
        ~LayerAxis();

        void _process(double delta) override;
        void _draw() override;

        void set_x1_offset(const int offset);
        void set_x2_offset(const int offset);
        void set_y1_offset(const int offset);
        void set_y2_offset(const int offset);
        void set_range_frequency(const Vector2 range);
        void set_range_amplitude(const Vector2 range);
        void set_text_font(const Ref<Font> &font);
        void set_active(const bool value);

        int get_x1_offset() const;
        int get_x2_offset() const;
        int get_y1_offset() const;
        int get_y2_offset() const;
        Vector2 get_range_frequency() const;
        Vector2 get_range_amplitude() const;
        Ref<Font> get_text_font() const;
        bool get_active() const;


        float pixels_to_graph_frequency(float f);
        float pixels_to_graph_amplitude(float a);

        Vector2 pixels_to_graph(Vector2 pixels);

        float graph_to_pixels_frequency(float f);
        float graph_to_pixels_amplitude(float a);

        Vector2 graph_to_pixels(Vector2 graph);
        
    };
}

#endif