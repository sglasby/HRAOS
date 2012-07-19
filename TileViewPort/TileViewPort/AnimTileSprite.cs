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

    
    public AnimTileSprite(TileSheet tile_sheet, params StaticTileSprite[] anim_frames) {
        if (anim_frames == null || anim_frames.Length == 0) {
            throw new ArgumentException("Got null or empty anim_frames array");
        }
        _tile_sheet    = tile_sheet;
        frame_sequence = anim_frames;
        num_frames     = frame_sequence.Length;
        this.ID        = ObjectRegistrar.Sprites.register_obj_as(this, typeof(ITileSprite) );
    } // TileSprite(sh,tex,Rectangle)
    

//    public AnimTileSprite(TileSheet tile_sheet, int OpenGL_texture_id, int xx, int yy, int ww, int hh) :
//        this(tile_sheet, OpenGL_texture_id, new Rectangle(xx, yy, ww, hh)) {
//        // This overload has an empty method body
//    } // TileSprite(TileSheet,tex,x,y,w,h)


} // class

