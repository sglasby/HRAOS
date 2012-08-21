﻿using System;

using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;


class TVPC : OpenTK.GLControl {

    public TilingModes tiling_mode;

    public int tile_grid_offset_x_px;  // offset for smooth-scrolling of tile grid rendering
    public int tile_grid_offset_y_px;  // offset for smooth-scrolling of tile grid rendering
    // public bool render_only_integral_tiles { get; set; }  // _maybe_ add such a feature, after all else works smoothly...

    // public int this.Width;   // Width  in pixels, inherited from System.Windows.Forms.Control
    // public int this.Height;  // Height in pixels, inherited from System.Windows.Forms.Control

    public int tile_width_px;   // Width  to render a single tile, in pixels
    public int tile_height_px;  // Height to render a single tile, in pixels

    private int? Width_in_tiles;   // Cached calculation, change upon resize
    private int? Height_in_tiles;  //Cached calculation, change upon resize
    public int width_in_tiles {
        get {
            if (Width_in_tiles != null) { return (int) Width_in_tiles; }
            // If this.Width does not divide evenly, we add an extra tile (integer division rounds down)
            int non_integral_width_tile = (this.Width % this.tile_width_px != 0) ? 1 : 0;
            int ww = (this.Width / this.tile_width_px);
            // Depending on partial-tile-scroll position,
            // the TileViewPort may need to render (ww + non_integral_width_tile), 
            // plus an extra columns of tiles along the left and right:
            Width_in_tiles =  (ww + non_integral_width_tile + 2);
            return (int) Width_in_tiles;
        }
    }
    public int height_in_tiles {
        get {
            if (Height_in_tiles != null) { return (int) Height_in_tiles; }
            // If this.Height does not divide evenly, we add an extra tile (integer division rounds down)
            int non_integral_height_tile = (this.Height % this.tile_height_px != 0) ? 1 : 0;
            int hh = (this.Height / this.tile_height_px);
            // Depending on partial-tile-scroll position,
            // the TileViewPort may need to render (hh + non_integral_height_tile), 
            // plus an extra row of tiles along the top and bottom:
            Height_in_tiles = (hh + non_integral_height_tile + 2);
            return (int) Height_in_tiles;
        }
    }

    private void allocate_layers() {
        this.layers = new IGridIterable[ViewPortLayers.COUNT];
        this.layers[ViewPortLayers.UI_Elements] = new DenseGrid(width_in_tiles, height_in_tiles, 0);
        Width_in_tiles  = null;  // Force re-calculation upon next access
        Height_in_tiles = null;  // Force re-calculation upon next access
    }

    void OnResize(object sender, EventArgs ee) {
        allocate_layers();
    }

    // public int Width;   // in pixels, of the control, inherited from System.Windows.Forms.Control
    // public int Height;  // in pixels, of the control, inherited from System.Windows.Forms.Control

    public SimpleMapV1      map { get; private set; }
    public IGridIterable[]  layers = null;
    public ScrollConstraint scroll_constraint { get; set; }

    private int X_origin;
    private int Y_origin;
    public int x_origin {
                get { return X_origin; }
        private set { X_origin = GridUtility.Clamp(value, min_x_offset, max_x_offset); }
    }
    public int y_origin {
                get { return Y_origin; }
        private set { Y_origin = GridUtility.Clamp(value, min_y_offset, max_y_offset); }
    }

    public int center_x { get { return (width_in_tiles  / 2); } }
    public int center_y { get { return (height_in_tiles / 2); } }

    public int min_x_offset {
        get {
            switch (this.scroll_constraint) {
                case ScrollConstraint.EntireMap:
                    return 0;
                case ScrollConstraint.CenterTile:
                    return -(this.center_x);
                case ScrollConstraint.EdgeCorner:
                    return -(this.width_in_tiles - 1);
                default:
                    throw new Exception("Got impossible ScrollConstraint value");
            }
        }
    }
    public int min_y_offset {
        get {
            switch (this.scroll_constraint) {
                case ScrollConstraint.EntireMap:
                    return 0;
                case ScrollConstraint.CenterTile:
                    return -(this.center_y);
                case ScrollConstraint.EdgeCorner:
                    return -(this.height_in_tiles - 1);
                default:
                    throw new Exception("Got impossible ScrollConstraint value");
            }
        }
    }

