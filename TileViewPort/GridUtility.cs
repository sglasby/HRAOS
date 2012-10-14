// TODO:
// Perhaps reorganize this in some way...
// Seems messy to have two so-similar classes

public static class GridUtility2D {
    // Some useful constants:
    public const int max_width  = 256;
    public const int max_height = 256;

    // Functions for a 2D grid, useful for converting (X,Y,width) into (index), and vice-versa:
    public static int indexForXYW(int xx, int yy, int ww) { return (ww * yy) + xx; }
    public static int XforIW(int ii, int ww) { return ii % ww; }
    public static int YforIW(int ii, int ww) { return ii / ww; }

    public static int Clamp(int value, int min, int max) {
        if (value <= min) { return min; }
        if (value >= max) { return max; }
        return value;
    } // Clamp()

} // class GridUtility

public static class GridUtility3D {
    // Functions for a 3D grid, useful for converting (X,Y,Z,width,height) into (index), and vice-versa:
    public static int indexForXYZWH(int xx, int yy, int zz, int ww, int hh) { return (ww * hh * zz) + (ww * yy) + xx; }
    public static int XforIWH(int ii, int ww, int hh) { return  ii %  ww; }
    public static int YforIWH(int ii, int ww, int hh) { return (ii % (ww * hh)) / ww; }
    public static int ZforIWH(int ii, int ww, int hh) { return  ii / (ww * hh); }
}
