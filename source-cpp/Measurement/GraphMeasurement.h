#ifndef GRAPH_MEASUREMENT
#define GRAPH_MEASUREMENT

#include "GraphDraw/GraphCurve.h"

#include <iostream>
#include <fstream>
#include <vector>
#include <random>
#include <cstdint>

#include <godot_cpp/classes/audio_bus_layout.hpp>
#include <godot_cpp/classes/audio_stream_generator.hpp>
#include <godot_cpp/classes/audio_stream_wav.hpp>
#include <godot_cpp/classes/audio_stream_generator_playback.hpp>
#include <godot_cpp/classes/audio_server.hpp>
#include <godot_cpp/classes/audio_effect_record.hpp>
#include <godot_cpp/classes/audio_effect_spectrum_analyzer.hpp>
#include <godot_cpp/classes/audio_effect_spectrum_analyzer_instance.hpp>


namespace godot {

class GraphMeasurement : public GraphCurve
{
	GDCLASS(GraphMeasurement, GraphCurve);

protected:
	static void _bind_methods();

private:
	enum AudioInputEffects
	{
		SPECTRUM_ANALYZER,
		RECORDER,
	};

	enum AudioBuses
	{
		MASTER_BUS,
		OUTPUT_BUS,
		INPUT_BUS
	};

	Ref<AudioEffectRecord> audio_recording;
	Ref<AudioEffectSpectrumAnalyzerInstance> audio_spectrum;
	AudioBusLayout audio_bus;
	AudioServer *audio_server;
	std::vector<float> measurement_data_frequency;
	std::vector<float> measurement_data_amplitude;

	bool is_measuring = false;

public:
	void _process(double delta) override;
	GraphMeasurement();
	~GraphMeasurement();
	
	void reinit();
	void start_measurement(float duration);
	Array end_measurement();

	void generate_white_noise(std::vector<int16_t>& buffer, int durationSeconds, int sampleRate, int numChannels);
	void write_wav_file(const std::string& filename, const std::vector<int16_t>& buffer, int sampleRate, int numChannels);
};

}







#endif // GRAPH_MEASUREMENT
