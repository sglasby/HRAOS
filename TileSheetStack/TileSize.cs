using System;

namespace TileSheetStack
{
    public class TileSize
    {
        public int width_pixels  { get; private set; }
        public int height_pixels { get; private set; }

        public int offset_x { get; private set; }
        public int offset_y { get; private set; }

        public bool is_oversize  { get; private set; }
        public bool is_undersize { get; private set; }
        public bool is_unit_tile { get { return !(is_oversize || is_undersize); } }

        public TileSize(int ww, int hh, int x_off, int y_off, int size)
        {
            width_pixels  = ww;
            height_pixels = hh;

            offset_x = x_off;
            offset_y = y_off;

            // Pass size +1 or -1 for an oversize / undersize tile:
            if (size > 0) { is_oversize  = true; }
            if (size < 0) { is_undersize = true; }
            // Pass size == 0 for a "normal" size tile
        }

        public TileSize(int ww, int hh) : this(ww, hh, 0, 0, 0) { }

    } // class

} // namespace
