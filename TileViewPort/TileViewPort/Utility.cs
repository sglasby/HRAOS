public static class Utility {

    public static int clamp(int min, int max, int value) {
        // TODO: Various versions of this exist, such as GridUtility.Clamp(), should be unified...
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }

} // class