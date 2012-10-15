using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using OpenTK.Graphics.OpenGL;

public class TileSprite : ObjectRegistrar.IHaximaSerializeable, ITileSprite {

    public int    ID  { get; private set; }
    public string tag { get { return String.Format("{0}-{1}", ObjectRegistrar.Sprites.tag_prefix, ID); } }

    public TileSheet sheet      { get; private set; }
    public int           num_frames { get { return this.frame_indexes.Length; } }

    // TODO: 
    // Do something useful regarding a range check on these (or index % num_frames)
    // (May belong in some wrapper method, or the calling classes)
    public int[]       frame_indexes { get; private set; }
    public Bitmap[]    bitmap        { get; private set; }
    public Rectangle[] rect          { get; private set; }
    public int[]       texture       { get; private set; }



    //////////////////////////////////////////////////////////////////////

    // Constructor methods:
    public TileSprite(TileSheet tile_sheet, params int[] frame_index_args) {
        // This form of the constructor is more convenient to call when 
        // the frames are specified via a single index.
        // When specified by [x,y] within the TileSheet, the other form is needful.
        // 
        // TODO: Some means to specify an arg list of (tile_sheet, [x1,y1], [x2,y2], ...)
        //       If possible, it would be a nicety...
        if (tile_sheet == null) {
            throw new ArgumentException("Got null tile_sheet");
        }
        if (frame_index_args == null || frame_index_args.Length == 0) {
            throw new ArgumentException("Got null or empty frame_indexes array");
        }
        this.sheet         = tile_sheet;
        this.frame_indexes = new int[frame_index_args.Length];  // Causes this.num_frames to be initialized
        this.bitmap        = new Bitmap[this.num_frames];
        this.rect          = new Rectangle[this.num_frames];
        this.texture       = new int[this.num_frames];

        for (int ii = 0; ii < this.num_frames; ii++) {
            int index_on_sheet     = frame_index_args[ii];
            int which_sheet        = GridUtility3D.ZforIWH(index_on_sheet, tile_sheet.width_tiles, tile_sheet.height_tiles);

            this.frame_indexes[ii] = index_on_sheet;
            this.bitmap[ii]        = this.sheet.bitmaps[which_sheet];
            this.rect[ii]          = this.sheet.rect_for_tile(index_on_sheet);
            this.texture[ii]       = Load_OpenGL_texture_for_tile(this.bitmap[ii], this.rect[ii]);
        }
        this.ID = ObjectRegistrar.Sprites.register_obj_as(this, typeof(ITileSprite) );
    } // NEW_TileSprite(sh,frame_indexes)


    private int Load_OpenGL_texture_for_tile(Bitmap bitmap, Rectangle tile_rect) {
        GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
        int texture_ID = GL.GenTexture();
        Check_for_GL_error("After calling GL.GenTexture()");

        GL.Enable(EnableCap.Texture2D);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

        GL.BindTexture(TextureTarget.Texture2D, texture_ID);
        Check_for_GL_error("After calling GL.BindTexture()");

        BitmapData tile_data = bitmap.LockBits(tile_rect,
                                               ImageLockMode.ReadOnly,
                                               System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

        GL.TexImage2D(TextureTarget.Texture2D,          // target
              0,                                        // level
              PixelInternalFormat.Rgba,                 // internal_format
              tile_data.Width,                          // width
              tile_data.Height,                         // height
              0,                                        // border
              OpenTK.Graphics.OpenGL.PixelFormat.Bgra,  // format
              PixelType.UnsignedByte,                   // pixel_type
              tile_data.Scan0                           // pixel_data
              );
        Check_for_GL_error("After calling GL.TexImage2D()");

        bitmap.UnlockBits(tile_data);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);

        return texture_ID;
    } // Load_OpenGL_texture_for_tile()

    public static void Check_for_GL_error(string msg) {
        ErrorCode err = GL.GetError();
        if (err != ErrorCode.NoError) {
            string str = String.Format("{0} GL.GetError() returns: {1}", msg, err);
            throw new Exception(str);
        }
    } // Check_for_GL_error()


    //////////////////////////////////////////////////////////////////////

    // Drawing methods and the like:

    //public void blit_square_tile() {
    //} // blit_square_tile()

    public void blit_square_tile(int pixel_xx, int pixel_yy, int frame) {
        int ff = frame % this.num_frames;
        double HALF_TILE_WW = this.rect[ff].Width  / 2;
        double HALF_TILE_HH = this.rect[ff].Height / 2;

        // Define OpenGL vertex coordinates for a square centered on the origin (0.0, 0.0)
        double LL = -HALF_TILE_WW;
        double RR = +HALF_TILE_WW;
        double TT = -HALF_TILE_HH;  // OpenGL origin coordinate (0,0) at bottom left, we want top left
        double BB = +HALF_TILE_HH;  // OpenGL origin coordinate (0,0) at bottom left, we want top left

        //double LL = -16;
        //double RR = +16;
        //double TT = -16;
        //double BB = +16;

        const double angle = 0.0;

        GL.PushMatrix();
        GL.Translate((pixel_xx + HALF_TILE_WW), (pixel_yy + HALF_TILE_HH), 0);
        GL.Rotate(angle, 0.0, 0.0, -1.0);

        GL.BindTexture(TextureTarget.Texture2D, this.texture[ff]);
        TileSprite.Check_for_GL_error("In blit_square_tile() after calling GL.BindTexture()");

        GL.Begin(BeginMode.Quads);
        {
            GL.TexCoord2(0.0f, 1.0f);  GL.Vertex2(LL, BB);  // Texture, Vertex coordinates for Bottom Left
            GL.TexCoord2(1.0f, 1.0f);  GL.Vertex2(RR, BB);  // Texture, Vertex coordinates for Bottom Right
            GL.TexCoord2(1.0f, 0.0f);  GL.Vertex2(RR, TT);  // Texture, Vertex coordinates for Top Right
            GL.TexCoord2(0.0f, 0.0f);  GL.Vertex2(LL, TT);  // Texture, Vertex coordinates for Top Left
        }
        GL.End();

        GL.PopMatrix();
    } // blit_square_tile()

    //public void GDI_Draw_Tile(Graphics gg, int xx, int yy, ImageAttributes attrib, int frame) {
    //} // GDI_Draw_Tile()

    public void GDI_Draw_Tile(Graphics gg, int xx, int yy, ImageAttributes attrib, int frame) {
        int ff = frame % this.num_frames;
        // Keeping this around, as it may prove convenient to be able 
        // to draw a tile onto a Control for certain UI purposes.

        // Draw the region this.rect of the image onto gg at xx,yy, with no scaling
        Rectangle rr = this.rect[ff];
        Rectangle destRect = new Rectangle(xx, yy, rr.Width, rr.Height);
        gg.DrawImage(this.bitmap[ff],
                     new Rectangle(xx, yy, rr.Width, rr.Height),
                     rr.X, rr.Y, rr.Width, rr.Height,
                     GraphicsUnit.Pixel, attrib);
    } // GDI_Draw_Tile()

} // class