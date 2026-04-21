#include "LayerAxis.h"

#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#include <godot_cpp/variant/color.hpp>

using namespace godot;

void LayerAxis::_bind_methods()
{
    ClassDB::bind_method(D_METHOD("set_x1_offset", "offset"), &LayerAxis::set_x1_offset);
    ClassDB::bind_method(D_METHOD("get_x1_offset"), &LayerAxis::get_x1_offset);
    ClassDB::bind_method(D_METHOD("set_x2_offset", "offset"), &LayerAxis::set_x2_offset);
    ClassDB::bind_method(D_METHOD("get_x2_offset"), &LayerAxis::get_x2_offset);
    ClassDB::bind_method(D_METHOD("set_y1_offset", "offset"), &LayerAxis::set_y1_offset);
    ClassDB::bind_method(D_METHOD("get_y1_offset"), &LayerAxis::get_y1_offset);
    ClassDB::bind_method(D_METHOD("set_y2_offset", "offset"), &LayerAxis::set_y2_offset);
    ClassDB::bind_method(D_METHOD("get_y2_offset"), &LayerAxis::get_y2_offset);
    ClassDB::bind_method(D_METHOD("set_range_frequency", "range"), &LayerAxis::set_range_frequency);
    ClassDB::bind_method(D_METHOD("get_range_frequency"), &LayerAxis::get_range_frequency);
    ClassDB::bind_method(D_METHOD("set_range_amplitude", "range"), &LayerAxis::set_range_amplitude);
    ClassDB::bind_method(D_METHOD("get_range_amplitude"), &LayerAxis::get_range_amplitude);
    ClassDB::bind_method(D_METHOD("set_text_font", "font"), &LayerAxis::set_text_font);
    ClassDB::bind_method(D_METHOD("get_text_font"), &LayerAxis::get_text_font);
    ClassDB::bind_method(D_METHOD("set_active", "value"), &LayerAxis::set_active);
    ClassDB::bind_method(D_METHOD("get_active"), &LayerAxis::get_active);

    ClassDB::add_property("LayerAxis", PropertyInfo(Variant::INT, "x1_offset"), "set_x1_offset", "get_x1_offset");
    ClassDB::add_property("LayerAxis", PropertyInfo(Variant::INT, "x2_offset"), "set_x2_offset", "get_x2_offset");
    ClassDB::add_property("LayerAxis", PropertyInfo(Variant::INT, "y1_offset"), "set_y1_offset", "get_y1_offset");
    ClassDB::add_property("LayerAxis", PropertyInfo(Variant::INT, "y2_offset"), "set_y2_offset", "get_y2_offset");
    ClassDB::add_property("LayerAxis", PropertyInfo(Variant::VECTOR2, "range_amplitude"), "set_range_amplitude", "get_range_amplitude");
    ClassDB::add_property("LayerAxis", PropertyInfo(Variant::VECTOR2, "range_frequency"), "set_range_frequency", "get_range_frequency");
    ClassDB::add_property("LayerAxis", PropertyInfo(Variant::OBJECT, "text_font"), "set_text_font", "get_text_font");
    ClassDB::add_property("LayerAxis", PropertyInfo(Variant::BOOL, "active"), "set_active", "get_active");

}

LayerAxis::LayerAxis()
{
    
}

LayerAxis::~LayerAxis()
{

}

void LayerAxis::_process(double delta)
{
    
}

