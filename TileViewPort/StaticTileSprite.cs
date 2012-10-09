using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using OpenTK.Graphics.OpenGL;

public class StaticTileSprite : ObjectRegistrar.IHaximaSerializeable, ITileSprite {
    public int    ID  { get; private set; }
    public string tag { get { return String.Format("{0}-{1}", ObjectRegistrar.Sprites.tag_prefix, ID); } }

    TileSheet _tile_sheet  { get; set; }
    int       _which_sheet { get; set; }
    Image     _image       { get { return _tile_sheet.sheets[_which_sheet]; } }
    Rectangle _rect        { get; set; }
    int       _texture     { get; set; }

    public int num_frames { get { return 1; } }

    public TileSheet tile_sheet(int frame) { return _tile_sheet; }
    public Image     image     (int frame) { return _image;      }
    public Rectangle rect      (int frame) { return _rect;       }
    public int       texture   (int frame) { return _texture;    }

    public StaticTileSprite(TileSheet tile_sheet, int which_sheet, int OpenGL_texture_id, Rectangle rect) {
        _tile_sheet  = tile_sheet;
        _which_sheet = which_sheet;
        _texture     = OpenGL_texture_id;
        _rect        = rect;
        this.ID      = ObjectRegistrar.Sprites.register_obj_as(this, typeof(ITileSprite) );
    } // TileSprite(sh,tex,Rectangle)

    public StaticTileSprite(TileSheet tile_sheet, int which_sheet, int OpenGL_texture_id, int xx, int yy, int ww, int hh) :
        this(tile_sheet, which_sheet, OpenGL_texture_id, new Rectangle(xx, yy, ww, hh)) {
        // This overload has an empty method body
    } // TileSprite(TileSheet,tex,x,y,w,h)

    // TODO: 
    // Perhaps move square/hex tile drawing methods from TileViewPortControl into TileSprite ...
    // Such methods would want an argument for the TVP (or other GLControl?) to draw them upon ...
    // For now, will make TileViewPortControl.Render() get the texture_id via (sprite_obj).texture ...

    public void GDI_Draw_Tile(Graphics gg, int xx, int yy, ImageAttributes attrib) {
        // Keeping this around, as it may prove convenient to be able 
        // to draw a tile onto a Control for certain UI purposes.

        // Draw the region this.rect of the image onto gg at xx,yy, with no scaling
        Rectangle destRect = new Rectangle(xx, yy, _rect.Width, _rect.Height);
        gg.DrawImage(_image,
                     new Rectangle(xx, yy, _rect.Width, _rect.Height),
                     _rect.X, _rect.Y, _rect.Width, _rect.Height,
                     GraphicsUnit.Pixel, attrib);
    } // GDI_Draw_Tile()

    public void GDI_Draw_Tile(Graphics gg, int xx, int yy, ImageAttributes attrib, int frame) {
        // The frame argument is not needed, for a StaticTileSprite
        GDI_Draw_Tile(gg, xx, yy, attrib);
    }

} // class TileSprite
