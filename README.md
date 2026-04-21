---

# Audio Data Analysis & EQ

A powerful graphical interface designed for the precise visualization, manipulation, and interpretation of audio data. This application allows users to analyze frequency responses, apply parametric filters, and perform real-time audio measurements.

---

![Image](https://github.com/Bstaja/EQ-app/blob/main/imgs/image29.png)
![Image](https://github.com/Bstaja/EQ-app/blob/main/imgs/image30.png)

## Main Functionalities

### 1. Data Handling & I/O
* **CSV Support:** Import and export measurement data via standardized CSV files (Frequency/Amplitude columns).
* **Parametric Filter Loading:** Support for non-standard formats to load complex, custom filter configurations.
* **Data Export:** Easily share processed results or analysis data for use in external applications.

### 2. Advanced Graphing & Visualization
* **Logarithmic Scaling:** Accurate horizontal axis scaling for frequency (Hz) and vertical scaling for amplitude (dB).
* **Interactive Cursor:** Real-time yellow guide lines track the mouse cursor to provide precise coordinate values at any point on the graph.
* **Layer Management:** Customize graph colors, rename layers, and toggle visibility or deletion for efficient multi-graph management.

### 3. Parametric Biquad Filters
* **Real-Time Processing:** Apply parametric filters based on the **Biquad** architecture (supporting frequency, gain, and Q-factor).
* **Visual Manipulation:** Adjust filters directly on the graph by dragging control points or using the dedicated sidebar menu.
* **Dynamic Updating:** The application instantly recalculates and redraws the filtered curve whenever parameters are modified.

### 4. Audio Measurements & Calibration
* **Real-Time Capture:** Select input/output devices to perform live measurements using test signals like **white noise**.
* **Microphone Calibration:** Load calibration files to compensate for hardware errors in real-time.
* **Comparative Analysis:** Overlay measurements with imported reference data (e.g., professional industry benchmarks) for comparison.
