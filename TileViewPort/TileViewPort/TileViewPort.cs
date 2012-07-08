using System;
//using System.ComponentModel;
//using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.Collections.Generic;
using System.Text;


public class TileViewPort
{
    public TileViewPortControl control;

    public int width_tiles { get; private set; }
    public int height_tiles { get; private set; }

    public int Width;  // width_pixels
    public int Height; // height_pixels

    public SimpleMapV1 map { get; private set; }
    public IGridIterable[] layers;
    public ViewPortScrollingConstraint constraint { get; set; }

    private int X_origin;  // relative to map
    public int x_origin   // relative to map
    {
        get { return X_origin; }
        set { X_origin = GridUtility.Clamp(value, min_x_offset(), max_x_offset()); }
    } // x_origin()

    private int Y_origin;  // relative to map
    public int y_origin   // relative to map
    {
        get { return Y_origin; }
        set { Y_origin = GridUtility.Clamp(value, min_y_offset(), max_y_offset()); }
    } // y_origin()

    public int center_x() { return (width_tiles / 2); }
    public int center_y() { return (height_tiles / 2); }

    public int min_x_offset()
    {
        switch (constraint)
        {
            case ViewPortScrollingConstraint.EntireMap:
                return 0;
            case ViewPortScrollingConstraint.CenterTile:
                return -(center_x());
            case ViewPortScrollingConstraint.EdgeCorner:
                return -(width_tiles - 1);
            default:
                throw new Exception("Got impossible ViewPortScrollingConstraint");

        }
    } // min_x_offset()

    public int min_y_offset()
    {
        switch (constraint)
        {
            case ViewPortScrollingConstraint.EntireMap:
                return 0;
            case ViewPortScrollingConstraint.CenterTile:
                return -(center_y());
            case ViewPortScrollingConstraint.EdgeCorner:
                return -(height_tiles - 1);
            default:
                throw new Exception("Got impossible ViewPortScrollingConstraint");
        }
    } // min_y_offset()

    public int max_x_offset()
    {
        switch (constraint)
        {
            case ViewPortScrollingConstraint.EntireMap:
                return (map.width - this.width_tiles);
            case ViewPortScrollingConstraint.CenterTile:
                return (map.width - (center_x() + 1));
            case ViewPortScrollingConstraint.EdgeCorner:
                return (map.width - 1);
            default:
                throw new Exception("Got impossible ViewPortScrollingConstraint");
        }
    } // max_x_offset()

    public int max_y_offset()
    {
        switch (constraint)
        {
            case ViewPortScrollingConstraint.EntireMap:
                return (map.height - this.height_tiles);
            case ViewPortScrollingConstraint.CenterTile:
                return (map.height - (center_y() + 1));
            case ViewPortScrollingConstraint.EdgeCorner:
                return (map.height - 1);
            default:
                throw new Exception("Got impossible ViewPortScrollingConstraint");
        }
    } // max_y_offset()


    ///////////////////////////////////////////////////////////////////////////

    public TileViewPort(TileViewPortControl tvp,
                int ww, int hh,
                ViewPortScrollingConstraint constraint_arg,
                SimpleMapV1 map_arg, int map_xx, int map_yy)
    {
        if (tvp == null) { throw new ArgumentException("TileViewPort() - got null TileViewPortControl\n"); }
        if (map_arg == null) { throw new ArgumentException("TileViewPort() - got null Map\n"); }

        control = tvp;
        tvp.owner = this;

        width_tiles = ww;
        height_tiles = hh;
        constraint = constraint_arg;
        map = map_arg;
        x_origin = map_xx;
        y_origin = map_yy;

        this.Width = (width_tiles * map.sheet.tileWidth);
        this.Height = (height_tiles * map.sheet.tileHeight);

        if (Width > control.Width) { throw new ArgumentException("TileViewPort() - tiles width  too large for control\n"); }
        if (Height > control.Height) { throw new ArgumentException("TileViewPort() - tiles height too large for control\n"); }

        setControlEdgeCentering();

        layers = new IGridIterable[ViewPortLayers.COUNT];
        layers[ViewPortLayers.UI_Elements] = new DenseGrid(width_tiles, height_tiles, 0);
        layers[ViewPortLayers.UI_Elements].set_contents_at_XY(center_x(), center_y(), 272);  // Center
        layers[ViewPortLayers.UI_Elements].set_contents_at_XY(0, 0, 272);  // NW
        layers[ViewPortLayers.UI_Elements].set_contents_at_XY(width_tiles - 1, 0, 272);  // NE
        layers[ViewPortLayers.UI_Elements].set_contents_at_XY(0, height_tiles - 1, 272);  // SW
        layers[ViewPortLayers.UI_Elements].set_contents_at_XY(width_tiles - 1, height_tiles - 1, 272);  // SE


    } // TileViewPort(tvp, ww,hh, constraint, map,x,y)

    private void setControlEdgeCentering()
    {
        // If the control size is larger than the map displayed, 
        // we desire that the viewport be displayed centered within the control.
        // 
        // Setting the control size to an even number of pixels greater than needed 
        // for the intended tile width*height will thus provide a thin border around the tile region.
        int viewport_pixels_ww = width_tiles * map.sheet.tileWidth;
        int viewport_pixels_hh = height_tiles * map.sheet.tileHeight;

        int extra_ww = Math.Max(0, control.Width - viewport_pixels_ww);
        int extra_hh = Math.Max(0, control.Height - viewport_pixels_hh);

        control.left_pad = extra_ww / 2;
        control.top_pad = extra_hh / 2;
    } // setControlEdgeCentering()

    public object contents_at_LXY(int layer, int xx, int yy)
    {
        // TODO: The arg checking here implies that xx and yy are relative to the viewport, not the map...
        if (layer < MapLayers.MIN) { return null; }
        if (layer > MapLayers.MAX) { return null; }
        if (xx < 0) { return null; }
        if (xx >= width_tiles) { return null; }
        if (yy < 0) { return null; }
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
