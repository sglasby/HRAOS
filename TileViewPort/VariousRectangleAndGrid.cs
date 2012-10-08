using System;

public class DenseGrid : IGridIterable {
    public int width  { get; set; }
    public int height { get; set; }
    private int[] grid;

    public DenseGrid(int ww, int hh, int fill) {
        if ((ww < 1) || (ww > GridUtility2D.max_width )) { throw new ArgumentException("invalid width");  }
        if ((hh < 1) || (hh > GridUtility2D.max_height)) { throw new ArgumentException("invalid height"); }
        width  = ww;
        height = hh;

        int num_elements = width * height;
        grid = new int[num_elements];

        if (fill == 0)
            return;  // C# already initialized this.grid with all-bits-zero, no point looping for a no-op
        this.fill(fill);
    } // DenseGrid()

    // The int[] contents version of the constructor is useful while bootstrapping,
    // but may be obsoleted once we have scripting/data persistence...
    public DenseGrid(int ww, int hh, int[] contents) :
        this(ww, hh, 0) {
        // Empty contents is OK:
        if ((contents == null) || (contents.Length == 0)) { return; }

        // However, wrong number of initializing elements probably means an error:
        int num_elements = width * height;
        if (contents.Length != num_elements) { throw new ArgumentException("wrong number of elements in contents array"); }

        // Fill the grid with the contents:
        grid = contents;  // Shallow copy is sufficient, how this is handled is one of the nice things in C#
    } // DenseGrid()

    public int contents_at_XY(int xx, int yy) {
        if (xx < min_x()) { return 0; }
        if (xx > max_x()) { return 0; }
        if (yy < min_y()) { return 0; }
        if (yy > max_y()) { return 0; }

        int ii = GridUtility2D.indexForXYW(xx, yy, width);
        return grid[ii];
    } // contents_at_XY()

    public int set_contents_at_XY(int xx, int yy, int new_contents) {
        if (xx < min_x()) { return 0; }
        if (xx > max_x()) { return 0; }
        if (yy < min_y()) { return 0; }
        if (yy > max_y()) { return 0; }

        int ii = GridUtility2D.indexForXYW(xx, yy, width);
        grid[ii] = new_contents;
        return grid[ii];  // Return what was set
    } // set_contents_at_XY()

    public int min_x() { return 0; }
    public int min_y() { return 0; }

    public int center_x() { return width / 2; }
    public int center_y() { return height / 2; }

    public int max_x() { return Math.Max(0, width - 1); }
    public int max_y() { return Math.Max(0, height - 1); }

    public void fill(int value) {
        // Fill all cells with provided value:
        for (int yy = min_y(); yy <= max_y(); yy++) {
            for (int xx = min_x(); xx <= max_x(); xx++) {
                set_contents_at_XY(xx, yy, value);
            } // for (xx)
        } // for (yy)
    } // fill()

