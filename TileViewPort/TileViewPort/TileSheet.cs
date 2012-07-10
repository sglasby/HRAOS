using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

// TODO:
// Several source files in the TileViewPort project define more than one (class, enum, interface) per file.
// The C# style guides suggest one such per file; perhaps desireable to split them up...
public class TileSheet
{
    // in Haxima/Nazghul, tile sheets had a tag.
    // TileSheet should likely inherit from HaxObj to get ID() and tag() methods, etc.
    // TODO: That will presumably occur once (Map API + OpenTK TileViewPort) are merged with (HRAOS)...

    public string fileName { get; set; }

    public int width      { get; set; }  // 1..some_max, width  in tiles of the sheet
    public int height     { get; set; }  // 1..some_max, height in tiles of the sheet

    public int tileWidth  { get; set; }  // 1..some_max (commonly 32), width  in pixels of 1 tile
    public int tileHeight { get; set; }  // 1..some_max (commonly 32), height in pixels of 1 tile

    public int x_offset   { get; set; }  // 0..n (usually 0), num blank pixels on sheet left edge
    public int y_offset   { get; set; }  // 0..n (usually 0), num blank pixels on sheet top edge

    public int x_spacing  { get; set; }  // 0..n (usually 0), num blank pixels between columns
    public int y_spacing  { get; set; }  // 0..n (usually 0), num blank pixels between rows

    public Image sheet { get; private set; }
    TileSprite[] tile_rects;

    public TileSheet(string file_name_arg,
                     int sheet_width_arg, int sheet_height_arg,
                     int tile_width_arg,  int tile_height_arg,
                     int x_offset_arg,    int y_offset_arg,
                     int x_spacing_arg,   int y_spacing_arg)
    {
        // check file existence, readable
        if (!File.Exists(file_name_arg))
        {
            string    ex_string = String.Format("File '{0}' does not exist.", file_name_arg);
            Exception ex        = new Exception(ex_string);
            throw (ex);
        }
        fileName = file_name_arg;

        // Check sheet and tile dimension args:
        if (tile_width_arg   < 1) { throw (new Exception("Invalid tile width")  ); }
        if (tile_height_arg  < 1) { throw (new Exception("Invalid tile height") ); }
        if (sheet_width_arg  < 1) { throw (new Exception("Invalid sheet width") ); }
        if (sheet_height_arg < 1) { throw (new Exception("Invalid sheet height")); }

        tileWidth  = tile_width_arg;
        tileHeight = tile_height_arg;
        width      = sheet_width_arg;
        height     = sheet_height_arg;

        x_offset  = x_offset_arg;
        y_offset  = y_offset_arg;
        x_spacing = x_spacing_arg;
        y_spacing = y_spacing_arg;

        sheet = Image.FromFile(fileName);

        // Check image dimensions against args:
        int calc_min_ww = x_offset + (tileWidth  * width)  + (x_spacing * (width  - 1));  // Unused tiles/blank space on right  is OK
        int calc_min_hh = y_offset + (tileHeight * height) + (y_spacing * (height - 1));  // Unused tiles/blank space on bottom is OK
        if (sheet.Width < calc_min_ww)
        {
            string ex_string = String.Format("TileSheet image '{0}' is size {1}x{2} pixels, args specify minimum width of {3} pixels.",
                    fileName, sheet.Width, sheet.Height, calc_min_ww);
            Exception ex = new Exception(ex_string);
            throw (ex);
        }
        if (sheet.Height < calc_min_hh)
        {
            string ex_string = String.Format("TileSheet image '{0}' is size {1}x{2} pixels, args specify minimum height of {3} pixels.",
                    fileName, sheet.Width, sheet.Height, calc_min_hh);
            Exception ex = new Exception(ex_string);
            throw (ex);
        }

        int num_rects = 1 + maxIndex();
        tile_rects = new TileSprite[num_rects];
        for (int ii = 0; ii < num_rects; ii++)
        {
            int tiles_xx = GridUtility.XforIW(ii, width);
            int tiles_yy = GridUtility.YforIW(ii, width);

            int xx = x_offset + (GridUtility.XforIW(ii, width) * tileWidth)  + (x_spacing * GridUtility.XforIW(ii, tileWidth));
            int yy = y_offset + (GridUtility.YforIW(ii, width) * tileHeight) + (y_spacing * GridUtility.YforIW(ii, tileHeight));


            tile_rects[ii] = new TileSprite(sheet, xx, yy, tileWidth, tileHeight);
        }

        return;
    } // tileSheet()

