using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using OpenTK.Graphics.OpenGL;


// TODO:
// Several source files in the TileViewPort project define more than one (class, enum, interface) per file.
// The C# style guides suggest one such per file; perhaps desireable to split them up...
public class TileSheet {
    // in Haxima/Nazghul, tile sheets had a tag.
    // TileSheet should likely inherit from HaxObj to get ID() and tag() methods, etc.
    // TODO: That will presumably occur once (Map API + OpenTK TileViewPort) are merged with (HRAOS)...
    // TODO: It may make sense for TileSheet to implement IGridIterable, once this is merged with HRAOS...

    public string fileName  { get; set; }

    public int width_tiles  { get; set; }  // 1..some_max, width  in tiles of the sheet
    public int height_tiles { get; set; }  // 1..some_max, height in tiles of the sheet

    public int tile_wide_px { get; set; }  // 1..some_max (commonly 32), width  in pixels of 1 tile
    public int tile_high_px { get; set; }  // 1..some_max (commonly 32), height in pixels of 1 tile

    public int x_offset     { get; set; }  // 0..n (usually 0), num blank pixels on sheet left edge
    public int y_offset     { get; set; }  // 0..n (usually 0), num blank pixels on sheet top edge

    public int x_spacing    { get; set; }  // 0..n (usually 0), num blank pixels between columns
    public int y_spacing    { get; set; }  // 0..n (usually 0), num blank pixels between rows

    public Bitmap sheet { get; private set; }
    StaticTileSprite[]  tile_rects;

    public TileSheet(string file_name_arg,
                     int sheet_width_arg,   int sheet_height_arg,
                     int tile_width_px_arg, int tile_height_px_arg,
                     int x_offset_arg,      int y_offset_arg,
                     int x_spacing_arg,     int y_spacing_arg) {
        // Note: 
        // The TileSheet constructor makes OpenGL calls to load texture data,
        // and as such, needs to be called after the OpenGL system is initialized
        // in the sense of an OpenTK.Graphics.GraphicsContext being created.
        // 
        // This happens at 'Load' event time (just before the Form is displayed), 
        // which is currently handled within Form1.glControl1_Load(),
        // after the GLControl constructor is called in the Form1 constructor.

        // check file existence, readable
        if (!File.Exists(file_name_arg)) {
            // Note: 
            // The idiomatic practice in the C# world seems to be to call 
            // a file-using method, and handle an exception if the file is not found, 
            // lacks read permissions, etc.  May be desirable to change to conform with 
            // that convention, though it seems odd to me, coming from the Unix/C/Perl world...
            throw (new Exception(String.Format("File '{0}' does not exist.", file_name_arg)));
        }
        this.fileName = file_name_arg;

        // Check sheet and tile dimension args:
        if (tile_width_px_arg  < 1) { throw (new Exception("Invalid tile width"  )); }
        if (tile_height_px_arg < 1) { throw (new Exception("Invalid tile height" )); }
        if (sheet_width_arg    < 1) { throw (new Exception("Invalid sheet width" )); }
        if (sheet_height_arg   < 1) { throw (new Exception("Invalid sheet height")); }

        this.width_tiles  = sheet_width_arg;
        this.height_tiles = sheet_height_arg;

        this.tile_wide_px = tile_width_px_arg;
        this.tile_high_px = tile_height_px_arg;

        this.x_offset  = x_offset_arg;
        this.y_offset  = y_offset_arg;

        this.x_spacing = x_spacing_arg;
        this.y_spacing = y_spacing_arg;

        // Note:
        // I spent an hour or so scratching my head when my first attempt at "wave" sprite tilesheet
        // (of lava, 8 frames shifted down 4 pixels per frame) displayed very odd image data.
        // The problem turned out to be that the image was a PNG with 24-bit color depth.
        // 
        // This suggests that there is a bug, or some other unknown-to-me behavior, 
        // with the Bitmap constructor with regards to PNG with 24-bit color depth, 
        // or with the OpenGL texture setup I have when used with such an image.
        // So, beware such.
        // 
        // Hmmm...I wonder if such an issue is related to the strange results I see 
        // when I attempt sheet.MakeTransparent(Color.Magenta) to get transparency via color-key, 
        // as seen in _working_ OpenGL examples...
        this.sheet = new Bitmap(fileName);
        this.sheet.MakeTransparent(Color.Magenta);  // I am seeing (very) messed up colors, but the result _is_ transparent on Magenta...

        // Check image dimensions against args:
        // TODO: extract a method/property for calc_min_ww, calc_min_hh (rect_for_tile() is another caller)...
        int calc_min_ww = x_offset + (tile_wide_px  * width_tiles) + (x_spacing * (width_tiles  - 1));  // Unused tiles/blank space on right  is OK
        int calc_min_hh = y_offset + (tile_high_px * height_tiles) + (y_spacing * (height_tiles - 1));  // Unused tiles/blank space on bottom is OK
        if (sheet.Width < calc_min_ww) {
            string ex_string = String.Format("TileSheet image '{0}' is size {1}x{2} pixels, args specify minimum width of {3} pixels.",
                    fileName, sheet.Width, sheet.Height, calc_min_ww);
            throw (new Exception(ex_string));
        }
        if (sheet.Height < calc_min_hh) {
            string ex_string = String.Format("TileSheet image '{0}' is size {1}x{2} pixels, args specify minimum height of {3} pixels.",
                    fileName, sheet.Width, sheet.Height, calc_min_hh);
            throw (new Exception(ex_string));
        }

        this.tile_rects = new StaticTileSprite[num_tiles];
        Load_GL_Textures_for_TileSheet();

        return;
    } // tileSheet()