    public static DenseGrid BlitFromAOntoB(DenseGrid from_A, int from_x, int from_y,
                                           DenseGrid to_B, int to_x, int to_y,
                                           int blit_width, int blit_height) {
        // Blit from A onto B, with as many method arguments as possible.
        if (from_A == null)
            return null;
        if (to_B == null)
            return null;

        // The from_x,y are relative to the origin of from_A.
        if (from_x < from_A.min_x())
            return null; // At least somewhat off-edge to the left
        if (from_x > from_A.max_x())
            return null; // Entirely to the right
        if (from_y < from_A.min_y())
            return null; // At least somewhat off-edge above
        if (from_y > from_A.max_y())
            return null; // Entirely below

        // The to_x,y are relative to the origin of to_B.
        if (to_x + blit_width < to_B.min_x())
            return null; // Entirely to the left
        if (to_x > to_B.max_x())
            return null; // Entirely to the right
        if (to_y + blit_height < to_B.min_y())
            return null; // Entirely above
        if (to_y > to_B.max_y())
            return null; // Entirely below

        // The blit width/height are clipped to avoid needless looping.
        // Blit calls which overlap from_A or to_B are thus harmless.
        // Indeed, calls overlapping to_B are common and ordinary,
        // one reason being that from_A and to_B are often of different sizes.
        int max_from_w = Math.Min(from_A.max_x(), from_x + blit_width);
        int max_to_w   = Math.Min(to_B.max_x(), to_x + blit_width);
        int max_width  = Math.Min(max_from_w, max_to_w);
        blit_width = GridUtility2D.Clamp(blit_width, 1, max_width);

        int max_from_h = Math.Min(from_A.max_y(), from_y + blit_height);
        int max_to_h   = Math.Min(to_B.max_y(), to_y + blit_height);
        int max_height = Math.Min(max_from_h, max_to_h);
        blit_height = GridUtility2D.Clamp(blit_height, 1, max_height);

        // Iterate over the cells of from_A, and copy non-blank cells onto to_B:
        for (int yy = 0; yy <= blit_height; yy++) {
            for (int xx = 0; xx <= blit_width; xx++) {
                // To find the from/to blit coordinates,
                // add in the x,y offsets for from_A and to_B:
                int from_ii = GridUtility2D.indexForXYW(from_x + xx, from_y + yy, from_A.width);
                int   to_ii = GridUtility2D.indexForXYW(to_x + xx, to_y + yy, to_B.width);

                int contents = from_A.contents_at_XY(from_x + xx, from_y + yy);
                if (contents != 0) {
                    // Non-zero, not a blank cell (blank cells are skipped)
                    to_B.set_contents_at_XY(to_x + xx, to_y + yy, contents);
                }
            } // for (xx)
        } // for (yy)

        return to_B;  // to_B has been modified
    } // BlitFromAOntoB(from_A,x,y, to_B,x,y, ww,hh)

    public static DenseGrid BlitFromAOntoB(DenseGrid from_A, DenseGrid to_B,
                                           int to_x, int to_y, int blit_width, int blit_height) {
        // Blit the specified region of A 
        //     (from origin of A with the specified blit width,heigth) 
        // onto B at the specified (x,y)
        return DenseGrid.BlitFromAOntoB(from_A, 0, 0, to_B, to_x, to_y, blit_width, blit_height);
    } // BlitFromAOntoB(from_A, to_B,x,y, ww,hh)

    public static DenseGrid BlitFromAOntoB(DenseGrid from_A, DenseGrid to_B, int to_x, int to_y) {
        // Blit the entirety of A onto B at the specified (x,y)
        return DenseGrid.BlitFromAOntoB(from_A, 0, 0, to_B, to_x, to_y, from_A.width, from_A.height);
    } // BlitFromAOntoB(from_A, to_B,x,y, ww,hh)

    private static int canonicalize_rotation(int rotation) {
        rotation = rotation % 360;  // -359..-1, 0, 1..359
        if (rotation < 0) {
            rotation += 360;  // -90 --> 270, -180 --> 180, -270 --> 90
        }
        rotation = (rotation / 90) * 90;  // 1 --> 0, 89 --> 90
        return rotation;
    } // canonicalize_rotation()

    public DenseGrid RotateResult(int rotation) {
        DenseGrid new_grid;
        rotation = canonicalize_rotation(rotation);

        switch (rotation) {
            case 0:
                new_grid = this.Rotate000();
                break;
            case 90:
                new_grid = this.Rotate090();
                break;
            case 180:
                new_grid = this.Rotate180();
                break;
            case 270:
                new_grid = this.Rotate270();
                break;
            default:
                throw new ArgumentException("Impossible: Got strange rotation\n");
        }
        return new_grid;
    } // RotateResult()

    public DenseGrid Rotate000() {
        DenseGrid new_grid = new DenseGrid(this.width, this.height, 0);
        DenseGrid.BlitFromAOntoB(this, new_grid, 0, 0);
        return new_grid;
    } // Rotate000()

