using System;
using System.Windows.Forms;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;


public class TVPC : OpenTK.GLControl {
    // public int Width;   // Width  in pixels, inherited from System.Windows.Forms.Control
    // public int Height;  // Height in pixels, inherited from System.Windows.Forms.Control

    // Note that (tile_width_px, tile_height_px) could differ from that specified in a TileSheet, thus rendering smaller/larger...
    public int tile_width_px;   // Width  to render a single tile, in pixels
    public int tile_height_px;  // Height to render a single tile, in pixels

    private bool loaded;  // Set in OnLoad()
    public  int  quanta = 0;  // Current animation frame for max-speed UI elements, such as "marching ants" selections
    public  int  frame  = 0;  // Current animation frame for map tiles

    public TilingModes      tiling_mode;
    public SimpleMapV1      map { get; private set; }
    public IGridIterable[]  layers = null;
    public ScrollConstraint scroll_constraint { get; set; }

    public int tile_grid_offset_x_px;  // offset for smooth-scrolling of tile grid rendering
    public int tile_grid_offset_y_px;  // offset for smooth-scrolling of tile grid rendering

    private int _padding_px;
    public int padding_px {
        // Number of pixels between tiles in the tile grid 
        // Usually set to zero, but useful for debugging and for printed maps
        get { return _padding_px; }
        set { _padding_px = Utility.clamp(0, this.tile_width_px, value); }
    }

    // public bool render_only_integral_tiles { get; set; }  // _maybe_ add such a feature, after all else works smoothly...

    public int width_in_tiles {
        get { return (this.Width / this.tile_width_px); }
    }
    public int height_in_tiles {
        get { return (this.Height / this.tile_height_px); }
    }

    private void allocate_layers() {
        this.layers = new IGridIterable[ViewPortLayers.COUNT];
        this.layers[ViewPortLayers.UI_Elements] = new DenseGrid(width_in_tiles, height_in_tiles, 0);
    }

    private int X_origin;
    private int Y_origin;
    public int x_origin {
         get { return X_origin; }
         set { X_origin = GridUtility2D.Clamp(value, min_x_offset, max_x_offset); }
    }
    public int y_origin {
         get { return Y_origin; }
         set { Y_origin = GridUtility2D.Clamp(value, min_y_offset, max_y_offset); }
    }

    public int center_x { get { return (width_in_tiles  / 2); } }
    public int center_y { get { return (height_in_tiles / 2); } }

