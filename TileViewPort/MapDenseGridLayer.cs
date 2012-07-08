using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WinForms_display_bitmap
{
    // REFACTORING: Replaced MapDenseGridLayer with DenseGrid
    public class MapDenseGridLayer : IGridIterable
    {
        public int width  { get; set; }
        public int height { get; set; }
        private int[] grid;

        public MapDenseGridLayer(int ww, int hh, int[] contents)
        {
            if ((ww < 1) || (ww > GridUtility.max_width )) { throw new ArgumentException("invalid width"); }
            if ((hh < 1) || (hh > GridUtility.max_height)) { throw new ArgumentException("invalid height"); }
            width  = ww;
            height = hh;

            int num_elements = width * height;
            grid = new int[num_elements];

            // Empty contents leaves grid[] filled with zeroes:
            if ((contents == null) || (contents.Length == 0)) { return; }

            // However, wrong number of initializing elements probably means an error:
            if (contents.Length != num_elements) { throw new ArgumentException("wrong number of elements in contents array"); }

            // Fill the grid with the contents:
            for (int yy = min_y(); yy <= max_y(); yy++)
            {
                for (int xx = min_x(); xx <= max_x(); xx++)
                {
                    int ii = GridUtility.indexForXYW(xx, yy, width);
                    grid[ii] = contents[ii];
                } // for (xx)
            } // for (yy)
        } // MapDenseGrid()

        public int contents_at_XY(int xx, int yy)
        {
            if (xx < min_x()) { return 0; }
            if (xx > max_x()) { return 0; }
            if (yy < min_y()) { return 0; }
            if (yy > max_y()) { return 0; }

            int ii = GridUtility.indexForXYW(xx, yy, width);
            return grid[ii];
        } // contents_at_XY()

        public int set_contents_at_XY(int xx, int yy, int new_contents)
        {
            if (xx < min_x()) { return 0; }
            if (xx > max_x()) { return 0; }
            if (yy < min_y()) { return 0; }
            if (yy > max_y()) { return 0; }

            int ii = GridUtility.indexForXYW(xx, yy, width);
            grid[ii] = new_contents;
            return grid[ii];  // Return what was set
        } // set_contents_at_XY()

        public int min_x() { return 0; }
        public int min_y() { return 0; }

        public int center_x() { return width  / 2; }
        public int center_y() { return height / 2; }

        public int max_x() { return Math.Max(0, width  - 1); }
        public int max_y() { return Math.Max(0, height - 1); }

    } // class MapDenseGridLayer

} // namespace
