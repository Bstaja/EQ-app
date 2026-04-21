#include "GraphMeasurement.h"
#include <godot_cpp/variant/utility_functions.hpp>

using namespace godot;

void GraphMeasurement::_bind_methods()
{
	ClassDB::bind_method(D_METHOD("reinit"), &GraphMeasurement::reinit);
	ClassDB::bind_method(D_METHOD("start_measurement", "duration"), &GraphMeasurement::start_measurement);
	ClassDB::bind_method(D_METHOD("end_measurement"), &GraphMeasurement::end_measurement);

}

GraphMeasurement::GraphMeasurement()
{

	if (Engine::get_singleton()->is_editor_hint())
	{
		set_process_mode(PROCESS_MODE_DISABLED);
		return;
	}

	std::cout << "\nPreparing for measurements...\n";

	audio_server = AudioServer::get_singleton();

	if (audio_server == NULL)
	{
		std::cout << "Failed to get reference to AudioServer!\n";
		queue_free();
		return;
	}

	std::cout << "Found AudioServer\n";

	UtilityFunctions::printraw("Found input devices:");
	UtilityFunctions::printraw(audio_server->get_input_device_list());
	UtilityFunctions::printraw("\nFound output devices:");
	UtilityFunctions::printraw(audio_server->get_output_device_list());

	/*
	audio_recording = audio_server->get_bus_effect(INPUT_BUS, AudioInputEffects::RECORDER);
	if (audio_recording == NULL)
	{
		std::cout << "\nFailed to get audio recorder!\n";
		queue_free();
		return;
	}

	std::cout << "\nFound AudioEffectRecording\n";
	*/

	audio_spectrum = audio_server->get_bus_effect_instance(audio_server->get_bus_index("AudioInput"), AudioInputEffects::SPECTRUM_ANALYZER);
	if (audio_spectrum == NULL)
	{
		std::cout << "Failed to get audio spectrum analyzer!\n";
		queue_free();
		return;
		
	}

	std::cout << "Found AudioSpectrumAnalyzer\n";

	std::cout << "Success!\n";
		
}
GraphMeasurement::~GraphMeasurement()
{

}

void GraphMeasurement::_process(double delta)
{
	graph_data_filtered_amplitude[0] = Math::linear2db(audio_spectrum->get_magnitude_for_frequency_range(graph_data_frequency[0], graph_data_frequency[1], AudioEffectSpectrumAnalyzerInstance::MAGNITUDE_AVERAGE).length()) + 60.0f;
	for (int i = 1; i < graph_data_frequency.size() - 1; i++)
	{
		graph_data_filtered_amplitude[i] = Math::linear2db(audio_spectrum->get_magnitude_for_frequency_range(graph_data_frequency[i - 1], graph_data_frequency[i + 1], AudioEffectSpectrumAnalyzerInstance::MAGNITUDE_AVERAGE).length()) + 60.0f;
		graph_data_filtered_amplitude[i] = graph_data_filtered_amplitude[i - 1] + smoothing_factor * (graph_data_filtered_amplitude[i] - graph_data_filtered_amplitude[i - 1]);
	}
	graph_data_filtered_amplitude[graph_data_frequency.size() - 1] = graph_data_filtered_amplitude[graph_data_frequency.size() - 2] + smoothing_factor * (graph_data_filtered_amplitude[graph_data_frequency.size() - 1] - graph_data_filtered_amplitude[graph_data_frequency.size() - 2]);
	graph_data_filtered_amplitude[graph_data_frequency.size() - 1] = Math::linear2db(audio_spectrum->get_magnitude_for_frequency_range(graph_data_frequency[graph_data_frequency.size() - 2], graph_data_frequency[graph_data_frequency.size() - 1], AudioEffectSpectrumAnalyzerInstance::MAGNITUDE_AVERAGE).length()) + 60.0f;

	if (is_measuring)
	{
		for (int i = 0; i < graph_data_filtered_amplitude.size(); i++)
		{
			//if (measurement_data_amplitude[i] < graph_data_filtered_amplitude[i])
			//{
				measurement_data_amplitude[i] *= 5;
				measurement_data_amplitude[i] += graph_data_filtered_amplitude[i];
				measurement_data_amplitude[i] /= 6;
				measurement_data_frequency[i] = graph_data_filtered_frequency[i];
			//}
		}
	}

	queue_redraw();
}