    public int max_x_offset {
        get {
            switch (this.scroll_constraint) {
                case ScrollConstraint.EntireMap:
                    return (this.map.width - this.width_in_tiles);
                case ScrollConstraint.CenterTile:
                    return (this.map.width - (this.center_x + 1));
                case ScrollConstraint.EdgeCorner:
                    return (this.map.width - 1);
                default:
                    throw new Exception("Got impossible ScrollConstraint value");
            }
        }
    }
    public int max_y_offset {
        get {
            switch (this.scroll_constraint) {
                case ScrollConstraint.EntireMap:
                    return (this.map.height - this.height_in_tiles);
                case ScrollConstraint.CenterTile:
                    return (this.map.height - (this.center_y + 1));
                case ScrollConstraint.EdgeCorner:
                    return (this.map.height - 1);
                default:
                    throw new Exception("Got impossible ScrollConstraint value");
            }
        }
    }

    public TVPC(int control_width_px_arg, int control_height_px_arg,
                int tile_width_px_arg,    int tile_height_px_arg,
                ScrollConstraint constraint_arg,
                SimpleMapV1 map_arg, int map_xx, int map_yy) {
        // 
        if (control_width_px_arg  < 1) { throw new ArgumentException("Got zero width for TVP control");  }
        if (control_height_px_arg < 1) { throw new ArgumentException("Got zero height for TVP control"); }
        if (tile_width_px_arg  < 1)    { throw new ArgumentException("Got zero tile_width_px_arg");  }
        if (tile_height_px_arg < 1)    { throw new ArgumentException("Got zero tile_height_px_arg"); }
        if (map_arg == null)           { throw new ArgumentException("Got null map_arg"); }
        this.Width  = control_width_px_arg;
        this.Height = control_height_px_arg;
        allocate_layers();

        this.tile_width_px  = tile_width_px_arg;
        this.tile_height_px = tile_height_px_arg;

        set_origin(map_arg, map_xx, map_yy);
        this.scroll_constraint = constraint_arg;
        this.tiling_mode       = TilingModes.Square;
        this.tile_grid_offset_x_px = 0;
        this.tile_grid_offset_y_px = 0;

    } // TVPC()

    // possibly other constructors, specifying control.Width, control.Height, etc...

    //private void setControlEdgeCentering() { /*...*/ }  // Taking a different approach to this functionality...

    public object contents_at_LXY(int layer, int xx, int yy) {
        if (layer < MapLayers.MIN) { return null; }
        if (layer > MapLayers.MAX) { return null; }
        if (xx < 0)                { return null; }
        if (xx >= width_in_tiles)  { return null; }
        if (yy < 0)                { return null; }
        if (yy >= height_in_tiles) { return null; }

        if (this.layers[layer] == null) {
            throw new ArgumentException("Accessed a layer which is null");
        }
        int sprite_ID = this.layers[layer].contents_at_XY(xx, yy);
        return ObjectRegistrar.Sprites.obj_for_ID(sprite_ID);
    } // contents_at_LXY()

    public int ID_at_LXY(int layer, int xx, int yy) {
        if (layer < MapLayers.MIN) { return 0; }
        if (layer > MapLayers.MAX) { return 0; }
        if (xx < 0)                { return 0; }
        if (xx >= width_in_tiles)  { return 0; }
        if (yy < 0)                { return 0; }
        if (yy >= height_in_tiles) { return 0; }

        if (this.layers[layer] == null) {
            throw new ArgumentException("Accessed a layer which is null");
        }
        int ID = this.layers[layer].contents_at_XY(xx, yy);
        return ID;
    } // ID_at_LXY()

    public bool set_origin(SimpleMapV1 map_arg, int map_xx, int map_yy) {
        // Set the map and the origin coordinates of the viewport
        // such that the viewport origin (top-left tile, for square grid)
        // is the tile with the specified coordinates.
        if (map_arg == null) { throw new ArgumentException("Got null map_arg"); }

        int xx = Utility.clamp(0, map_arg.width,  map_xx);
        int yy = Utility.clamp(0, map_arg.height, map_yy);

        this.map = map_arg;
        this.x_origin = xx;
        this.y_origin = yy;

        if ((map_xx != xx) || (map_yy != yy)) { return false; }
        return true;  // The specified coordinates were set, without adjustment
    } // set_origin()

