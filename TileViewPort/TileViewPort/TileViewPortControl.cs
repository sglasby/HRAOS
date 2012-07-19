using System.ComponentModel;  // for [BrowseableAttribute] (may be able to remove after refactoring...)
using System.Drawing;

using OpenTK.Graphics.OpenGL;

public enum TilingModes {
    None = 0,
    Square,
    Hex_NS,
    Hex_WE
}

public class TileViewPortControl {
    // A TileViewPort "has a" TileViewPortControl,
    // and draws within that control, being constrained 
    // by the Width and Height of same.

    // Inheriting from Control provides members/properties for:
    // X, Y, Width, Height
    // and various others

    const int XX_POS_MAX     = 24; // 25 == VIEW_WW / TILE_WW  so xx_pos can be 0..24
    const int YY_POS_MAX     = 17; // 18 == VIEW_HH / TILE_HH  so yy_pos can be 0..17
    const int TILE_WW        = 32;
    const int TILE_HH        = 32;

    const int SHEET_TILES_WW = 16;
    const int SHEET_TILES_HH = 16;
    const int NUM_TILES      = SHEET_TILES_WW * SHEET_TILES_HH;
    const int SHEET_WW_PX    = SHEET_TILES_WW * TILE_WW;
    const int SHEET_HH_PX    = SHEET_TILES_HH * TILE_HH;

    public TileViewPort owner;
    OpenTK.GLControl    gl_control;  // TODO: TileViewPortControl needs to be refactored so that it _inherits_ from GLControl, rather than "hasa"
    public TilingModes  tiling_mode = TilingModes.Square;
    public int left_pad;  // Extra pixels on the left
    public int top_pad;   // Extra pixels on the top

    Bitmap bitmap;
    int[] tiles;

    public TileViewPortControl(OpenTK.GLControl gl_control) {
        this.gl_control = gl_control;
        bitmap = new Bitmap("U4.B_enhanced-32x32.png");
        tiles  = new int[NUM_TILES];
    } // TileViewPortControl()


    public int Width {
        get { return gl_control.Width; }
    }

    public int Height {
        get { return gl_control.Height; }
    }

    public void Invalidate() {
        gl_control.Invalidate();
    }

    public void Render(int frame) {
        if (owner == null) {
            return;
            //throw new Exception("TileViewPortControl OnPaint() without owner");
        }

        int tileWidth  = owner.map.sheet.tile_wide_px;
        int tileHeight = owner.map.sheet.tile_high_px;

        GL.ClearColor(Color.Green);  // Coordinates which are "off map" will remain this color

        // TODO: 
        // Iterating through an IGridIterable (TileViewPort, Map*Layer, etc) is common enough, 
        // perhaps a suitable closure could be defined, to avoid buggy duplication of similar code blocks?
        for (int view_yy = 0; view_yy < owner.height_tiles; view_yy++) {
            for (int view_xx = 0; view_xx < owner.width_tiles; view_xx++) {
                int map_xx = (owner.x_origin + view_xx);
                int map_yy = (owner.y_origin + view_yy);

                bool on_map = (map_xx >= 0 &&
                               map_xx < owner.map.width &&
                               map_yy >= 0 &&
                               map_yy < owner.map.height);

                // Aha!  Reason why "extra_width" etc is not working...pixel_xx and pixel_yy are unused!
                // (Thats what you get, merging two similar demo sources together)
                // blit_square_tile() currently takes TILE x,y position, but should rather take pixel_xx, pixel_yy
                // so that this sort of pixel positioning can be decreed...
                int pixel_xx = left_pad + (view_xx * tileWidth);
                int pixel_yy = top_pad  + (view_yy * tileHeight);  

                if (on_map) {
                    foreach (int LL in MapLayers.MapRenderingOrder) {
                        ITileSprite sp = (ITileSprite) owner.map.contents_at_LXY(LL, map_xx, map_yy);
                        if (sp != null) {
                            this.blit_square_tile(view_xx, view_yy, sp.texture(frame), 0);
                        }
                    } // foreach(LL)
                }

                // TODO: 
                // With the (GDI+) drawing code commented out inside this loop,
                // currently the ViewportLayers are not rendered...
                // Probably need to fix TileSprite class first, as the sprite is from a different sheet than the terrain sprites are...
                foreach (int LL in ViewPortLayers.ViewPortRenderingOrder) {
                    // TODO: Is allocating these repeatedly a cause of slowness?  
                    // ...imageAttr could be defined outside of the loop...
                    // ...Assuming that this is still relevant with OpenGL rendering code...
                    //Color transparent_color = Color.FromArgb(0x00, 0xFF, 0x00, 0xFF);
                    //ImageAttributes imageAttr = new ImageAttributes();
                    //imageAttr.SetColorKey(transparent_color, transparent_color, ColorAdjustType.Default);

                    ITileSprite sp = (ITileSprite) owner.contents_at_LXY(LL, view_xx, view_yy);
                    if (sp != null) {
                            this.blit_square_tile(view_xx, view_yy, sp.texture(frame), 0);
                    }
                } // foreach(LL)

            } // for(view_xx)
        } // for(view_yy)

    } // was previously named TileViewPortControl.OnPaint(), and may become that again ...

    public void blit_square_tile(int xx_pos, int yy_pos, int texture_ID, int padding) {
        xx_pos  = clamp(0, XX_POS_MAX, xx_pos);
        yy_pos  = clamp(0, YY_POS_MAX, yy_pos);
        padding = clamp(0, 8, padding);

        double xx = xx_pos * (TILE_WW + padding);
        double yy = yy_pos * (TILE_HH + padding);

        const double LL = -(TILE_WW / 2);
        const double RR = +(TILE_WW / 2);
        const double TT = -(TILE_HH / 2);  // OpenGL origin coordinate (0,0) at bottom left, we want top left
        const double BB = +(TILE_HH / 2);  // OpenGL origin coordinate (0,0) at bottom left, we want top left

        const double HALF_TILE_WW = TILE_WW / 2;
        const double HALF_TILE_HH = TILE_HH / 2;
        const double angle = 0.0;

        GL.PushMatrix();
        GL.Translate(HALF_TILE_WW + xx, (yy + HALF_TILE_HH), 0);
        GL.Rotate(angle, 0.0, 0.0, -1.0);

        GL.BindTexture(TextureTarget.Texture2D, texture_ID);

        GL.Begin(BeginMode.Quads);

        GL.TexCoord2(0.0f, 1.0f);
        GL.Vertex2(LL, BB);
        GL.TexCoord2(1.0f, 1.0f);
        GL.Vertex2(RR, BB);
        GL.TexCoord2(1.0f, 0.0f);
        GL.Vertex2(RR, TT);
        GL.TexCoord2(0.0f, 0.0f);
        GL.Vertex2(LL, TT);

        GL.End();

        GL.PopMatrix();

    } // blit_square_tile()

    int clamp(int min, int max, int value) {
        // TODO: Various versions of this exist, such as GridUtility.Clamp(), should be unified...
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }


    [BrowsableAttribute(false)]
    public int x_origin {
        get { return owner.x_origin; }
        set { owner.x_origin = value; }
    }

    [BrowsableAttribute(false)]
    public int y_origin {
        get { return owner.y_origin; }
        set { owner.y_origin = value; }
    }


} // class TileViewPortControl