void LayerAxis::_draw()
{
    rect_size = get_rect().size;

    x1 = x1_offset;
    y1 = y1_offset;

    x2 = rect_size.x - x2_offset - x1_offset;
    y2 = rect_size.y - y2_offset - y1_offset;

    x_limit = x2 + x1_offset;
    y_limit = y2 + y1_offset;

    range = log10f(range_frequency.y) - log10f(range_frequency.x);

    amplitude_lines_nr = (range_amplitude.y - range_amplitude.x) / amplitude_axis_step;
    amplitude_lines_distance = (rect_size.y - y1_offset - y2_offset) / amplitude_lines_nr;

    for (int i = amplitude_lines_nr - 1; i >= 1; i--)
    {
        double y = y1_offset + i * amplitude_lines_distance;
        String txt = Variant(range_amplitude.y - (i)*amplitude_axis_step).stringify();
        Vector2 txt_size = text_font->get_string_size(txt);
        Vector2 txt_offset = Vector2(-txt_size.x - 2.0f, txt_size.y / 2.0f - 2.0f);
        //draw_string(text_font, Vector2(x1, y) + txt_offset, txt);
        draw_line(Vector2(x1, y), Vector2(x_limit, y), Color(.7f, .7f, .7f), 1.0f);
    }

    for (int i = 0; i < 17; i++)
    {
        float xpos = (float)x1_offset + ((log10f(frequency_axis_def[i]) - log10f(range_frequency.x)) / range) * (float)(x2 - x1);
        Vector2 txt_size = text_font->get_string_size(frequency_axis_str[i]);
        Vector2 txt_offset = Vector2(-txt_size.x / 2, 16.0f + y1_offset);
        draw_string(text_font, Vector2(xpos, y2) + txt_offset, frequency_axis_str[i]);
        draw_line(Vector2(xpos, y1), Vector2(xpos, y_limit), Color(.7f, .7f, .7f), 1.0f);
    }

    Vector2 mouse_pos = get_local_mouse_position();
    

    if (active && (mouse_pos.y > y1 && mouse_pos.y < y_limit) && (mouse_pos.x > x1 && mouse_pos.x < x_limit))
    {
        Vector2 graph_coords = pixels_to_graph(mouse_pos);
        draw_string(text_font, Vector2(x_limit + 2.0f, mouse_pos.y + 6.0f), Variant(Math::snapped(graph_coords.y, 0.01)).stringify(), HORIZONTAL_ALIGNMENT_LEFT, -1.0, 16, Color(1.0f, 1.0f, 0.0f));
        draw_string(text_font, Vector2(x_limit + 2.0f, mouse_pos.y + 22.0f), "dB", HORIZONTAL_ALIGNMENT_LEFT, -1.0, 16, Color(1.0f, 1.0f, 0.0f));

        draw_line(Vector2(x1, mouse_pos.y), Vector2(x_limit, mouse_pos.y), Color(1.0f, 1.0f, 0.0f), 1.0f);
        String txt = Variant(round(graph_coords.x)).stringify();
        Vector2 txt_size = text_font->get_string_size(txt);
        Vector2 txt_offset = Vector2(-txt_size.x / 2.0f, -4.0f);
        draw_string(text_font, Vector2(mouse_pos.x, y1) + txt_offset, txt + " Hz", HORIZONTAL_ALIGNMENT_LEFT, -1.0, 16, Color(1.0f, 1.0f, 0.0f));
        draw_line(Vector2(mouse_pos.x, y1), Vector2(mouse_pos.x, y_limit), Color(1.0f, 1.0f, 0.0f), 1.0f);
        
    }

    draw_rect(Rect2(x1, y1, x2, y2), Color(1.0f, 1.0f, 1.0f), false, 1.0f);

}

float LayerAxis::pixels_to_graph_frequency(float f)
{
    return powf(10.0f, ((f - x1_offset) * range) / (x2 - x1) + log10f(range_frequency.x));
}
float LayerAxis::pixels_to_graph_amplitude(float a)
{
    return ((((a - y1_offset) * (- range_amplitude.y + range_amplitude.x)) / (y_limit - y1) + range_amplitude.y));
}

Vector2 LayerAxis::pixels_to_graph(Vector2 pixels)
{
    float hz = pixels_to_graph_frequency(pixels.x);
    float db = pixels_to_graph_amplitude(pixels.y);
    return Vector2(hz, db);
}

float LayerAxis::graph_to_pixels_frequency(float f)
{
    return x1_offset + ((log10f(f) - log10f(range_frequency.x)) / range) * (x2 - x1);
}
float LayerAxis::graph_to_pixels_amplitude(float a)
{
    return (y1_offset + ((a - range_amplitude.y) / (- range_amplitude.y + range_amplitude.x)) * (y_limit - y1));
}

Vector2 LayerAxis::graph_to_pixels(Vector2 graph)
{
    float xpos = graph_to_pixels_frequency(graph.x);
    float ypos = graph_to_pixels_amplitude(graph.y);
    return Vector2(xpos, ypos);
}


void LayerAxis::set_x1_offset(const int offset) { x1_offset = offset; }
void LayerAxis::set_x2_offset(const int offset) { x2_offset = offset; }
void LayerAxis::set_y1_offset(const int offset) { y1_offset = offset; }
void LayerAxis::set_y2_offset(const int offset) { y2_offset = offset; }

void LayerAxis::set_range_frequency(const Vector2 range)
{
    this -> range_frequency = range;
}

void LayerAxis::set_range_amplitude(const Vector2 range)
{
    this -> range_amplitude = range;
}

void LayerAxis::set_text_font(const Ref<Font> &font) { text_font = font; }

void LayerAxis::set_active(const bool value) { active = value; }

int LayerAxis::get_x1_offset() const { return x1_offset; }
int LayerAxis::get_x2_offset() const { return x2_offset; }
int LayerAxis::get_y1_offset() const { return y1_offset; }
int LayerAxis::get_y2_offset() const { return y2_offset; }
Vector2 LayerAxis::get_range_frequency() const { return range_frequency; }
Vector2 LayerAxis::get_range_amplitude() const { return range_amplitude; }
Ref<Font> LayerAxis::get_text_font() const { return text_font; }
bool LayerAxis::get_active() const { return active; }