    public bool set_center(SimpleMapV1 map_arg, int map_xx, int map_yy) {
        // Set the map and origin such that the center tile 
        // of the viewport is the tile with the specified coordinates, 
        // or as close as can be managed, given scrolling constraints.
        if (map_arg == null) { throw new ArgumentException("Got null map_arg"); }

        int xx_c = map_arg.width  - this.center_x;
        int yy_c = map_arg.height - this.center_y;

        int xx = Utility.clamp(0, map_arg.width  - this.center_x, map_xx);
        int yy = Utility.clamp(0, map_arg.height - this.center_y, map_yy);

        this.map = map_arg;
        this.x_origin = xx;
        this.y_origin = yy;

        if ((xx_c != xx) || (yy_c != yy)) { return false; }
        return true;  // The specified coordinates were set, without adjustment
    } // set_center()

    public void Render(int frame) {
        if (this.Parent == null) { return; }  // Avoid rendering if not in a Form...is this check needful?
        int tile_width  = this.map.sheet.tile_wide_px;  // is this used now?
        int tile_height = this.map.sheet.tile_high_px;  // is this used now?
        GL.ClearColor(Color.Green);

        for (int view_yy = 0; view_yy < this.height_in_tiles; view_yy++) {
            for (int view_xx = 0; view_xx < this.width_in_tiles; view_xx++) {
                int map_xx = this.x_origin + view_xx;
                int map_yy = this.y_origin + view_yy;

                bool on_map = ((map_xx >= 0) &&
                               (map_xx < this.map.width) &&
                               (map_yy >= 0) &&
                               (map_yy < this.map.height));
                if (!on_map) { continue; }

                int pixel_xx = (view_xx * tile_width_px)  + tile_grid_offset_x_px;
                int pixel_yy = (view_yy * tile_height_px) + tile_grid_offset_y_px;

                // First, render sprites from MAP layers:
                foreach (int LL in MapLayers.MapRenderingOrder) {
                    ITileSprite sp = (ITileSprite) this.map.contents_at_LXY(LL, map_xx, map_yy);
                    if (sp != null) {
                        this.blit_square_tile(view_xx, view_yy, sp.texture(frame) );
                    }
                } // foreach(LL)

                // Second, render sprites on VIEWPORT layers:
                foreach (int LL in ViewPortLayers.ViewPortRenderingOrder) {
                    ITileSprite sp = (ITileSprite) this.contents_at_LXY(LL, view_xx, view_yy);
                    if (sp != null) {
                        this.blit_square_tile(view_xx, view_yy, sp.texture(frame) );
                    }
                } // foreach(LL)

            } // for(view_xx)
        } //  for(view_yy)
    } // Render()

    public void blit_square_tile(int tile_grid_xx, int tile_grid_yy, int texture_id) {
        // clamp tile_grid_xx
        // clamp tile_grid_yy
        double xx = tile_grid_xx * this.tile_width_px;
        double yy = tile_grid_yy * this.tile_height_px;

        // Define OpenGL vertex coordinates for a square centered on the origin (0.0, 0.0)
        double LL = -(this.tile_width_px / 2);
        double RR = +(this.tile_width_px / 2);
        double TT = -(this.tile_height_px / 2);  // OpenGL origin coordinate (0,0) at bottom left, we want top left
        double BB = +(this.tile_height_px / 2);  // OpenGL origin coordinate (0,0) at bottom left, we want top left

        double HALF_TILE_WW = this.tile_width_px / 2;
        double HALF_TILE_HH = this.tile_height_px / 2;
        const double angle = 0.0;

        GL.PushMatrix();
        GL.Translate((xx + HALF_TILE_WW), (yy + HALF_TILE_HH), 0);
        GL.Rotate(angle, 0.0, 0.0, -1.0);

        GL.BindTexture(TextureTarget.Texture2D, texture_id);

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


} // class
