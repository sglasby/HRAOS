using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WinForms_display_bitmap
{
    public class TileViewPortControl : Control
    {
        // A TileViewPort "has a" TileViewPortControl,
        // and draws within that control, being constrained 
        // by the Width and Height of same.

        // Inheriting from Control provides members/properties for:
        // X, Y, Width, Height
        // and various others

        public TileViewPort owner;
        public int left_pad;  // Extra pixels on the left
        public int top_pad;   // Extra pixels on the top

        public TileViewPortControl()
        {
            // The order of construction is:
            // 1) Designer creates TileViewPortControl
            // 2) program creates TileViewPort
            // 3) TileViewPort constructor sets this.owner
            // The TileViewPort and TileViewPortControl are now ready for use

            // Commentout out this SetStyle() call shows how long it takes to render the viewport 
            // (you can see it draw, without double-buffering hiding the drawing process)
            //
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer, true);
            
            //this.UpdateStyles();  // supposedly can help?  no difference seen
            // this.DoubleBuffered = true;  // no gain

            // Experimenting...no improvement...
            //this.SetStyle(
            //    ControlStyles.UserPaint |
            //    ControlStyles.Opaque |
            //    ControlStyles.AllPaintingInWmPaint |
            //    ControlStyles.OptimizedDoubleBuffer,
            //    true
            //    );

            // This variant was supposed to be helpful in speed, per URL
            // http://www.dotnet247.com/247reference/msgs/35/178364.aspx
            // 
            //this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
            //    ControlStyles.UserPaint | 
            //    ControlStyles.Opaque | 
            //    ControlStyles.DoubleBuffer, 
            //    true);

        } // TileViewPortControl()

        protected override void OnPaint(PaintEventArgs e)
        {
            if (owner == null)
            {
                return;
                //throw new Exception("TileViewPortControl OnPaint() without owner");
            }
            //base.OnPaint(e);  // Control.OnPaint() supposedly does nothing anyways?

            Graphics surface = e.Graphics;
            int tileWidth    = owner.map.sheet.tileWidth;
            int tileHeight   = owner.map.sheet.tileHeight;

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
                    int pixel_yy = top_pad  + (view_yy * tileHeight);

                    if (on_map)
                    {
                        foreach (int LL in MapLayers.MapRenderingOrder)
                        {
                            TileSprite sp = (TileSprite) owner.map.contents_at_LXY(LL, map_xx, map_yy);
                            if (sp != null)
                            {
                                sp.Draw(surface, pixel_xx, pixel_yy, null);
                            }
                        } // foreach(LL)
                    }
                    else
                    {
                        // Put down a background color on non-map areas of the TileViewPort:
                        surface.FillRectangle(Brushes.SlateGray, pixel_xx, pixel_yy, tileWidth - 1, tileHeight - 1);  // With dividing lines
                        //surface.FillRectangle(Brushes.SlateGray, pixel_xx, pixel_yy, tileWidth, tileHeight);  // Solid color
                    }

                    foreach (int LL in ViewPortLayers.ViewPortRenderingOrder)
                    {
                        // TODO: Is allocating this repeatedly a cause of slowness?
                        Color   transparent_color = Color.FromArgb(0x00, 0xFF, 0x00, 0xFF);
                        ImageAttributes imageAttr = new ImageAttributes();
                        imageAttr.SetColorKey(transparent_color, transparent_color, ColorAdjustType.Default);

                        TileSprite sp = (TileSprite) owner.contents_at_LXY(LL, view_xx, view_yy);
                        if (sp != null)
                        {
                            sp.Draw(surface, pixel_xx, pixel_yy, imageAttr);
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

    } // class TileViewPortControl

} // namespace 