    public TileSheet(string fileName, int tiles_across, int tiles_high) :
        this(fileName,
              tiles_across, tiles_high,
              32, 32,  // 32x32 is the most common tile size
              0, 0,    // zero X and Y offset (blank space on left/top of sheet)
              0, 0     // zero X and Y spacing (between columns/rows)
              )
    { /* empty method body for this overload */ }

    public TileSprite this[int tile_index]
    {
        get
        {
            if (tile_index < 0) { return null; }
            if (tile_index > maxIndex()) { return null; }
            return tile_rects[tile_index];
        }
    }

    public TileSprite this[int column, int row]
    {
        get
        {
            int tile_index = GridUtility.indexForXYW(column, row, width);
            if (tile_index < 0) { return null; }
            if (tile_index > maxIndex()) { return null; }
            return tile_rects[tile_index];
        }
    }

    public int maxIndex()
    {
        return (width * height) - 1;
    }

} // class TileSheet



// TODO:
// Some of the contents of TileSprite are specific to the needs of GDI+ drawing code, such as the original Draw() method.
// The OpenGL texture loading code needs to exist in the TileSprite constructor
//     (It is possible that noticeable efficiency would come from 
//      loading a tile sheet worth of textures in the TileSheet constructor.)
// TileSprite would then need to have a property for the OpenGL texture ID (integer), 
// which is needed by the tile blitting code 
//     (currently blit_square_tile() in TileViewPortControl.cs or Subject.cs, 
//      one of these will go away...)
public class TileSprite : ObjectRegistrar.IHaximaSerializeable
{
    public Image     image { get; set; }
    public Rectangle rect  { get; set; }

    public int ID { get; private set; }
    public string tag
    {
        get
        {
            return String.Format("{0}-{1}", ObjectRegistrar.Sprites.tag_prefix, ID);
        }
    } // tag()

    public TileSprite(Image img, int xx, int yy, int ww, int hh)
    {
        image = img;
        rect  = new Rectangle(xx, yy, ww, hh);
        ID    = ObjectRegistrar.Sprites.register_obj(this);
    } // TileSprite()

    // TODO: This drawing method uses a GDI+ method.
    // If the TileSprite class keeps such a drawing method, it should draw via OpenTK...
    // May want distinct methods to blit (square, hex_NS, hex_WE) tiles...
    public void Draw(Graphics gg, int xx, int yy, ImageAttributes attrib)
    {
        // Draw the region this.rect of the image onto gg at xx,yy, with no scaling
        Rectangle destRect = new Rectangle(xx, yy, rect.Width, rect.Height);
        //gg.DrawImage(image, destRect, rect, pixels);
        gg.DrawImage(image,
                     new Rectangle(xx, yy, rect.Width, rect.Height),
                     rect.X, rect.Y, rect.Width, rect.Height,
                     GraphicsUnit.Pixel, attrib);

    } // Draw()

} // class TileSprite



public static class GridUtility
{
    // Some useful constants:
    public const int max_width  = 256;
    public const int max_height = 256;

    // Functions useful for converting (X,Y,width) into (index), and vice-versa:
    public static int indexForXYW(int xx, int yy, int ww) { return (ww * yy) + xx; }
    public static int XforIW(int ii, int ww) { return ii % ww; }
    public static int YforIW(int ii, int ww) { return ii / ww; }

    public static int Clamp(int value, int min, int max)
    {
        if (value <= min) { return min; }
        if (value >= max) { return max; }
        return value;
    } // Clamp()

} // class GridUtility
