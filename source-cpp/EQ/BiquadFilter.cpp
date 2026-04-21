#include "BiquadFilter.h"

void BiquadFilter::update_filter(float frequency, float amplitude, float q)
{
	this->frequency = frequency;
	this->amplitude = amplitude;
	this->q = q;
	SR = 48000.0;
	A = pow(10.0, amplitude / 40.0);
	W0 = 2.0 * Math_PI * frequency / SR;
	sinW0 = sin(W0);
	cosW0 = cos(W0);
	alpha = sinW0 / (2 * q);

	calculate_coefficients();
	normalize_coefficients();
}

void BiquadFilter::update_filter_frequency(float frequency)
{
	this->frequency = frequency;
	W0 = 2.0 * Math_PI * frequency / SR;
	sinW0 = sin(W0);
	cosW0 = cos(W0);
	alpha = sinW0 / (2 * q);

	calculate_coefficients();
	normalize_coefficients();
}

void BiquadFilter::update_filter_amplitude(float amplitude)
{
	this->amplitude = amplitude;
	A = pow(10.0, amplitude / 40.0);

	calculate_coefficients();
	normalize_coefficients();
}

void BiquadFilter::update_filter_q(float q)
{
	this->q = q;
	alpha = sinW0 / (2 * q);

	calculate_coefficients();
	normalize_coefficients();
}

float BiquadFilter::get_amplitude_at(float frequency)
{
	w = 2.0 * Math_PI * frequency / SR;

	return (10.0 * log10(abs((b0 * b0 + b1 * b1 + b2 * b2 + 2.0f * (b0 * b1 + b1 * b2) * cos(w) + 2.0 * b0 * b2 * cos(2.0 * w)) /
		   (1.0 + a1 * a1 + a2 * a2 + 2.0 * (a1 + a1 * a2) * cos(w) + 2.0 * a2 * cos(2.0 * w)))));
}

void BiquadFilter::normalize_coefficients()
{
	b0 /= a0;
	b1 /= a0;
	b2 /= a0;
	a1 /= a0;
	a2 /= a0;
	a0 = 1.0;
}

float BiquadFilter::get_frequency() { return frequency; }
float BiquadFilter::get_amplitude() { return amplitude; }
float BiquadFilter::get_q()			{ return q; }

int BiquadFilter::get_type()		{ return type; }

godot::Vector2 BiquadFilter::get_pos() { return godot::Vector2(frequency, amplitude); }

void Peaking::calculate_coefficients()
{
	b0 = 1.0 + alpha * A;
	b1 = -2.0 * cosW0;
	b2 = 1.0 - alpha * A;
	a0 = 1.0 + alpha / A;
	a1 = -2.0 * cosW0;
	a2 = 1.0 - alpha / A;
}

Peaking::Peaking(float frequency, float amplitude, float q)
{
	type = GraphConstants::PEAKING;

	update_filter(frequency, amplitude, q);
}

void LowPass::calculate_coefficients() 
{
	b0 = (1.0 - cosW0) / 2.0;
	b1 = 1.0 - cosW0;
	b2 = b0;
	a0 = 1.0 + alpha;
	a1 = -2.0 * cosW0;
	a2 = 1.0 - alpha;
}

LowPass::LowPass(float frequency, float amplitude, float q)
{
	type = GraphConstants::LOW_PASS;

	update_filter(frequency, amplitude, q);
}

void HighPass::calculate_coefficients()
{
	b0 = (1.0 + cosW0) / 2.0;
	b1 = -1.0 - cosW0;
	b2 = b0;
	a0 = 1.0 + alpha;
	a1 = -2.0 * cosW0;
	a2 = 1.0 - alpha;
}

HighPass::HighPass(float frequency, float amplitude, float q)
{
	type = GraphConstants::HIGH_PASS;

	update_filter(frequency, amplitude, q);
}

void BandPass::calculate_coefficients()
{
	b0 = sinW0 / 2.0;
	b1 = 0;
	b2 = -sinW0 / 2.0;
	a0 = 1.0 + alpha;
	a1 = -2.0 * cosW0;
	a2 = 1.0 - alpha;
}

BandPass::BandPass(float frequency, float amplitude, float q)
{
	type = GraphConstants::BAND_PASS;

	update_filter(frequency, amplitude, q);
}

void Notch::calculate_coefficients()
{
	b0 = 1.0;
	b1 = -2.0 * cosW0;
	b2 = 1.0;
	a0 = 1.0 + alpha;
	a1 = -2.0 * cosW0;
	a2 = 1.0 - alpha;
}

Notch::Notch(float frequency, float amplitude, float q)
{
	type = GraphConstants::NOTCH;

	update_filter(frequency, amplitude, q);
}

void AllPass::calculate_coefficients()
{
	b0 = 1.0 - alpha;
	b1 = -2.0 * cosW0;
	b2 = 1.0 + alpha;
	a0 = b2;
	a1 = -2.0 * cosW0;
	a2 = b0;
}

AllPass::AllPass(float frequency, float amplitude, float q)
{
	type = GraphConstants::ALL_PASS;

	update_filter(frequency, amplitude, q);
}

void LowShelf::calculate_coefficients()
{
	double sqrtA = 2.0 * sqrt(A) * alpha;

	b0 = A * ((A + 1.0) - (A - 1.0) * cosW0 + sqrtA);
	b1 = 2.0 * A * ((A - 1.0) - (A + 1.0) * cosW0);
	b2 = A * ((A + 1.0) - (A - 1.0) * cosW0 - sqrtA);
	a0 = (A + 1.0) + (A - 1.0) * cosW0 + sqrtA;
	a1 = -2.0 * ((A - 1) + (A + 1) * cosW0);
	a2 = (A + 1.0) + (A - 1.0) * cosW0 - sqrtA;
}

LowShelf::LowShelf(float frequency, float amplitude, float q)
{
	type = GraphConstants::LOW_SHELF;

	update_filter(frequency, amplitude, q);
}

void HighShelf::calculate_coefficients()
{
	double sqrtA = 2.0 * sqrt(A) * alpha;

	b0 = A * ((A + 1.0) + (A - 1.0) * cosW0 + sqrtA);
	b1 = -2.0 * A * ((A - 1.0) + (A + 1.0) * cosW0);
	b2 = A * ((A + 1.0) + (A - 1.0) * cosW0 - sqrtA);
	a0 = (A + 1.0) - (A - 1.0) * cosW0 + sqrtA;
	a1 = 2.0 * ((A - 1) - (A + 1) * cosW0);
	a2 = (A + 1.0) - (A - 1.0) * cosW0 - sqrtA;
}

HighShelf::HighShelf(float frequency, float amplitude, float q)
{
	type = GraphConstants::HIGH_SHELF;

	update_filter(frequency, amplitude, q);
}


