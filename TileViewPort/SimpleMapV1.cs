using System;

// TODO: Perhaps split off the various utility / constants classes/enums/interfaces into their own files...

public class MapLayers  // Layers which exist on a map
{
    public const int Terrain    = 0;
    public const int Features   = 1;
    public const int Items      = 2;
    public const int Vehicles   = 3;
    public const int Beings     = 4;
    public const int Fields     = 5;  // Also fogs, fires, etc
    public const int Fog_of_War = 6;  // 

    public const int COUNT = 7;
    public const int MIN   = 0;
    public const int MAX   = Fog_of_War;

    public static int[] MapRenderingOrder = { Terrain, Beings };
} // MapLayers

public class ViewPortLayers  // Layers which exist on a ViewPort
{
    public const int UI_Elements = 0;  // Cursors, boundary lines, etc 
    // (Likely more than one layer in this set, once definite purposes are defined)

    public const int COUNT = 1;
    public const int MIN   = 0;
    public const int MAX   = UI_Elements;

    public static int[] ViewPortRenderingOrder = { UI_Elements };
} // class ViewPortLayers

public enum ScrollConstraint
{
    EntireMap,   // No ViewPort tile may be off-map
    CenterTile,  // The center tile of the ViewPort must be on-map
    EdgeCorner   // At least a corner of the ViewPort must be on-map
}

public interface IGridIterable
{
    int width  { get; set; }
    int height { get; set; }

    int min_x();
    int min_y();

    int center_x();
    int center_y();

    int max_x();
    int max_y();

    int contents_at_XY(int xx, int yy);
    int set_contents_at_XY(int xx, int yy, int new_contents);

    // TODO: What syntax to declare an indexer[x,y] in an interface?
    // public int this[int xx, int yy] { }
    // public int this[int index] { }

} // interface IGridIterable



public class SimpleMapV1
{
    public int width       { get; set; }
    public int height      { get; set; }
    public TileSheet sheet { get; private set; } // This belongs elsewhere, after implementation bootstrapping...
    public IGridIterable[] layers;
    // TODO: Add support for "default object/terrain", likely on a per-layer basis...

    public SimpleMapV1(int ww, int hh, TileSheet ts)
    {
        if ((ww < 1) || (ww > GridUtility2D.max_width )) { throw new ArgumentException("invalid width"); }
        if ((hh < 1) || (hh > GridUtility2D.max_height)) { throw new ArgumentException("invalid height"); }
        if (ts == null) { throw new ArgumentException("invalid tilesheet"); }

        width  = ww;
        height = hh;
        sheet  = ts;
        layers = new IGridIterable[MapLayers.COUNT];
        layers[MapLayers.Terrain] = new MapCompositedLayer(width, height);
        layers[MapLayers.Beings]  = new MapSparseGridLayer(width, height, null);
    } // SimpleMapV1() with MapCompositedLayer


    // TODO: 
    // Some set of methods to add contents to the various layers.
    // One method per layer?  May be needful, as the different layers have different types of contents...
    // 

    public void AddTerrainRegion(DenseGrid RR, int xx, int yy)
    {
        MapCompositedLayer canvas = (MapCompositedLayer) layers[MapLayers.Terrain];
        canvas.AddContentRegion(RR, xx, yy);
    }


    public object contents_at_LXY(int layer, int xx, int yy)
    {
        if (layer < MapLayers.MIN) { return null; }
        if (layer > MapLayers.MAX) { return null; }
        if (xx <  0)      { return null; }
        if (xx >= width)  { return null; }
        if (yy <  0)      { return null; }
        if (yy >= height) { return null; }

        if (layers[layer] == null)
        {
            throw new ArgumentException("Got invalid layer");
        }
        // More refactoring coming up, once the map data is object_IDs rather than sprite_IDs...
        // For that matter, does one generally want (the obj reference, or the obj ID) to be returned from such a method?
        // Possibly we want an overload to get either?  Study how it is used in practice, refactor to match most convenient mode of use...
        int sprite_ID = layers[layer].contents_at_XY(xx, yy);
        return ObjectRegistrar.Sprites.obj_for_ID(sprite_ID);
    } // contents_at_LXY()



} // class SimpleMapV1


// TODO: Add constructor for new GridLayer of (ww * hh) with (fill)
// TODO: Add some "blit" and "add element" methods
