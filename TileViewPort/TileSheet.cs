using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using OpenTK.Graphics.OpenGL;

// This class stores information about a "stack" of 
// one or more image files (sheets) with geometry information 
// defining a grid of rectangular regions (indexed 0 .. N-1 ).
// 
// It does NOT store any information about individual rectangle regions,
// as that job is handled by other classes, which implement ITileSprite.
// 
public class TileSheet : ObjectRegistrar.IHaximaSerializeable {
    public int    ID  { get; private set; }
    public string tag { get { return String.Format("{0}-{1}", ObjectRegistrar.Sprites.tag_prefix, ID); } }

    public  string[] file_names { get; private set; }
    public  Bitmap[] bitmaps    { get; private set; }

    public int num_image_sheets {
        get { return this.bitmaps.Length; }
    }
    public int num_tiles {
        get {
            // This is a one-based value, the number of tiles for array allocations
            return (width_tiles * height_tiles * depth_sheets);
        }
    }
    public int max_index {
        get {
            // This is a zero-based value, for array indexing and loops
            return this.num_tiles - 1;
        }
    }

    public int width_tiles  { get; set; }  // 1..some_max, width  in tiles of the sheet
    public int height_tiles { get; set; }  // 1..some_max, height in tiles of the sheet
    public int depth_sheets { get; set; }  // 1..some_max, number of sheets in the "stack"

    public int tile_wide_px { get; set; }  // 1..some_max (commonly 32), width  in pixels of 1 tile
    public int tile_high_px { get; set; }  // 1..some_max (commonly 32), height in pixels of 1 tile

    public int x_offset     { get; set; }  // 0..n (usually 0), num blank pixels on sheet left edge
    public int y_offset     { get; set; }  // 0..n (usually 0), num blank pixels on sheet top edge

    public int x_spacing    { get; set; }  // 0..n (usually 0), num blank pixels between columns
    public int y_spacing    { get; set; }  // 0..n (usually 0), num blank pixels between rows

    public Rectangle rect_for_tile(int ii) {
        if (ii < 0 || ii > this.max_index) {
            throw new ArgumentException("Tile index arg out of range");
        }
        int xx = GridUtility3D.XforIWH(ii, this.width_tiles, this.height_tiles);
        int yy = GridUtility3D.YforIWH(ii, this.width_tiles, this.height_tiles);
        return rect_for_tile(xx, yy);
    } // rect_for_tile(ii)

    public Rectangle rect_for_tile(int xx_column, int yy_row) {
        if (xx_column < 0 || xx_column >= this.width_tiles) {
            throw new ArgumentException("Tile X coordinate arg out of range");
        }
        if (yy_row < 0 || yy_row >= this.height_tiles) {
            throw new ArgumentException("Tile Y coordinate arg out of range");
        }
        int       rect_xx = x_offset + (xx_column * tile_wide_px) + (x_spacing * xx_column);
        int       rect_yy = y_offset + (yy_row    * tile_high_px) + (y_spacing * yy_row);
        Rectangle rect    = new Rectangle(rect_xx, rect_yy, tile_wide_px, tile_high_px);
        return rect;
    } // rect_for_tile(xx,yy)

    public Rectangle rect_for_tile(int xx_column, int yy_row, int zz_sheet) {
        // All sheets must have certain minimum dimensions, 
        // thus any on-grid (coordinates valid) tile rectangle which is requested
        // does not depend on which sheet.
        // (This overload is present for the sake of convenience.)
        if (zz_sheet < 0 || zz_sheet >= this.num_image_sheets) {
            // Harmless (because zz_sheet is not needful), but best to warn about a senseless request:
            throw new ArgumentException("Tile image sheet depth arg out of range");
        }
        return rect_for_tile(xx_column, yy_row);
    } // rect_for_tile(xx,yy,zz)

    public int min_image_file_width {
        // All image file(s) must have be at least this many pixels wide, 
        // to satisfy the dimensions indicated for this TileSheet:
        // If any/all image files have _greater_ width (unused space on the right), that is OK.
        get {
            return x_offset + (tile_wide_px * width_tiles) + (x_spacing * (width_tiles - 1));
        }
    }
    public int min_image_file_height {
        // All image file(s) must have be at least this many pixels tall, 
        // to satisfy the dimensions indicated for this TileSheet.
        // If any/all image files have _greater_ height (unused space on the bottom), that is OK.
        get{
            return y_offset + (tile_high_px * height_tiles) + (y_spacing * (height_tiles - 1));
        }
    }



    //////////////////////////////////////////////////////////////////////

