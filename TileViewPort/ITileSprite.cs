using System.Drawing;


interface ITileSprite {
    int num_frames { get; }

    TileSheet tile_sheet(int frame);
    Image     image(int frame);
    Rectangle rect(int frame);
    int       texture(int frame);
} // interface

