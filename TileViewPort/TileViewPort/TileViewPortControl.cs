using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

public enum TilingModes
{
    None = 0,
    Square,
    Hex_NS,
    Hex_WE
}

public class TileViewPortControl
{
    // A TileViewPort "has a" TileViewPortControl,
    // and draws within that control, being constrained 
    // by the Width and Height of same.

    // Inheriting from Control provides members/properties for:
    // X, Y, Width, Height
    // and various others

    const int XX_POS_MAX = 24; // 25 == VIEW_WW / TILE_WW  so xx_pos can be 0..24
    const int YY_POS_MAX = 17; // 18 == VIEW_HH / TILE_HH  so yy_pos can be 0..17
    const int TILE_WW = 32;
    const int TILE_HH = 32;
    const int SHEET_TILES_WW = 16;
    const int SHEET_TILES_HH = 16;
    const int NUM_TILES = SHEET_TILES_WW * SHEET_TILES_HH;
    const int SHEET_WW_PX = SHEET_TILES_WW * TILE_WW;
    const int SHEET_HH_PX = SHEET_TILES_HH * TILE_HH;

    public TileViewPort owner;
    public int left_pad;  // Extra pixels on the left
    public int top_pad;   // Extra pixels on the top

    Bitmap bitmap;
    int texture;
    int[] tiles;
    OpenTK.GLControl gl_control;
    public TilingModes tiling_mode = TilingModes.Square;

    public TileViewPortControl(OpenTK.GLControl gl_control)
    {
        this.gl_control = gl_control;
        bitmap = new Bitmap("U4.B_enhanced-32x32.png");
        tiles = new int[NUM_TILES];
    } // TileViewPortControl()


    public int Width
    {
        get { return gl_control.Width; }
    }

    public int Height
    {
        get { return gl_control.Height; }
    }

    public void Invalidate()
    {
        gl_control.Invalidate();
    }
    public void Render()
    {
        if (owner == null)
        {
            return;
            //throw new Exception("TileViewPortControl OnPaint() without owner");
        }

        int tileWidth = owner.map.sheet.tileWidth;
        int tileHeight = owner.map.sheet.tileHeight;

        GL.ClearColor(Color.Green); // Yay! .NET Colors can be used directly!


        for (int view_yy = 0; view_yy < owner.height_tiles; view_yy++)
        {

            for (int view_xx = 0; view_xx < owner.width_tiles; view_xx++)
            {
                int map_xx = (owner.x_origin + view_xx);
                int map_yy = (owner.y_origin + view_yy);

                bool on_map = (map_xx >= 0 &&
                               map_xx < owner.map.width &&
                               map_yy >= 0 &&
                               map_yy < owner.map.height);

                int pixel_xx = left_pad + (view_xx * tileWidth);
                int pixel_yy = top_pad + (view_yy * tileHeight);

                if (on_map)
                {
                    foreach (int LL in MapLayers.MapRenderingOrder)
                    {
                        TileSprite sp = (TileSprite)owner.map.contents_at_LXY(LL, map_xx, map_yy);
                        if (sp != null)
                        {
                            // FIXME: do the GL drawing here!!! sp.Draw(surface, pixel_xx, pixel_yy, null);
                        }
                    } // foreach(LL)
                }
                else
                {
                    // Put down a background color on non-map areas of the TileViewPort:
                    // GL.clear()
                }

                foreach (int LL in ViewPortLayers.ViewPortRenderingOrder)
                {
                    // TODO: Is allocating this repeatedly a cause of slowness?
                    Color transparent_color = Color.FromArgb(0x00, 0xFF, 0x00, 0xFF);
                    ImageAttributes imageAttr = new ImageAttributes();
                    imageAttr.SetColorKey(transparent_color, transparent_color, ColorAdjustType.Default);

                    TileSprite sp = (TileSprite)owner.contents_at_LXY(LL, view_xx, view_yy);
                    if (sp != null)
                    {
                        //sp.Draw(surface, pixel_xx, pixel_yy, imageAttr);
                        // FIXME: gl drawing code!
                    }
                } // foreach(LL)

            } // for(view_xx)
        } // for(view_yy)

    } // OnPaint()

    [BrowsableAttribute(false)]
    public int x_origin
    {
        get { return owner.x_origin; }
        set { owner.x_origin = value; }
    }

    [BrowsableAttribute(false)]
    public int y_origin
    {
        get { return owner.y_origin; }
        set { owner.y_origin = value; }
    }

    public void LoadTextures()
    {
        //GL.ClearColor(Color.CornflowerBlue);
        //GL.Enable(EnableCap.Texture2D);

        GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
        GL.GenTextures(1, out texture);
        GL.BindTexture(TextureTarget.Texture2D, texture);

        BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, SHEET_WW_PX, SHEET_HH_PX),
                              ImageLockMode.ReadOnly,
                              System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                      data.Width, data.Height, 0,
                      OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
        bitmap.UnlockBits(data);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);


        GL.GenTextures(NUM_TILES, tiles);
        int xx, yy, ii;
        for (yy = 0; yy < SHEET_TILES_HH; yy++)
        {
            for (xx = 0; xx < SHEET_TILES_WW; xx++)
            {
                ii = (yy * SHEET_TILES_WW) + xx;
                //tiles[ii] = 1000 + ii;
                GL.BindTexture(TextureTarget.Texture2D, tiles[ii]);

                BitmapData bb = bitmap.LockBits(new System.Drawing.Rectangle(xx * TILE_WW, yy * TILE_HH, TILE_WW, TILE_HH),
                                                  ImageLockMode.ReadOnly,
                                                  System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                              bb.Width, bb.Height, 0,
                              OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bb.Scan0);

                bitmap.UnlockBits(bb);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            } // for(xx)
        } // for(yy)
    } // Load()


} // class TileViewPortControl
