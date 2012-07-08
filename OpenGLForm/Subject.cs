using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using System.Drawing.Imaging;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace OpenGLForm
{
    public enum TilingModes
    {
        None = 0,
        Square,
        Hex_NS,
        Hex_WE
    }

    public class Subject
    {
        const int XX_POS_MAX = 24; // 25 == VIEW_WW / TILE_WW  so xx_pos can be 0..24
        const int YY_POS_MAX = 17; // 18 == VIEW_HH / TILE_HH  so yy_pos can be 0..17
        const int TILE_WW = 32;
        const int TILE_HH = 32;
        const int SHEET_TILES_WW = 16;
        const int SHEET_TILES_HH = 16;
        const int NUM_TILES = SHEET_TILES_WW * SHEET_TILES_HH;
        const int SHEET_WW_PX = SHEET_TILES_WW * TILE_WW;
        const int SHEET_HH_PX = SHEET_TILES_HH * TILE_HH;

        Bitmap bitmap;
        int texture;
        int[] tiles;

        TilingModes tiling_mode = TilingModes.Hex_NS;
        int padding = 1;
        public double angle = 0.0;

        public Subject()
        {
            bitmap = new Bitmap("U4.B_enhanced-32x32.png");
            tiles = new int[NUM_TILES];
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

        public void Render()
        {
            switch (tiling_mode)
            {
                case TilingModes.Square:
                    blit_square_grid(0, XX_POS_MAX, 0, YY_POS_MAX, padding);
                    break;

                case TilingModes.Hex_NS:
                    blit_hex_NS_grid(0, XX_POS_MAX, 0, YY_POS_MAX, padding);
                    break;

                case TilingModes.Hex_WE:
                    blit_hex_WE_grid(0, XX_POS_MAX, 0, YY_POS_MAX, padding);
                    break;

                case TilingModes.None:
                default:
                    break;
            } // switch()
        }


        void blit_square_grid(int min_x, int max_x, int min_y, int max_y, int padding)
        {
            int ii = 0;
            for (int yy = min_y; yy <= max_y; yy++)
            {
                for (int xx = min_x; xx <= max_x; xx++)
                {
                    ii++;
                    ii = ii % 16;

                    blit_square_tile(xx, yy, ii, padding);
                } // for(xx)
            } // for(yy)
        } // blit_square_grid()

        void blit_hex_NS_grid(int min_x, int max_x, int min_y, int max_y, int padding)
        {
            int ii = 0;
            for (int yy = min_y; yy <= max_y; yy++)
            {
                for (int xx = min_x; xx <= max_x; xx++)
                {
                    ii++;
                    ii = ii % 16;

                    blit_hex_NS_tile(xx, yy, ii, padding);
                } // for(xx)
            } // for(yy)
        } // blit_hex_NS_grid()

        void blit_hex_WE_grid(int min_x, int max_x, int min_y, int max_y, int padding)
        {
            int ii = 0;
            for (int yy = min_y; yy <= max_y; yy++)
            {
                for (int xx = min_x; xx <= max_x; xx++)
                {
                    ii++;
                    ii = ii % 16;

                    blit_hex_WE_tile(xx, yy, ii, padding);
                } // for(xx)
            } // for(yy)
        } // blit_hex_WE_grid()

        public void blit_square_tile(int xx_pos, int yy_pos, int tile_index, int padding)
        {
            xx_pos = clamp(0, XX_POS_MAX, xx_pos);
            yy_pos = clamp(0, YY_POS_MAX, yy_pos);
            padding = clamp(0, 8, padding);

            double xx = xx_pos * (TILE_WW + padding);
            double yy = yy_pos * (TILE_HH + padding);

            const double LL = -(TILE_WW / 2);
            const double RR = +(TILE_WW / 2);
            const double TT = +(TILE_HH / 2);
            const double BB = -(TILE_HH / 2);

            const double HALF_TILE_WW = TILE_WW / 2;
            const double HALF_TILE_HH = TILE_HH / 2;

            GL.PushMatrix();
            GL.Translate(HALF_TILE_WW + xx, (yy + HALF_TILE_HH), 0);
            GL.Rotate(angle, 0.0, 0.0, -1.0);

            GL.BindTexture(TextureTarget.Texture2D, tiles[tile_index]);
            GL.Begin(BeginMode.Quads);

            GL.TexCoord2(0.0f, 1.0f); GL.Vertex2(LL, BB);
            GL.TexCoord2(1.0f, 1.0f); GL.Vertex2(RR, BB);
            GL.TexCoord2(1.0f, 0.0f); GL.Vertex2(RR, TT);
            GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(LL, TT);

            GL.End();

            GL.PopMatrix();

        } // blit_square_tile()

        void blit_hex_NS_tile(int xx_pos, int yy_pos, int tile_index, int padding)
        {
            xx_pos = clamp(0, XX_POS_MAX, xx_pos);
            yy_pos = clamp(0, YY_POS_MAX, yy_pos);
            padding = clamp(0, 8, padding);

            // Consider a right triangle with angles (90, 60, 30) and sides (1, Sqrt(3), hypotenuse 2)
            // An equilateral hexagon is made of six equilateral triangles,
            // each composed of two such right triangles.
            // 
            // A hexagon with North-South grain will thus have 
            //   width  (West-East, point-to-point distance) WW = (2*HH)/sqrt(3)
            //   height (North-South, edge-to-edge distance) HH = (WW/2)*sqrt(3)
            // so all needful figures can be determined given either WW or HH to start with.
            // (A hexagon with East-West grain is the same, but with width and height exchanged.)

            double ROOT_3 = Math.Sqrt(3.0);
            // TODO: hex-constructor methods which can be passed the desired WW or HH...
            //double WW = 32;
            //double HH = (WW / 2) * ROOT_3;  // 27.7128 pixels tall
            double HH = 32;
            double WW = (2.0 * HH) / ROOT_3;  // 36.9504 pixels wide
            double w = (WW / 4);
            double h = (WW / 4) * ROOT_3;
            //double side_length = (WW / 2);

            // Vertex coordinates along hex X and Y axis:
            double x1 = -2.0 * w;
            double x2 = -1.0 * w;
            double x3 = 0.0;
            double x4 = +1.0 * w;
            double x5 = +2.0 * w;  // WW

            double y1 = +h;
            double y2 = 0.0;
            double y3 = -h;

            // Texture coordinates along hex X and Y axis:
            // These values assume that the texture has the same pixel dimensions 
            //  as the vertex destination, meaning 
            //      WW = (2*32)/sqrt(3) for HH = 32 pixels
            //   or HH = (32/2)*sqrt(3) for WW = 32 pixels
            // 
            // There would be some distortion if these texture coordinates were applied to a texture with width == height.
            // (And my tile art thus far has that property...need to find or make some sample hex tiles...)
            double tx1 = 0.0;
            double tx2 = 1.0 / 4.0;
            double tx3 = 2.0 / 4.0;
            double tx4 = 3.0 / 4.0;
            double tx5 = 1.0;

            double ty1 = 0.0;
            double ty2 = 0.5;
            double ty3 = 1.0;

            double xx = xx_pos * (3.0 * w + padding);
            double yy = yy_pos * (HH + padding);

            if (xx_pos % 2 == 0)
            {
                // Even-numbered rows are offset vertically by half a hex
                yy += h;
            }

            GL.PushMatrix();
            GL.Translate(xx + (2.0 * w), (yy + h), 0);
            GL.Rotate(angle, 0.0, 0.0, -1.0);

            GL.BindTexture(TextureTarget.Texture2D, tiles[tile_index]);
            GL.Begin(BeginMode.TriangleFan);

            //           ___    
            //          /   \   ty3
            //         /     \ 
            //        <   X   > ty2
            //         \     / 
            //          \___/   ty1
            // 
            // tx1, tx2, tx3, tx4, tx5

            GL.TexCoord2(tx3, ty2); GL.Vertex2(x3, y2);  // Center of Hex
            GL.TexCoord2(tx2, ty3); GL.Vertex2(x2, y3);  // Top Left
            GL.TexCoord2(tx4, ty3); GL.Vertex2(x4, y3);  // Top Right
            GL.TexCoord2(tx5, ty2); GL.Vertex2(x5, y2);  // Center Right
            GL.TexCoord2(tx4, ty1); GL.Vertex2(x4, y1);  // Bottom Right
            GL.TexCoord2(tx2, ty1); GL.Vertex2(x2, y1);  // Bottom Left
            GL.TexCoord2(tx1, ty2); GL.Vertex2(x1, y2);  // Center Left
            GL.TexCoord2(tx2, ty3); GL.Vertex2(x2, y3);  // Top Left


            GL.End();

            GL.PopMatrix();

        } // blit_hex_NS_tile()

        void blit_hex_WE_tile(int xx_pos, int yy_pos, int tile_index, int padding)
        {
            xx_pos = clamp(0, XX_POS_MAX, xx_pos);
            yy_pos = clamp(0, YY_POS_MAX, yy_pos);
            padding = clamp(0, 8, padding);

            // Consider a right triangle with angles (90, 60, 30) and sides (1, Sqrt(3), hypotenuse 2)
            // An equilateral hexagon is made of six equilateral triangles,
            // each composed of two such right triangles.
            // 
            // A hexagon with North-South grain will thus have 
            //   width  (West-East, point-to-point distance) WW = (2*HH)/sqrt(3)
            //   height (North-South, edge-to-edge distance) HH = (WW/2)*sqrt(3)
            // so all needful figures can be determined given either WW or HH to start with.
            // (A hexagon with East-West grain is the same, but with width and height exchanged.)

            double ROOT_3 = Math.Sqrt(3.0);
            //double HH = 32;
            //double WW = (HH / 2.0) * ROOT_3;  // 27.7128 pixels wide
            double WW = 32;
            double HH = WW / (ROOT_3) * 2.0;    // 36.9504 pixels tall
            double w = WW / 2.0;
            double h = HH / 4.0;
            //double side_length = h / ROOT_3;

            // Vertex coordinates along hex X and Y axis:
            double x1 = -w;
            double x2 = 0;
            double x3 = +w;

            double y1 = -2.0 * h;
            double y2 = -1.0 * h;
            double y3 = 0.0;
            double y4 = +1.0 * h;
            double y5 = +2.0 * h;

            // Texture coordinates along hex X and Y axis:
            // These values assume that the texture has the same pixel dimensions 
            //  as the vertex destination, meaning 
            //      WW = (2*32)/sqrt(3) for HH = 32 pixels
            //   or HH = (32/2)*sqrt(3) for WW = 32 pixels
            // 
            // There would be some distortion if these texture coordinates were applied to a texture with width == height.
            // (And my tile art thus far has that property...need to find or make some sample hex tiles...)
            double tx1 = 0.0;
            double tx2 = 0.5;
            double tx3 = 1.0;

            double ty1 = 1.0;
            double ty2 = 3.0 / 4.0;
            double ty3 = 2.0 / 4.0;
            double ty4 = 1.0 / 4.0;
            double ty5 = 0.0;

            // These values work for square tiles, need some modification for a NS-grain hex grid...
            double xx = xx_pos * (TILE_WW + padding);
            double yy = yy_pos * (3.0 * h + padding);

            if (yy_pos % 2 == 0)
            {
                // Even-numbered rows are offset vertically by half a hex
                xx += w;
            }

            GL.PushMatrix();
            GL.Translate(xx + w, (yy + 2.0 * h), 0);
            GL.Rotate(angle, 0.0, 0.0, -1.0);

            GL.BindTexture(TextureTarget.Texture2D, tiles[tile_index]);
            GL.Begin(BeginMode.TriangleFan);

            //       /\     y5
            //      /  \    y4
            //     |    |   
            //     |    |   y3
            //     |    |   
            //      \  /    y2
            //       \/     y1
            // tX1, tX2, tX3

            GL.TexCoord2(tx2, ty3); GL.Vertex2(x2, y3);  // Center of Hex
            GL.TexCoord2(tx1, ty2); GL.Vertex2(x3, y2);  // Top Left
            GL.TexCoord2(tx1, ty4); GL.Vertex2(x3, y4);  // Top Right
            GL.TexCoord2(tx2, ty5); GL.Vertex2(x2, y5);  // Center Right
            GL.TexCoord2(tx3, ty4); GL.Vertex2(x1, y4);  // Bottom Right
            GL.TexCoord2(tx3, ty2); GL.Vertex2(x1, y2);  // Bottom Left
            GL.TexCoord2(tx2, ty1); GL.Vertex2(x2, y1);  // Center Left
            GL.TexCoord2(tx1, ty2); GL.Vertex2(x3, y2);  // Top Left

            GL.End();

            GL.PopMatrix();

        } // blit_hex_WE_tile()

        int clamp(int min, int max, int value)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
