using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Text;

namespace TileSheetStack
{
    public class TileSheetStack : IGrid3D
    {
        #region region_implementing_IGrid3D

        public int width  { get; set; }
        public int height { get; set; }
        public int depth  { get; set; }

        public int min_x { get { return 0; } }
        public int min_y { get { return 0; } }
        public int min_z { get { return 0; } }

        public int center_x { get { return (min_x + max_x) / 2; } }
        public int center_y { get { return (min_y + max_y) / 2; } }

        public int max_x { get { return width  - 1; } }
        public int max_y { get { return height - 1; } }
        public int max_z { get { return depth  - 1; } }

        public int index(int xx, int yy, int zz)
        {
            return (width * height * zz) + (width * yy) + (xx);
        } //

        public int index(int xx, int yy)
        {
            int zz = 0;
            return (width * height * zz) + (width * yy) + (xx);
        } // 

        public int max_index { get { return (width * height * depth) - 1; } }

        public int x_for_index(int ii) { return (ii % (width * height)) % height; }
        public int y_for_index(int ii) { return (ii % (width * height)) / width;  }
        public int z_for_index(int ii) { return (ii % (width * height));          }

        #endregion

        string[]                image_filenames;
        System.Drawing.Bitmap[] bitmaps;
        // ...and an array of some kind of OpenGL texture type...

        // Hmmm...are these redundant, since we have height, width, depth from IGrid3D ?
        //public int width_tiles  { get; private set; }
        //public int height_tiles { get; private set; }
        //public int depth_tiles  { get; private set; }

        public TileSize tilesize      { get; set; }
        public Point    sheet_offset  { get; set; }
        public Point    sheet_spacing { get; set; }

        public TileSheetStack(int arg_ts_ww, int arg_ts_hh, int arg_ts_dd, TileSize arg_ts,
                              Point arg_sheet_offset, Point arg_sheet_spacing,
                              params string[] arg_image_filenames)
        {
            // TODO: arg checking.  Or perhaps that belongs in the .set methods?
            width  = arg_ts_ww;
            height = arg_ts_hh;
            depth  = arg_ts_dd;

            tilesize      = arg_ts;
            sheet_offset  = arg_sheet_offset;
            sheet_spacing = arg_sheet_spacing;

            if (arg_image_filenames == null ||
                arg_image_filenames.Length == 0)
            {
                throw new ArgumentException("Got null or zero-length filename list");
            }

            int LL = arg_image_filenames.Length;
            image_filenames = new string[LL];
            bitmaps         = new Bitmap[LL];
            for (int ii = 0; ii < LL; ii++)
            {
                string fn = arg_image_filenames[ii];
                if (!System.IO.File.Exists(fn) )
                {
                    throw new ArgumentException(String.Format("Filename argument {0} does not exist", fn));
                }
                image_filenames[ii] = fn;
                bitmaps[ii]         = new Bitmap(fn);

                // ...call method to load texture array for all of the textures on all of the bitmaps...
            }

        } // 

        public TileSheetStack(int ww, int hh, int dd, TileSize ts,
                              params string[] arg_image_filenames)
            : this(ww, hh, dd, ts, new Point(0, 0), new Point(0, 0), arg_image_filenames) { }

        // Definitions for stuff like the below might belong 
        // in the immutable script preamble, 
        // rather than as C# static members...
        //public static TileSize tile_32x32 = new TileSize(32, 32);
        //public static TileSize tile_16x16 = new TileSize(16, 16);
        //public static TileSize tile_8x8   = new TileSize( 8,  8);

    } // class

} // namespace
