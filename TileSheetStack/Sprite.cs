using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileSheetStack
{
    class Sprite
    {
        public TileSize tilesize { get; private set; }

        // TODO:
        // - Add support for "facings"
        // - Add support for creating "wave" sprites

        public SpriteSequence seq { get; private set; }
        public int current_frame { get; set; }  // TODO: set {value = SomeClass.Clamp(0, num_frames=1); _current_frame = value; }
        public int num_frames    { get { return seq.num_frames; } }
        public int frame_step    { get; set; }

        public Sprite(TileSize arg_ts, SpriteSequence arg_seq)
        {
            tilesize = arg_ts;
            seq      = arg_seq;
        } // 

        public int advance_frame()
        {
            // It may be that TileViewPort or other users of this class
            // will need some way to be told that one or more sprites/tiles
            // need to be redrawn.
            // 
            // Then again, maybe TileViewPort just unconditionally redraws each animation tick?
            // (That would be something like 2..12 times per second)  Not sure yet.
            current_frame += frame_step;
            current_frame %= num_frames;
            return current_frame;
        }


    } // class

} // namespace