    // Constructor methods:
    public TileSheet(int sheet_width_in_tiles, int sheet_height_in_tiles,
                         int tile_width_px_arg,    int tile_height_px_arg,
                         int x_offset_arg,         int y_offset_arg,
                         int x_spacing_arg,        int y_spacing_arg,
                         params string[] file_names_arg) {
        // Check the dimensional arguments for the sheet and the tiles:
        if (sheet_width_in_tiles  < 1) { throw new ArgumentException("Invalid sheet width in tiles" ); }
        if (sheet_height_in_tiles < 1) { throw new ArgumentException("Invalid sheet height in tiles"); }
        this.width_tiles  = sheet_width_in_tiles;
        this.height_tiles = sheet_height_in_tiles;

        if (tile_width_px_arg  < 1) { throw new ArgumentException("Invalid tile width in pixels"  ); }
        if (tile_height_px_arg < 1) { throw new ArgumentException("Invalid tile height in pixels" ); }
        this.tile_wide_px = tile_width_px_arg;
        this.tile_high_px = tile_height_px_arg;

        if (x_offset_arg < 0) { throw new ArgumentException("x_offset cannot be negative"); }
        if (y_offset_arg < 0) { throw new ArgumentException("y_offset cannot be negative"); }
        this.x_offset = x_offset_arg;
        this.y_offset = y_offset_arg;

        if (x_spacing_arg < 0) { throw new ArgumentException("x_spacing cannot be negative"); }
        if (y_spacing_arg < 0) { throw new ArgumentException("y_spacing cannot be negative"); }
        this.x_spacing = x_spacing_arg;
        this.y_spacing = y_spacing_arg;

        // Check that image file names params is sensible, allocate stuff for all N sheets:
        if (file_names_arg == null || file_names_arg.Length == 0) {
            throw new ArgumentException("Missing or zero-length file_names argument");
        }
        this.depth_sheets = file_names_arg.Length;
        this.file_names   = new string[depth_sheets];
        this.bitmaps       = new Bitmap[depth_sheets];

        // Check that all file(s) mentioned exist:
        for (int ii = 0; ii < depth_sheets; ii++) {
            string file_name = file_names_arg[ii];

            this.file_names[ii] = file_name;

            // I had rather a time wrestling with getting color-key type transparency to work with OpenGL.
            // http://stackoverflow.com/questions/12417946/sprite-texture-atlas-gdi-bitmap-maketransparent-for-color-key-with-opentk
            // http://www.opentk.com/node/3160#comment-13910
            // 
            // Note that the MSDN documentation states that MakeTransparent() result in Format32bppArgb, 
            // http://msdn.microsoft.com/en-us/library/8517ckds.aspx
            // however the result appears to be in Format32bppPArgb.  Not sure what is going on with that...
            // 
            // Also, I had an earlier problem with a PNG created from scratch in GIMP, which had an odd stride.
            // There may be additional fiddling about to make sure that ALL bitmap files will work smoothly...

            this.bitmaps[ii] = new Bitmap(file_names[ii]);
            this.bitmaps[ii].MakeTransparent(Color.Magenta);

            // Check image dimensions against args:
            if (bitmaps[ii].Width < min_image_file_width) {
                string ex_string = String.Format("TileSheet image '{0}' is size {1}x{2} pixels, args indicate minimum width of {3} pixels.",
                        file_names[ii], bitmaps[ii].Width, bitmaps[ii].Height, min_image_file_width);
                throw (new Exception(ex_string));
            }
            if (bitmaps[ii].Height < min_image_file_height) {
                string ex_string = String.Format("TileSheet image '{0}' is size {1}x{2} pixels, args indicate minimum height of {3} pixels.",
                        file_names[ii], bitmaps[ii].Width, bitmaps[ii].Height, min_image_file_height);
                throw (new Exception(ex_string));
            }
        } // for(ii)

        this.ID = 123456;  // TODO: change this later
        // this.ID = ObjectRegistrar.Sprites.register_obj_as(this, typeof(NEW_TileSheet));
        return;
    } // NEW_TileSheet(many_args)

    public TileSheet(int tiles_across, int tiles_high, params string[] file_names_arg) :
        this(tiles_across, tiles_high,
             32, 32,  // (32 x 32 pixels) is the most common tile size
             0,  0,   // zero X and Y offset  (no blank space on left/top of sheet)
             0,  0,   // zero X and Y spacing (no blank space between columns/rows)
             file_names_arg) {
        // This overload has an empty method body
    } // NEW_TileSheet()

} // class
