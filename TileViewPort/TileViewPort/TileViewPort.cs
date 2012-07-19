using System;

public class TileViewPort
{
    public TileViewPortControl control;

    public int width_tiles  { get; private set; }
    public int height_tiles { get; private set; }

    public int Width;  // width_pixels
    public int Height; // height_pixels

    public SimpleMapV1 map { get; private set; }
    public IGridIterable[] layers;
    public ScrollConstraint constraint { get; set; }

    private int X_origin;  // relative to map
    public  int x_origin   // relative to map
    {
        get { return X_origin; }
        set { X_origin = GridUtility.Clamp(value, min_x_offset(), max_x_offset()); }
    } // x_origin()

    private int Y_origin;  // relative to map
    public  int y_origin   // relative to map
    {
        get { return Y_origin; }
        set { Y_origin = GridUtility.Clamp(value, min_y_offset(), max_y_offset()); }
    } // y_origin()

    public int center_x() { return (width_tiles  / 2); }
    public int center_y() { return (height_tiles / 2); }

    public int min_x_offset()
    {
        switch (constraint)
        {
            case ScrollConstraint.EntireMap:
                return 0;
            case ScrollConstraint.CenterTile:
                return -(center_x());
            case ScrollConstraint.EdgeCorner:
                return -(width_tiles - 1);
            default:
                throw new Exception("Got impossible ViewPortScrollingConstraint");

        }
    } // min_x_offset()

    public int min_y_offset()
    {
        switch (constraint)
        {
            case ScrollConstraint.EntireMap:
                return 0;
            case ScrollConstraint.CenterTile:
                return -(center_y());
            case ScrollConstraint.EdgeCorner:
                return -(height_tiles - 1);
            default:
                throw new Exception("Got impossible ViewPortScrollingConstraint");
        }
    } // min_y_offset()

    public int max_x_offset()
    {
        switch (constraint)
        {
            case ScrollConstraint.EntireMap:
                return (map.width - this.width_tiles);
            case ScrollConstraint.CenterTile:
                return (map.width - (center_x() + 1));
            case ScrollConstraint.EdgeCorner:
                return (map.width - 1);
            default:
                throw new Exception("Got impossible ViewPortScrollingConstraint");
        }
    } // max_x_offset()

    public int max_y_offset()
    {
        switch (constraint)
        {
            case ScrollConstraint.EntireMap:
                return (map.height - this.height_tiles);
            case ScrollConstraint.CenterTile:
                return (map.height - (center_y() + 1));
            case ScrollConstraint.EdgeCorner:
                return (map.height - 1);
            default:
                throw new Exception("Got impossible ViewPortScrollingConstraint");
        }
    } // max_y_offset()


    ///////////////////////////////////////////////////////////////////////////

    public TileViewPort(TileViewPortControl tvp,
                int ww, int hh,
                ScrollConstraint constraint_arg,
                SimpleMapV1 map_arg, int map_xx, int map_yy)
    {
        if (tvp     == null) { throw new ArgumentException("TileViewPort() - got null TileViewPortControl\n"); }
        if (map_arg == null) { throw new ArgumentException("TileViewPort() - got null Map\n"); }

        control   = tvp;
        tvp.owner = this;

        width_tiles  = ww;
        height_tiles = hh;
        constraint   = constraint_arg;
        map          = map_arg;
        x_origin     = map_xx;
        y_origin     = map_yy;

        this.Width  = (width_tiles  * map.sheet.tile_wide_px);
        this.Height = (height_tiles * map.sheet.tile_high_px);

        if (Width  > control.Width)  { throw new ArgumentException("TileViewPort() - tiles width  too large for control\n"); }
        if (Height > control.Height) { throw new ArgumentException("TileViewPort() - tiles height too large for control\n"); }

        setControlEdgeCentering();
        layers = new IGridIterable[ViewPortLayers.COUNT];
        layers[ViewPortLayers.UI_Elements] = new DenseGrid(width_tiles, height_tiles, 0);
    } // TileViewPort(tvp, ww,hh, constraint, map,x,y)

    private void setControlEdgeCentering()
    {
        // If the control size is larger than the map displayed, 
        // we desire that the viewport be displayed centered within the control.
        // 
        // Setting the control size to an even number of pixels greater than needed 
        // for the intended tile width*height will thus provide a thin border around the tile region.
        // TODO: Not working, see below...
        int viewport_pixels_ww = width_tiles  * map.sheet.tile_wide_px;
        int viewport_pixels_hh = height_tiles * map.sheet.tile_high_px;

        int extra_ww = Math.Max(0, control.Width  - viewport_pixels_ww);
        int extra_hh = Math.Max(0, control.Height - viewport_pixels_hh);

        // TODO: The edge-padding is all on the top and right; review this once the myriad duplication has been refactored out...
        control.left_pad = extra_ww / 2;
        control.top_pad  = extra_hh / 2;
    } // setControlEdgeCentering()

    public object contents_at_LXY(int layer, int xx, int yy)
    {
        // TODO: The arg checking here implies that xx and yy are relative to the viewport, not the map...
        if (layer < MapLayers.MIN) { return null; }
        if (layer > MapLayers.MAX) { return null; }
        if (xx <  0)            { return null; }
        if (xx >= width_tiles)  { return null; }
        if (yy <  0)            { return null; }
        if (yy >= height_tiles) { return null; }

        if (layers[layer] == null)
        {
            throw new ArgumentException("Got invalid layer");
        }
        // More refactoring coming up, once the map data is object_IDs rather than sprite_IDs...
        int sprite_ID = layers[layer].contents_at_XY(xx, yy);
        return ObjectRegistrar.Sprites.obj_for_ID(sprite_ID);
    } // contents_at_LXY()

    public void set_origin(SimpleMapV1 map_arg, int map_xx, int map_yy)
    {
        if (map_arg == null) { throw new ArgumentException("Got null map_arg\n"); }
        map_xx = GridUtility.Clamp(map_xx, 0, map_arg.width);
        map_yy = GridUtility.Clamp(map_yy, 0, map_arg.height);

        map = map_arg;
        x_origin = map_xx;
        y_origin = map_yy;
        // Perhaps useful to return a bool for whether the set value is equal to the specified?
    } // set_origin()

    public void set_center(SimpleMapV1 map_arg, int map_xx, int map_yy)
    {
        // Sets the map and origin such that the center tile of the viewport
        // is in the specified x,y (or as close as can be managed, given scrolling constraints)
        if (map_arg == null) { throw new ArgumentException("Got null map_arg\n"); }
        map_xx = GridUtility.Clamp(map_xx, 0, map_arg.width);
        map_yy = GridUtility.Clamp(map_yy, 0, map_arg.height);

        map = map_arg;
        x_origin = (map_xx - center_x());
        y_origin = (map_yy - center_y());
        // Perhaps useful to return a bool for whether the set value is equal to the specified?
    } // set_center()

    public void Invalidate()
    {
        control.Invalidate();
    } // Invalidate()



} // class TileViewPort
