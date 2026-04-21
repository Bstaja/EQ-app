#include "GraphCalc.h"

#include <godot_cpp/core/class_db.hpp>

using namespace godot;

void GraphCalc::_bind_methods()
{
	ClassDB::bind_method(D_METHOD("pixels_to_graph_coords", "pos_pixels"), &GraphCalc::pixels_to_graph_coords);
}

GraphCalc::GraphCalc()
{

}

GraphCalc::~GraphCalc()
{

}

Vector2 GraphCalc::pixels_to_graph_coords(Vector2 pos_pixels)
{
    Vector2 pos_graph = Vector2(0.0f, 0.0f);
    return pos_graph;
}