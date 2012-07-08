using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;


namespace WinForms_display_bitmap
{
    public class MapCompositedLayer : IGridIterable
    {
        public int width  { get; set; }
        public int height { get; set; }

        public int next_rendering_order { get; set; }
        // TODO: In future, handle re-compacting of rendering order values
        //       after an exciting history of adding, removing, and re-ordering the rendering order...

        public SortedList<int, object[]> content_regions;
        // Key => Value is of the form RenderingOrderKeyValue => [IGridIterable, pos_x, pos_y]

        public MapCompositedLayer(int ww, int hh)
        {
            width  = ww;
            height = hh;
            next_rendering_order = 1;
            content_regions = new SortedList<int, object[]>();
        } // MapCompositedLayer()


        // TODO: 
        // Refactor to include the notion of a "currently viewed area"
        // (there can be more than one, need to keep track of them to call "refresh" methods).
        // 
        // The code for contents_at_XY() becomes a simple lookup of the DenseGridLayer which is that area,
        // and the code to create/refresh one is taken from the current contents_at_XY() 
        // and certain parts of the loop in Form1 OnPaint().


        public void AddContentRegion(IGridIterable new_region, int pos_x, int pos_y)
        {
            // Note that duplicates (multiple adds of same IGridIterable) are valid,
            // as well as multiple content regions with the same origin and/or dimensions.
            // (The layers above this may be concerned with reducing such overlaps, but we do not care at this level.)
            // 
            if ((pos_x < 0) || (pos_x >= width )) { throw new ArgumentException("Invalid pos_x"); }
            if ((pos_y < 0) || (pos_y >= height)) { throw new ArgumentException("Invalid pos_y"); }

            int      key   = next_rendering_order++;
            object[] value = new object[] { new_region, pos_x, pos_y };
            content_regions.Add(key, value);
        } // AddContentRegion()

        public int contents_at_XY(int xx, int yy)
        {
            Rectangle rr = new Rectangle(xx, yy, 1, 1); // make into a member, to avoid lots of new() spamming ???
            int id = 0;
            foreach (KeyValuePair<int, object[]> kvp in content_regions)
            {
                // Using a SortedList<>, for iteration in ascending order by key.
                // The key is also the "rendering order", so that 
                // multiple overlapping content regions (which are allowed, and indeed ordinary)
                // result in the element with the highest rendering order over-writing
                // those from earlier content regions.
                // 
                // TODO: Something different will be desired for layers which support multiple objects per tile,
                //       presumably adding elements into a list of objects present...
                // 
                int key = kvp.Key;
                IGridIterable grid = (IGridIterable) kvp.Value[0];
                int pos_x          = (int) kvp.Value[1];
                int pos_y          = (int) kvp.Value[2];

                Rectangle rect = new Rectangle(pos_x, pos_y, grid.width, grid.height);  // Perhaps also a member for same reasons ???
                if (rr.IntersectsWith(rect))
                {
                    int grid_rel_x = xx - pos_x;
                    int grid_rel_y = yy - pos_y;
                    int val = grid.contents_at_XY(grid_rel_x, grid_rel_y);
                    if (val != 0) { id = val; }
                }
            } // foreach

            // If we get here, then zero content regions overlapped,
            // so return the NULL object (or the default_terrain, if implemented)
            if (id != 0) { return id; }
            return 127;  // TODO: return "default object" for this map
        } // contents_at_XY()

        public int set_contents_at_XY(int xx, int yy, int new_contents)
        {
            // TODO: Needs to be implemented...
            // For now, we ignore this...
            return 0;
        } // set_contents_at_XY()

        public int min_x() { return 0; }
        public int min_y() { return 0; }

        public int center_x() { return width / 2; }
        public int center_y() { return height / 2; }

        public int max_x() { return Math.Max(0, width - 1); }
        public int max_y() { return Math.Max(0, height - 1); }

    } // class MapCompositedLayer

}
