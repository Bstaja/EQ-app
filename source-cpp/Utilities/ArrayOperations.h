
template<typename T>
T* resize_array(T* initial_array, int current_size, int new_size) {
    T* new_array = new T[new_size];

    int nr_elements_copy = (current_size < new_size) ? current_size : new_size;
    for (int i = 0; i < nr_elements_copy; i++) {
        new_array[i] = initial_array[i];
    }

    // Delete the original array
    delete[] initial_array;

    return new_array;
}