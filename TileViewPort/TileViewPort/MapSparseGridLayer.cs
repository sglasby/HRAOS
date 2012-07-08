using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


    class MapSparseGridLayer : IGridIterable
    {
        public int width  { get; set; }
        public int height { get; set; }
        private Dictionary<int, int> grid_dict;

        public MapSparseGridLayer(int ww, int hh, Dictionary<int, int> contents)
        {
            if ((ww < 1) || (ww > GridUtility.max_width )) { throw new ArgumentException("invalid width"); }
            if ((hh < 1) || (hh > GridUtility.max_height)) { throw new ArgumentException("invalid height"); }
            width  = ww;
            height = hh;

            //int num_elements = width * height;
            grid_dict = new Dictionary<int, int>();

            // Empty contents is fine:
            if ((contents == null) || (contents.Keys.Count == 0)) { return; }

            // Fill the grid_dict with any contents:
            foreach (int XY_key in contents.Keys)
            {
                // XY_key == (y * max_width) + x
                // thus (x,y) of (2,3) --> (256*3 + 2) == 770
                //int xx = XY_key % GridUtility.max_width;
                //int yy = XY_key / GridUtility.max_width;
                grid_dict[XY_key] = contents[XY_key];
            }
        } // MapSparseGridLayer()

        public int contents_at_XY(int xx, int yy)
        {
            if (xx < min_x()) { return 0; }
            if (xx > max_x()) { return 0; }
            if (yy < min_y()) { return 0; }
            if (yy > max_y()) { return 0; }

            int XY_key = (yy * GridUtility.max_height) + xx;
            int contents = 0;
            grid_dict.TryGetValue(XY_key, out contents);
            return contents;
        } // contents_at_XY()

        public int set_contents_at_XY(int xx, int yy, int new_contents)
        {
            if (xx < min_x()) { return 0; }
            if (xx > max_x()) { return 0; }
            if (yy < min_y()) { return 0; }
            if (yy > max_y()) { return 0; }

            int XY_key = (yy * GridUtility.max_height) + xx;
            grid_dict[XY_key] = new_contents;
            return grid_dict[XY_key];  // Return what was set
        } // set_contents_at_XY()

        public int min_x() { return 0; }
        public int min_y() { return 0; }

        public int center_x() { return width / 2; }
        public int center_y() { return height / 2; }

        public int max_x() { return Math.Max(0, width - 1); }
        public int max_y() { return Math.Max(0, height - 1); }

    } // class MapSparseGridLayer