    public TileSheet(string fileName, int tiles_across, int tiles_high) :
        this(fileName, tiles_across, tiles_high,
             32, 32,  // 32x32 is the most common tile size
             0, 0,    // zero X and Y offset (blank space on left/top of sheet)
             0, 0     // zero X and Y spacing (between columns/rows)
             ) {
        // This overload has an empty method body
    } // TileSheet()

    public void Load_GL_Textures_for_TileSheet() {
        int[] GL_textures = new int[num_tiles];
        GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
        GL.GenTextures(num_tiles, GL_textures);
        Check_for_GL_error("After calling GL.GenTextures()");

        int xx, yy, ii;
        for (yy = 0; yy < this.height_tiles; yy++) {
            for (xx = 0; xx < this.width_tiles; xx++) {
                ii = GridUtility.indexForXYW(xx, yy, this.width_tiles);

                GL.Enable(EnableCap.Texture2D);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

                GL.BindTexture(TextureTarget.Texture2D, GL_textures[ii]);
                Check_for_GL_error("After calling GL.BindTexture()");

                Rectangle  tile_rect = rect_for_tile(xx, yy);
                BitmapData tile_data = sheet.LockBits(tile_rect,
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

                sheet.UnlockBits(tile_data);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                tile_rects[ii] = new StaticTileSprite(this, GL_textures[ii], tile_rect);
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
            int tile_index = GridUtility.indexForXYW(xx_column, yy_row, this.width_tiles);
            return this[tile_index];

            //if (tile_index < 0)         { return null; }
            //if (tile_index > max_index) { return null; }
            //return tile_rects[tile_index];
        }
    } // indexer[column,row]

    public Rectangle rect_for_tile(int ii) {
        int xx = GridUtility.XforIW(ii, this.width_tiles);
        int yy = GridUtility.YforIW(ii, this.width_tiles);
        return rect_for_tile(xx, yy);
    } // rect_for_tile(ii)

    public Rectangle rect_for_tile(int xx_column, int yy_row) {
        // xx_column and yy_row are 0-based
        int       rect_xx = x_offset + (xx_column * tile_wide_px)  + (x_spacing * xx_column);
        int       rect_yy = y_offset + (yy_row    * tile_high_px) + (y_spacing * yy_row);
        Rectangle rect    = new Rectangle(rect_xx, rect_yy, tile_wide_px, tile_high_px);
        return rect;
    } // rect_for_tile(xx,yy)
    
    public int max_index {
        get {
            return (width_tiles * height_tiles) - 1;  // Zero-based, for array indexing and loops
        }
    }

    public int num_tiles {
        get {
            return (width_tiles * height_tiles);  // One-based, number of tiles for array allocations
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







public static class GridUtility {
    // Some useful constants:
    public const int max_width  = 256;
    public const int max_height = 256;

    // Functions useful for converting (X,Y,width) into (index), and vice-versa:
    public static int indexForXYW(int xx, int yy, int ww) { return (ww * yy) + xx; }
    public static int XforIW(int ii, int ww) { return ii % ww; }
    public static int YforIW(int ii, int ww) { return ii / ww; }

    public static int Clamp(int value, int min, int max) {
        if (value <= min) { return min; }
        if (value >= max) { return max; }
        return value;
    } // Clamp()

} // class GridUtility
