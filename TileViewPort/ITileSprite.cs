using System.Drawing;
using System.Drawing.Imaging;

interface ITileSprite {
    TileSheet   sheet      { get; }
    int         num_frames { get; }

    Bitmap[]    bitmap     { get; }
    Rectangle[] rect       { get; }
    int[]       texture    { get; }

    void blit_square_tile(int pixel_xx, int pixel_yy, int frame);
    void GDI_Draw_Tile(Graphics gg, int xx, int yy, ImageAttributes attrib, int frame);
} // interface
