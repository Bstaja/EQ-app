class GraphConstants
{
public:
	static const int PEAKING = 0;
	static const int LOW_PASS = 1;
	static const int HIGH_PASS = 2;
	static const int BAND_PASS = 3;
	static const int NOTCH = 4;
	static const int ALL_PASS = 5;
	static const int LOW_SHELF = 6;
	static const int HIGH_SHELF = 7;
	inline static const char* FILTER_STRINGS[] = {"Peaking", "Low Pass", "High Pass", "Band Pass", "Notch", "All Pass", "Low Shelf", "High Shelf"};
};