    public int max_x { get { return width_in_tiles  - 1; } }
    public int max_y { get { return height_in_tiles - 1; } }

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
                ScrollConstraint constraint_arg) {
        // Order of setup of things in the parent form (form1) 
        // makes it needful that the (map, x, y) get set AFTER the constructor.
        // As such, the constructor creates a TVPC with map,x,y (null,0,0)
        // and later on, set_origin() or set_center() will set those, and call allocate_layers()
        // 
        if (control_width_px_arg  < 1) { throw new ArgumentException("Got zero width for TVP control");  }
        if (control_height_px_arg < 1) { throw new ArgumentException("Got zero height for TVP control"); }
        if (tile_width_px_arg  < 1)    { throw new ArgumentException("Got zero tile_width_px_arg");  }
        if (tile_height_px_arg < 1)    { throw new ArgumentException("Got zero tile_height_px_arg"); }

        this.Width  = control_width_px_arg;
        this.Height = control_height_px_arg;

        this.tile_width_px  = tile_width_px_arg;
        this.tile_height_px = tile_height_px_arg;

        this.scroll_constraint = constraint_arg;
        this.tiling_mode       = TilingModes.Square;
        this.padding_px      = 0;

        this.tile_grid_offset_x_px = 0;  // Partial-tile scrolling can result in non-zero values
        this.tile_grid_offset_y_px = 0;  // Partial-tile scrolling can result in non-zero values

        this.Load   += this.OnLoad;
        this.Paint  += this.OnPaint;
        this.Resize += this.OnResize;

        // Pass all key events to the parent form:
        this.PreviewKeyDown += new PreviewKeyDownEventHandler(OnPreviewKeyDown);
    } // TVPC()

    // possibly other constructors, specifying control.Width, control.Height, etc...

    //private void setControlEdgeCentering() { /*...*/ }  // Taking a different approach to this functionality...
    // 
    // It would only be desirable for there to be centering/padding 
    // of the tile grid with a non-integral-tiles this.Size
    // if a mode "this.render_only_integral_tiles" existed, and was in effect.
    // 
    // Otherwise, the entire size of (this) will be used for rendering tile contents
    // (including partial tiles along any/all edges),
    // and this.tile_grid_offset_(x|y)_px will be used for partial-tile offset 
    // of the tile grid relative to the control origin (0,0)

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
        return ObjectRegistrar.Sprites.object_for_ID(sprite_ID);
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

        if (map_arg == null) {
            this.map      = null;
            this.x_origin = 0;
            this.y_origin = 0;
            return false;
            //throw new ArgumentException("Got null map_arg");
        }
        int xx = Utility.clamp(0, map_arg.width,  map_xx);
        int yy = Utility.clamp(0, map_arg.height, map_yy);

        this.map = map_arg;
        this.x_origin = xx;
        this.y_origin = yy;
        allocate_layers();

        // The resulting coordinates may differ from those specified by args,
        // either due to out-of-map-bounds coordinates which were clamp()ed,
        // or due to the scroll_constraint.  
        // If so, return false, so that our caller (if it cares) can distinguish.
        if ((map_xx != xx) || (map_yy != yy)) { return false; }
        return true;  // The specified coordinates were set, without adjustment
    } // set_origin()

    public bool set_center(SimpleMapV1 map_arg, int map_xx, int map_yy) {
        // Set the map and origin such that the center tile 
        // of the viewport is the tile with the specified coordinates, 
        // or as close as can be managed, given scrolling constraints.
        if (map_arg == null) { throw new ArgumentException("Got null map_arg"); }

        int xx = Utility.clamp(0, map_arg.width,  map_xx);
        int yy = Utility.clamp(0, map_arg.height, map_yy);

        this.map      = map_arg;
        this.x_origin = xx - center_x;
        this.y_origin = yy - center_y;
        allocate_layers();

        // The resulting coordinates may differ from those specified by args,
        // either due to out-of-map-bounds coordinates which were clamp()ed,
        // or due to the scroll_constraint.  
        // If so, return false, so that our caller (if it cares) can distinguish.
        if (this.x_origin != map_xx - center_x) { return false; }
        if (this.y_origin != map_yy - center_y) { return false; }
        return true;  // The specified coordinates were set, without adjustment
    } // set_center()

    public void Render() {
        if (this.Parent == null) { return; }  // Avoid rendering if not in a Form...is this check needful?
        GL.ClearColor(Color.Green);  // This color will appear on off-map regions, and any between-tiles padding pixels

        // The various coordinates which are used:
        // ( view_xx,  view_yy) are viewport tile coordinates of the tile to render
        // (  map_xx,   map_xx) are      map tile coordinates of a map tile to render (or else, off-map)
        // (pixel_xx, pixel_yy) are         pixel coordinates on screen for the (top left) origin of the tile to render

        // Note:
        // If any layer has over-sized tile contents 
        // (cursor surrounding the selected tile, larger 1x1 tile which spills over a bit into other tiles)
        // then the rendering loop needs to be: 
        //     foreach(layer), for(y), for(x)
        // rather than: 
        //     for(y), for(x), foreach(layer)
        // Otherwise, tiles rightwards/below the over-sized tile will overlap it, 
        // preventing any pixels rightwards/below the tile bounds from being seen.
        // 
        // For the moment, only "cursors" in the ViewPortLayers.UI_Elements layer 
        // are over-sized, so I am cheating a bit.

        // First, render tiles on MAP layers:
        for (int view_yy = 0; view_yy < this.height_in_tiles; view_yy++) {
            for (int view_xx = 0; view_xx < this.width_in_tiles; view_xx++) {
                int map_xx = this.x_origin + view_xx;
                int map_yy = this.y_origin + view_yy;

                int pixel_xx = (view_xx * (this.tile_width_px  + this.padding_px)) + this.tile_grid_offset_x_px;
                int pixel_yy = (view_yy * (this.tile_height_px + this.padding_px)) + this.tile_grid_offset_y_px;

                bool on_map = ((map_xx >= 0) &&
                               (map_xx < this.map.width) &&
                               (map_yy >= 0) &&
                               (map_yy < this.map.height));
                if (!on_map) { continue; }

                // First, render sprites from MAP layers:
                foreach (int LL in MapLayers.MapRenderingOrder) {
                    ITileSprite sp = (ITileSprite) this.map.contents_at_LXY(LL, map_xx, map_yy);
                    if (sp != null) {
                        sp.blit_square_tile(pixel_xx, pixel_yy, this.frame);
                    }
                } // foreach(LL)
            } // for(view_xx)
        } // for(view_yy)

        // Then, render sprites on VIEWPORT layers:
        for (int view_yy = 0; view_yy < this.height_in_tiles; view_yy++) {
            for (int view_xx = 0; view_xx < this.width_in_tiles; view_xx++) {
                int pixel_xx = (view_xx * (this.tile_width_px + this.padding_px)) + this.tile_grid_offset_x_px;
                int pixel_yy = (view_yy * (this.tile_height_px + this.padding_px)) + this.tile_grid_offset_y_px;

                foreach (int LL in ViewPortLayers.ViewPortRenderingOrder) {
                    ITileSprite sp = (ITileSprite) this.contents_at_LXY(LL, view_xx, view_yy);
                    if (sp != null) {
                        // Hack: Center cursor tile larger than 32x32.  Need a more proper way of handling this...
                        int ff = frame % sp.num_frames;
                        int offset_x = (this.tile_width_px  - sp.rect[ff].Width)  / 2;
                        int offset_y = (this.tile_height_px - sp.rect[ff].Height) / 2;
                        sp.blit_square_tile(pixel_xx + offset_x, pixel_yy + offset_y, this.frame /*this.quanta*/ );
                    }
                } // foreach(LL)

            } // for(view_xx)
        } //  for(view_yy)
    } // Render()

    private void OnLoad(object sender, EventArgs e) {
        SetupViewport();
        //GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Texture2D);
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

        // In theory, these (plus sheet.MakeTransparent(Color.Magenta) in TileSheet.cs) should enable transparency...
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

        GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
        TileSprite.Check_for_GL_error("In TVPC.OnLoad() after calling GL.Hint()");

        loaded = true;
    } // OnLoad()

    private void SetupViewport() {
        // This exists here, in part because there are two callers:  glControl1_Load() and glControl1_Resize()
        // TODO: This code may belong in TileViewPortControl (when it isa GLControl)
        GL.MatrixMode(MatrixMode.Projection);  // Why MatrixMode.Projection here, and .Modelview in glControl1_Paint() ?
        GL.LoadIdentity();
        GL.Ortho(0, this.Width, this.Height, 0, 0, 1);  // OpenGL origin coordinate (0,0) is at bottom left; we want origin at top left
        GL.Viewport(0, 0, this.Width, this.Height);     // Use all of the GLControl area for the GL Viewport
        TileSprite.Check_for_GL_error("In TVPC.SetupViewport() after calling GL.Viewport()");
    } // SetupViewport()

    private void OnPaint(object sender, PaintEventArgs e) {
        if (!loaded)
            return;

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.MatrixMode(MatrixMode.Modelview);    // Why MatrixMode.Modelview here, and .Projection in SetupViewport() ?
        GL.LoadIdentity();
        this.Render();

        GL.Flush();
        this.SwapBuffers();
    } // glControl1_Paint()

    private void OnResize(object sender, EventArgs e) {
        // Hmmm...when exactly does the GLControl get resized?  (Seems to be: Upon construction, and then when?)
        allocate_layers();
        SetupViewport();
        this.Invalidate();
    }

    public void OnPreviewKeyDown(object sender, PreviewKeyDownEventArgs ee) {
        // By default, certain key events are pre-processed by the Form,
        // before any contained Control gets that key event.
        // This occurs for (TAB, RETURN, ESC, UP ARROW, DOWN ARROW, LEFT ARROW, RIGHT ARROW),
        // which are of interest to us in our handling.
        // 
        // We desire to handle key events in the form, thus this method is
        // an event handler for PreviewKeyDown which causes the key events
        // for those keys to be passed on to the form, by means of
        // setting e.IsInputKey = true for the wanted keys. 
        //     http://msdn.microsoft.com/en-us/library/system.windows.forms.control.previewkeydown.aspx

        switch (ee.KeyCode) {
            case Keys.Tab:
            case Keys.Return:
            case Keys.Escape:
                ee.IsInputKey = true;
                break;

            case Keys.Up:
            case Keys.Down:
            case Keys.Left:
            case Keys.Right:
                ee.IsInputKey = true;
                break;
        }
    } // OnPreviewKeyDown()

} // class

