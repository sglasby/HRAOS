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

    public string fileName { get; set; }

    public int width      { get; set; }  // 1..some_max, width  in tiles of the sheet
    public int height     { get; set; }  // 1..some_max, height in tiles of the sheet

    public int tileWidth  { get; set; }  // 1..some_max (commonly 32), width  in pixels of 1 tile
    public int tileHeight { get; set; }  // 1..some_max (commonly 32), height in pixels of 1 tile

    public int x_offset   { get; set; }  // 0..n (usually 0), num blank pixels on sheet left edge
    public int y_offset   { get; set; }  // 0..n (usually 0), num blank pixels on sheet top edge

    public int x_spacing  { get; set; }  // 0..n (usually 0), num blank pixels between columns
    public int y_spacing  { get; set; }  // 0..n (usually 0), num blank pixels between rows

    public Bitmap sheet { get; private set; }
    TileSprite[]  tile_rects;
    int[]         GL_textures;

    public TileSheet(string file_name_arg,
                     int sheet_width_arg, int sheet_height_arg,
                     int tile_width_arg,  int tile_height_arg,
                     int x_offset_arg,    int y_offset_arg,
                     int x_spacing_arg,   int y_spacing_arg) {
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
        if (tile_width_arg   < 1) { throw (new Exception("Invalid tile width")); }
        if (tile_height_arg  < 1) { throw (new Exception("Invalid tile height")); }
        if (sheet_width_arg  < 1) { throw (new Exception("Invalid sheet width")); }
        if (sheet_height_arg < 1) { throw (new Exception("Invalid sheet height")); }

        this.tileWidth  = tile_width_arg;
        this.tileHeight = tile_height_arg;
        this.width      = sheet_width_arg;
        this.height     = sheet_height_arg;

        this.x_offset  = x_offset_arg;
        this.y_offset  = y_offset_arg;
        this.x_spacing = x_spacing_arg;
        this.y_spacing = y_spacing_arg;

        this.sheet = new Bitmap(fileName);

        // Check image dimensions against args:
        int calc_min_ww = x_offset + (tileWidth  * width)  + (x_spacing * (width  - 1));  // Unused tiles/blank space on right  is OK
        int calc_min_hh = y_offset + (tileHeight * height) + (y_spacing * (height - 1));  // Unused tiles/blank space on bottom is OK
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

        this.tile_rects  = new TileSprite[num_tiles];
        this.GL_textures = new int[num_tiles];

        Load_GL_Textures_for_TileSheet();

        for (int ii = 0; ii < num_tiles; ii++) {
            int tiles_xx = GridUtility.XforIW(ii, width);
            int tiles_yy = GridUtility.YforIW(ii, width);

            int xx = x_offset + (GridUtility.XforIW(ii, width) * tileWidth) + (x_spacing * GridUtility.XforIW(ii, tileWidth));
            int yy = y_offset + (GridUtility.YforIW(ii, width) * tileHeight) + (y_spacing * GridUtility.YforIW(ii, tileHeight));


            tile_rects[ii] = new TileSprite(this, GL_textures[ii], xx, yy, tileWidth, tileHeight);
        }

        return;
    } // tileSheet()

    public TileSheet(string fileName, int tiles_across, int tiles_high) :
        this(fileName,
              tiles_across, tiles_high,
              32, 32,  // 32x32 is the most common tile size
              0, 0,    // zero X and Y offset (blank space on left/top of sheet)
              0, 0     // zero X and Y spacing (between columns/rows)
              ) {
        /* empty method body for this overload */
    } // TileSheet()

    public void Load_GL_Textures_for_TileSheet() {
        GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

        // ??? Is the block below needful?  It creates a texture out of the entire sheet...
//        BitmapData data = sheet.LockBits(new System.Drawing.Rectangle(0, 0, sheet.Width, sheet.Height),
//                              ImageLockMode.ReadOnly,
//                              System.Drawing.Imaging.PixelFormat.Format32bppArgb);
//        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
//                      data.Width, data.Height, 0,
//                      OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
//        sheet.UnlockBits(data);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);


        GL.GenTextures(num_tiles, GL_textures);
        int xx, yy, ii;
        for (yy = 0; yy < this.height; yy++) {
            for (xx = 0; xx < this.width; xx++) {
                ii = (yy * this.width) + xx;  // Use a GridUtility method call for this?
                GL.BindTexture(TextureTarget.Texture2D, GL_textures[ii]);

                // FIXME: the Rectangle below assumes that (x_offset, y_offset, x_spacing, y_spacing) are all zero ...
                Rectangle  this_tile = new Rectangle(xx * tileWidth, yy * tileHeight, tileWidth, tileHeight);
                BitmapData tile_data = sheet.LockBits(this_tile,
                                                      ImageLockMode.ReadOnly,
                                                      System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2D,
                              0,
                              PixelInternalFormat.Rgba,
                              tile_data.Width,
                              tile_data.Height,
                              0,
                              OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                              PixelType.UnsignedByte,
                              tile_data.Scan0);

                sheet.UnlockBits(tile_data);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);

            } // for(xx)
        } // for(yy)
    } // Load_GL_Textures_for_TileSheet()


    public TileSprite this[int tile_index] {
        get {
            if (tile_index < 0)        { return null; }
            if (tile_index > maxIndex) { return null; }
            return tile_rects[tile_index];
        }
    }

    public TileSprite this[int column, int row] {
        get {
            int tile_index = GridUtility.indexForXYW(column, row, width);
            if (tile_index < 0)        { return null; }
            if (tile_index > maxIndex) { return null; }
            return tile_rects[tile_index];
        }
    }

    public int maxIndex {
        get {
            return (width * height) - 1;  // Zero-based, for array indexing and loops
        }
    }
    public int num_tiles {
        get {
            return (width * height);  // One-based, number of tiles for array allocations
        }
    }

} // class TileSheet



public class TileSprite : ObjectRegistrar.IHaximaSerializeable {
    public TileSheet tile_sheet { get; private set; }
    public Image     image      { get { return tile_sheet.sheet; } }
    public Rectangle rect       { get; private set; }

    public int    ID  { get; private set; }
    public string tag { get { return String.Format("{0}-{1}", ObjectRegistrar.Sprites.tag_prefix, ID); } }

    public int texture { get; private set; }

    public TileSprite(TileSheet tile_sheet, int OpenGL_texture_id, int xx, int yy, int ww, int hh) {
        this.tile_sheet = tile_sheet;
        this.texture    = OpenGL_texture_id;
        this.rect       = new Rectangle(xx, yy, ww, hh);
        this.ID         = ObjectRegistrar.Sprites.register_obj(this);
    } // TileSprite()

    // TODO: 
    // Perhaps move square/hex tile drawing methods from TileViewPortControl into TileSprite ...
    // Such methods would want an argument for the TVP (or other GLControl?) to draw them upon...
    // For now, will make TileViewPortControl.Render() get the texture_id via (sprite_obj).texture ...

    public void GDI_Draw_Tile(Graphics gg, int xx, int yy, ImageAttributes attrib) {
        // Keeping this around, as it may prove convenient to be able 
        // to draw a tile onto a Control for certain UI purposes.

        // Draw the region this.rect of the image onto gg at xx,yy, with no scaling
        Rectangle destRect = new Rectangle(xx, yy, rect.Width, rect.Height);
        gg.DrawImage(image,
                     new Rectangle(xx, yy, rect.Width, rect.Height),
                     rect.X, rect.Y, rect.Width, rect.Height,
                     GraphicsUnit.Pixel, attrib);
    } // GDI_Draw_Tile()

} // class TileSprite



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
