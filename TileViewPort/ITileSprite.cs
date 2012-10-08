using System.Drawing;
using System.Drawing.Imaging;

interface ITileSprite {
    int num_frames { get; }

    TileSheet tile_sheet(int frame);
    Image     image(int frame);
    Rectangle rect(int frame);
    int       texture(int frame);

    void GDI_Draw_Tile(Graphics gg, int xx, int yy, ImageAttributes attrib, int frame);
} // interface

