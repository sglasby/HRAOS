using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileSheetStack
{
    public interface IGrid3D
    {
        int width  { get; set; }
        int height { get; set; }
        int depth  { get; set; }

        int min_x { get; }
        int min_y { get; }
        int min_z { get; }

        int center_x { get; }
        int center_y { get; }
        // Note that center_z is not a useful concept

        int max_x { get; }
        int max_y { get; }
        int max_z { get; }

        int index(int xx, int yy, int zz);
        int index(int xx, int yy);  // Presumes z = 0

        int max_index { get; }  // { return (width * height * depth) - 1; }

        int x_for_index(int ii);  // { return (ii % (width * height)) % height; }
        int y_for_index(int ii);  // { return (ii % (width * height)) / width;  }
        int z_for_index(int ii);  // { return (ii % (width * height));          }

    } // interface

} // namespace
