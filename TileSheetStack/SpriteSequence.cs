using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileSheetStack
{
    class SpriteSequence
    {
        public TileSheetStack tilesheet { get; set; }
        public int[] frames { get; set; }
        public int num_frames { get { return frames.Length; } }

        public SpriteSequence(TileSheetStack arg_ts, int[] arg_frames)
        {
            tilesheet = arg_ts;
            int LL = arg_frames.Length;
            for (int nn = 0; nn < LL; nn++)
            {
                int ff = arg_frames[nn];
                if ((ff < 0) || (ff > tilesheet.max_index)) { throw new ArgumentException("Got frame index out of range"); }
            }
            frames = new int[LL];
            for (int nn = 0; nn < LL; nn++)
            {
                frames[LL] = arg_frames[nn];
            }
        } //

    } // class

} // namespace
