using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using OpenTK.Graphics.OpenGL;

public class TileSheet {
    // in Haxima/Nazghul, tile sheets had a tag.
    // TileSheet should likely inherit from HaxObj to get ID() and tag() methods, etc.
    // TODO: That will presumably occur once (Map API + OpenTK TileViewPort) are merged with (HRAOS)...
    // TODO: It may make sense for TileSheet to implement IGridIterable, once this is merged with HRAOS...

    public string[] file_names  { get; set; }

    public int width_tiles  { get; set; }  // 1..some_max, width  in tiles of the sheet
    public int height_tiles { get; set; }  // 1..some_max, height in tiles of the sheet
    public int depth_sheets { get; set; }  // 1..some_max, number of sheets in the "stack"

    public int tile_wide_px { get; set; }  // 1..some_max (commonly 32), width  in pixels of 1 tile
    public int tile_high_px { get; set; }  // 1..some_max (commonly 32), height in pixels of 1 tile

    public int x_offset     { get; set; }  // 0..n (usually 0), num blank pixels on sheet left edge
    public int y_offset     { get; set; }  // 0..n (usually 0), num blank pixels on sheet top edge

    public int x_spacing    { get; set; }  // 0..n (usually 0), num blank pixels between columns
    public int y_spacing    { get; set; }  // 0..n (usually 0), num blank pixels between rows

    //public  Bitmap             sheet  { get; private set; }  // TODO: Various cleanup from the original "single sheet" TileSheet, now that the refactor to "stack of sheets" seems to be working...
    public  Bitmap[]           sheets { get; private set; }
    private StaticTileSprite[] tile_rects;

    public TileSheet(// string file_name_arg,
                     int sheet_width_in_tiles, int sheet_height_in_tiles,
                     int tile_width_px_arg,    int tile_height_px_arg,
                     int x_offset_arg,         int y_offset_arg,
                     int x_spacing_arg,        int y_spacing_arg,
                     params string[] file_names_arg) {
        // Note: 
        // The TileSheet constructor makes OpenGL calls to load texture data,
        // and as such, needs to be called after the OpenGL system is initialized,
        // in the sense of an OpenTK.Graphics.GraphicsContext being created.
        // 
        // This happens at 'Load' event time (just before the Form is displayed), 
        // which is currently handled within Form1.glControl1_Load(),
        // after the GLControl constructor is called in the Form1 constructor.

        // Check the dimenaional arguments for the sheet and the tiles:
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
        this.sheets       = new Bitmap[depth_sheets];
        this.tile_rects   = new StaticTileSprite[num_tiles];  // Allocates for all sheets: width * height * depth

        // Check that all file(s) mentioned exist:
        for (int ii = 0; ii < depth_sheets; ii++) {
            string file_name = file_names_arg[ii];
            if (!File.Exists(file_name)) {
                // Note: 
                // The idiomatic practice in the C# world seems to be to call 
                // a file-using method, and handle an exception if the file is not found, 
                // lacks read permissions, etc.  This seems odd to me, coming from the Unix/C/Perl world.
                // TODO: 
                // Think about how (script parsing + resource loading) will 
                // (check for / handle) errors such as this, and figure out what strategy is most suitable...
                throw new Exception(String.Format("File '{0}' does not exist.", file_name));
            }
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

            //this.sheet = new Bitmap(file_names[0]);
            //this.sheet.MakeTransparent(Color.Magenta);
            this.sheets[ii] = new Bitmap(file_names[ii]);
            this.sheets[ii].MakeTransparent(Color.Magenta);

            // Check image dimensions against args:
            if (sheets[ii].Width < min_image_file_width) {
                string ex_string = String.Format("TileSheet image '{0}' is size {1}x{2} pixels, args specify minimum width of {3} pixels.",
                        file_names[ii], sheets[ii].Width, sheets[ii].Height, min_image_file_width);
                throw (new Exception(ex_string));
            }
            if (sheets[ii].Height < min_image_file_height) {
                string ex_string = String.Format("TileSheet image '{0}' is size {1}x{2} pixels, args specify minimum height of {3} pixels.",
                        file_names[ii], sheets[ii].Width, sheets[ii].Height, min_image_file_height);
                throw (new Exception(ex_string));
            }
            Load_GL_Textures_for_TileSheet(ii);
        } // for(ii)

        return;
    } // tileSheet()

    public TileSheet(int tiles_across, int tiles_high, params string[] file_names_arg) :
        this(tiles_across, tiles_high,
             32, 32,  // 32x32 is the most common tile size
             0, 0,    // zero X and Y offset (blank space on left/top of sheet)
             0, 0,     // zero X and Y spacing (between columns/rows)
             file_names_arg) {
        // This overload has an empty method body
    } // TileSheet()