    public DenseGrid Rotate090() {
        DenseGrid new_grid = new DenseGrid(this.height, this.width, 0);
        for (int yy = this.min_y(); yy <= this.max_y(); yy++) {
            for (int xx = this.min_x(); xx <= this.max_x(); xx++) {
                int dest_x = this.height - (yy + 1);
                int dest_y = xx;
                int value  = this.contents_at_XY(xx, yy);
                new_grid.set_contents_at_XY(dest_x, dest_y, value);
            } // for(xx)
        } // for(yy)
        return new_grid;
    } // Rotate090()

    public DenseGrid Rotate180() {
        DenseGrid new_grid = new DenseGrid(this.width, this.height, 0);
        for (int yy = this.min_y(); yy <= this.max_y(); yy++) {
            for (int xx = this.min_x(); xx <= this.max_x(); xx++) {
                int dest_x = this.width - (xx + 1);
                int dest_y = this.height - (yy + 1);
                int value  = this.contents_at_XY(xx, yy);
                new_grid.set_contents_at_XY(dest_x, dest_y, value);
            } // for(xx)
        } // for(yy)
        return new_grid;
    } // Rotate180()

    public DenseGrid Rotate270() {
        DenseGrid new_grid = new DenseGrid(this.height, this.width, 0);
        for (int yy = this.min_y(); yy <= this.max_y(); yy++) {
            for (int xx = this.min_x(); xx <= this.max_x(); xx++) {
                int dest_x = yy;
                int dest_y = this.width - (xx + 1);
                int value  = this.contents_at_XY(xx, yy);
                new_grid.set_contents_at_XY(dest_x, dest_y, value);
            } // for(xx)
        } // for(yy)
        return new_grid;
    } // Rotate270()

    public DenseGrid Flip_WE() {
        DenseGrid new_grid = new DenseGrid(this.width, this.height, 0);
        DenseGrid.BlitFromAOntoB(this, new_grid, 0, 0);

        // Starting with the outmost and working towards center,
        // swap the contents of each paired vertical strip.
        for (int W_xx = 0; W_xx < new_grid.center_x(); W_xx++)  // VERIFY: is this correct for even widths?
            {
            int E_xx = new_grid.max_x() - W_xx;
            for (int yy = 0; yy <= new_grid.max_y(); yy++) {
                int W_value = contents_at_XY(W_xx, yy);
                int E_value = contents_at_XY(E_xx, yy);
                new_grid.set_contents_at_XY(W_xx, yy, E_value);
                new_grid.set_contents_at_XY(E_xx, yy, W_value);
            } // for(yy)
        } // for(xx)
        return new_grid;
    } // Flip_WE()

    public DenseGrid Flip_NS() {
        DenseGrid new_grid = new DenseGrid(this.width, this.height, 0);
        DenseGrid.BlitFromAOntoB(this, new_grid, 0, 0);

        // Starting with the outmost and working towards center,
        // swap the contents of each paired horizontal strip.
        for (int N_yy = 0; N_yy < new_grid.center_y(); N_yy++)  // VERIFY: is this correct for even heights?
            {
            int S_yy = new_grid.max_y() - N_yy;
            for (int xx = 0; xx <= new_grid.max_x(); xx++) {
                int N_value = contents_at_XY(xx, N_yy);
                int S_value = contents_at_XY(xx, S_yy);
                new_grid.set_contents_at_XY(xx, N_yy, S_value);
                new_grid.set_contents_at_XY(xx, S_yy, N_value);
            } // for(yy)
        } // for(xx)
        return new_grid;
    } // Flip_NS()

    public bool intersects(int xx, int yy, int ww, int hh) {
        Rect bounds = new Rect(0, 0, width, height);
        return bounds.intersects(xx, yy, ww, hh);
    } // intersects(x,y,w,h)

} // class Grid