void GraphMeasurement::reinit()
{
	GraphMeasurement();
}

void GraphMeasurement::start_measurement(float duration)
{
	const int sampleRate = 96000;
	const int durationSeconds = duration;
	const int numChannels = 2;

	std::vector<int16_t> buffer;
	generate_white_noise(buffer, durationSeconds, sampleRate, numChannels);

	write_wav_file("D:\\Facultate\\Licenta\\TestStuff\\whitenoise.wav", buffer, sampleRate, numChannels);

	std::cout << "White noise WAV file generated: whitenoise.wav" << std::endl;

	measurement_data_amplitude.clear();
	measurement_data_frequency.clear();
	measurement_data_amplitude.resize(graph_data_filtered_amplitude.size());
	measurement_data_frequency.resize(graph_data_filtered_frequency.size());
	for (int i = 0; i < measurement_data_amplitude.size(); i++)
	{
		measurement_data_amplitude[i] = -500.0;
	}
	is_measuring = true;
}

Array GraphMeasurement::end_measurement()
{
	is_measuring = false;
	PackedFloat64Array fr = PackedFloat64Array();
	PackedFloat64Array db = PackedFloat64Array();
	Array arr = Array();

	for (int i = 0; i < measurement_data_frequency.size(); i++)
	{
		fr.append(measurement_data_frequency[i]);
		db.append(measurement_data_amplitude[i]);
	}

	arr.append(fr);
	arr.append(db);

	return arr;
}


void GraphMeasurement::generate_white_noise(std::vector<int16_t>& buffer, int durationSeconds, int sampleRate, int numChannels) {
	std::random_device rd;
	std::mt19937 gen(rd());
	std::uniform_int_distribution<int16_t> dis(-32768, 32767);

	size_t numSamples = sampleRate * durationSeconds * numChannels;
	buffer.resize(numSamples);
	for (size_t i = 0; i < numSamples; ++i) {
		buffer[i] = dis(gen);
	}
}

void GraphMeasurement::write_wav_file(const std::string& filename, const std::vector<int16_t>& buffer, int sampleRate, int numChannels) {
	std::ofstream outFile(filename, std::ios::binary);

	// WAV file header
	outFile.write("RIFF", 4);
	uint32_t fileSize = 36 + buffer.size() * sizeof(int16_t);
	outFile.write(reinterpret_cast<const char*>(&fileSize), 4);
	outFile.write("WAVE", 4);
	outFile.write("fmt ", 4);

	uint32_t fmtChunkSize = 16;
	uint16_t audioFormat = 1; // PCM
	uint16_t blockAlign = numChannels * sizeof(int16_t);
	uint32_t byteRate = sampleRate * blockAlign;
	uint16_t bitsPerSample = 16;

	outFile.write(reinterpret_cast<const char*>(&fmtChunkSize), 4);
	outFile.write(reinterpret_cast<const char*>(&audioFormat), 2);
	outFile.write(reinterpret_cast<const char*>(&numChannels), 2);
	outFile.write(reinterpret_cast<const char*>(&sampleRate), 4);
	outFile.write(reinterpret_cast<const char*>(&byteRate), 4);
	outFile.write(reinterpret_cast<const char*>(&blockAlign), 2);
	outFile.write(reinterpret_cast<const char*>(&bitsPerSample), 2);

	outFile.write("data", 4);
	uint32_t dataChunkSize = buffer.size() * sizeof(int16_t);
	outFile.write(reinterpret_cast<const char*>(&dataChunkSize), 4);
	outFile.write(reinterpret_cast<const char*>(buffer.data()), dataChunkSize);

	outFile.close();
}