    public void Load_GL_Textures_for_TileSheet(int which_sheet) {
        int[] GL_textures = new int[num_tiles];
        GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
        GL.GenTextures(num_tiles, GL_textures);
        Check_for_GL_error("After calling GL.GenTextures()");

        int xx, yy, ii;
        for (yy = 0; yy < this.height_tiles; yy++) {
            for (xx = 0; xx < this.width_tiles; xx++) {
                ii = GridUtility3D.indexForXYZWH(xx, yy, which_sheet, this.width_tiles, this.height_tiles);
                //ii = GridUtility2D.indexForXYW(xx, yy, this.width_tiles);

                GL.Enable(EnableCap.Texture2D);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

                GL.BindTexture(TextureTarget.Texture2D, GL_textures[ii]);
                Check_for_GL_error("After calling GL.BindTexture()");

                Rectangle  tile_rect = rect_for_tile(xx, yy);
                BitmapData tile_data = sheets[which_sheet].LockBits(tile_rect,
                                                                    ImageLockMode.ReadOnly,
                                                                    System.Drawing.Imaging.PixelFormat.Format32bppPArgb);  // This _works_
                // PixelFormat values for the last arg seem to produce OS-dependent results???
                // I observe: 
                // Format32bppPArgb --> ___ for Windows 7,          transparent for WinXP, transparent for Unbuntu Linux
                // Format32bppRgb   --> transparency for Windows 7, black for WinXP,       magenta for Ubuntu Linux

                GL.TexImage2D(TextureTarget.Texture2D,
                              0,
                              PixelInternalFormat.Rgba,
                              tile_data.Width,
                              tile_data.Height,
                              0,
                              OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                              PixelType.UnsignedByte,
                              tile_data.Scan0);
                Check_for_GL_error("After calling GL.TexImage2D()");

                sheets[which_sheet].UnlockBits(tile_data);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                tile_rects[ii] = new StaticTileSprite(this, which_sheet, GL_textures[ii], tile_rect);
            } // for(xx)
        } // for(yy)
    } // Load_GL_Textures_for_TileSheet()

    public StaticTileSprite this[int tile_index] {
        get {
            if (tile_index < 0)         { return null; }
            if (tile_index > max_index) { return null; }
            return tile_rects[tile_index];
        }
    } // indexer[ii]

    public StaticTileSprite this[int xx_column, int yy_row] {
        get {
            int tile_index = GridUtility2D.indexForXYW(xx_column, yy_row, this.width_tiles);
            return this[tile_index];

            //if (tile_index < 0)         { return null; }
            //if (tile_index > max_index) { return null; }
            //return tile_rects[tile_index];
        }
    } // indexer[column,row]

    public Rectangle rect_for_tile(int ii) {
        int xx = GridUtility3D.XforIWH(ii, this.width_tiles, this.height_tiles);
        int yy = GridUtility3D.YforIWH(ii, this.width_tiles, this.height_tiles);
        //int zz = GridUtility3D.ZforIWH(ii, this.width_tiles, this.height_tiles);  // Not needed at this point...

        //int xx = GridUtility2D.XforIW(ii, this.width_tiles);
        //int yy = GridUtility2D.YforIW(ii, this.width_tiles);
        return rect_for_tile(xx, yy);
    } // rect_for_tile(ii)

    public Rectangle rect_for_tile(int xx_column, int yy_row) {
        // xx_column and yy_row are 0-based
        int       rect_xx = x_offset + (xx_column * tile_wide_px) + (x_spacing * xx_column);
        int       rect_yy = y_offset + (yy_row    * tile_high_px) + (y_spacing * yy_row);
        Rectangle rect    = new Rectangle(rect_xx, rect_yy, tile_wide_px, tile_high_px);
        return rect;
    } // rect_for_tile(xx,yy)
    
    public int max_index {
        get {
            return (width_tiles * height_tiles * depth_sheets) - 1;  // Zero-based, for array indexing and loops
        }
    }

    public int num_tiles {
        get {
            return (width_tiles * height_tiles * depth_sheets);  // One-based, number of tiles for array allocations
        }
    }

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

    public static void Check_for_GL_error(string msg) {
        ErrorCode err = GL.GetError();
        if (err != ErrorCode.NoError) {
            string str = String.Format("{0} GL.GetError() returns: {1}", msg, err);
            throw new Exception(str);
        }
    } // Check_for_GL_error()

} // class TileSheet







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
