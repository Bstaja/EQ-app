#ifndef GRAPHCALC_H
#define GRAPHCALC_H

#include <godot_cpp/classes/node.hpp>
#include "../GraphDraw/LayerAxis.h"

namespace godot
{
    class GraphCalc : public Node
    {
        GDCLASS(GraphCalc, Node)

    protected:
        static void _bind_methods();

    public:
        GraphCalc();
        ~GraphCalc();
        Vector2 pixels_to_graph_coords(Vector2 pos_pixels);
    };
}

#endif