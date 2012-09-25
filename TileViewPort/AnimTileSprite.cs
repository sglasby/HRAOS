using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using OpenTK.Graphics.OpenGL;

public class AnimTileSprite : ObjectRegistrar.IHaximaSerializeable, ITileSprite {

    public int    ID  { get; private set; }
    public string tag { get { return String.Format("{0}-{1}", ObjectRegistrar.Sprites.tag_prefix, ID); } }  // TODO: Probably want a different tag prefix...

    TileSheet _tile_sheet { get; set; }
    Image     _image      { get { return _tile_sheet.sheet; } }

    public int num_frames { get; private set; }
    StaticTileSprite[] frame_sequence { get; set; }

    //Rectangle _rect       { get; set; }
    //int       _texture    { get; set; }

    public TileSheet tile_sheet(int frame) { return _tile_sheet; }
    public Image     image     (int frame) { return _image;      }
    public Rectangle rect      (int frame) { int ff = frame % num_frames; return frame_sequence[ff].rect(ff);    }
    public int       texture   (int frame) { int ff = frame % num_frames; return frame_sequence[ff].texture(ff); }

    public AnimTileSprite(TileSheet tile_sheet, params int[] frame_indexes) {
        // This form of the constructor is more convenient to call when 
        // the frames are specified via a single index.
        // When specified by [x,y] within the TileSheet, the other form is needful.
        // 
        // TODO: Some means to specify an arg list of (tile_sheet, [x1,y1], [x2,y2], ...)
        //       If possible, it would be a nicety...
        if (tile_sheet == null) {
            throw new ArgumentException("Got null tile_sheet");
        }
        if (frame_indexes == null || frame_indexes.Length == 0) {
            throw new ArgumentException("Got null or empty frame_indexes array");
        }
        _tile_sheet    = tile_sheet;
        num_frames     = frame_indexes.Length;
        frame_sequence = new StaticTileSprite[num_frames];
        for (int ii = 0; ii < num_frames; ii++) {
            int this_tile_index = frame_indexes[ii];
            frame_sequence[ii]  = tile_sheet[this_tile_index];
        }
        this.ID = ObjectRegistrar.Sprites.register_obj_as(this, typeof(ITileSprite) );
    } // AnimTileSprite(sh,frame_indexes)

    public AnimTileSprite(TileSheet tile_sheet, params StaticTileSprite[] anim_frames) {
        // This form of the constructor is useful if it is desired to specify the frames 
        // by [x,y] on the TileSheet (or by [x,y,z] on a TileSheetStack).
        // When a single index will suffice, the other form is handier.
        if (tile_sheet == null) {
            throw new ArgumentException("Got null tile_sheet");
        }
        if (anim_frames == null || anim_frames.Length == 0) {
            throw new ArgumentException("Got null or empty anim_frames array");
        }
        _tile_sheet    = tile_sheet;
        frame_sequence = anim_frames;
        num_frames     = frame_sequence.Length;
        this.ID        = ObjectRegistrar.Sprites.register_obj_as(this, typeof(ITileSprite) );
    } // AnimTileSprite(sh,anim_frames)
    

//    public AnimTileSprite(TileSheet tile_sheet, int OpenGL_texture_id, int xx, int yy, int ww, int hh) :
//        this(tile_sheet, OpenGL_texture_id, new Rectangle(xx, yy, ww, hh)) {
//        // This overload has an empty method body
//    } // TileSprite(TileSheet,tex,x,y,w,h)

    public void GDI_Draw_Tile(Graphics gg, int xx, int yy, ImageAttributes attrib, int frame) {
        // Keeping this around, as it may prove convenient to be able 
        // to draw a tile onto a Control for certain UI purposes.

        // Draw the region this.rect of the image onto gg at xx,yy, with no scaling
        Rectangle rr = this.rect(frame);
        Rectangle destRect = new Rectangle(xx, yy, rr.Width, rr.Height);
        gg.DrawImage(_image,
                     new Rectangle(xx, yy, rr.Width, rr.Height),
                     rr.X, rr.Y, rr.Width, rr.Height,
                     GraphicsUnit.Pixel, attrib);
    } // GDI_Draw_Tile()

} // class

