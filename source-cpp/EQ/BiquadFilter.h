#ifndef BIQUAD_H
#define BIQUAD_H

#include <godot_cpp/core/math.hpp>
#include <godot_cpp/variant/vector2.hpp>
#include <iostream>
#include "../Utilities/GraphConstants.h"


class BiquadFilter
{
protected:
	int type;

	float amplitude;
	float frequency;
	float q;

	double SR;
	double A;
	double W0;
	double sinW0;
	double cosW0;
	double alpha;

	double b0;
	double b1;
	double b2;
	double a0;
	double a1;
	double a2;

	double w;

public:
	void update_filter(float frequency, float amplitude, float q);
	void update_filter_frequency(float frequency);
	void update_filter_amplitude(float amplitude);
	void update_filter_q(float q);
	float get_amplitude_at(float frequency);
	virtual void calculate_coefficients()
	{
		std::cout << "\nCalculating coefficients failed, called from base class BiquadFilter instead of child.\n";
	};
	void normalize_coefficients();

	float get_frequency();
	float get_amplitude();
	float get_q();

	int get_type();

	godot::Vector2 get_pos();
	BiquadFilter() {};
};

class Peaking: public BiquadFilter
{

public:
	Peaking(float frequency, float amplitude, float q);
	void calculate_coefficients() override;

};

class LowPass : public BiquadFilter
{

public:
	LowPass(float frequency, float amplitude, float q);
	void calculate_coefficients() override;

};

class HighPass : public BiquadFilter
{

public:
	HighPass(float frequency, float amplitude, float q);
	void calculate_coefficients() override;

};

class BandPass : public BiquadFilter
{

public:
	BandPass(float frequency, float amplitude, float q);
	void calculate_coefficients() override;

};

class Notch : public BiquadFilter
{

public:
	Notch(float frequency, float amplitude, float q);
	void calculate_coefficients() override;

};

class AllPass : public BiquadFilter
{

public:
	AllPass(float frequency, float amplitude, float q);
	void calculate_coefficients() override;

};

class LowShelf : public BiquadFilter
{

public:
	LowShelf(float frequency, float amplitude, float q);
	void calculate_coefficients() override;

};

class HighShelf : public BiquadFilter
{

public:
	HighShelf(float frequency, float amplitude, float q);
	void calculate_coefficients() override;

};



#endif