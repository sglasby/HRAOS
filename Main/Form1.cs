using System;
using System.Diagnostics;  // for Stopwatch
using System.Drawing;
using System.Windows.Forms;

using OpenTK;
using OpenTK.Graphics.OpenGL;


namespace OpenGLForm {
    // TODO: A better name for this Form would be a good thing...
    public partial class Form1 : Form {
        Stopwatch   sw = new Stopwatch(); // available to all event handlers

        TileSheet   ts;
        TileSheet   ui_ts;
        TileSheet   f1234;
        TileSheet   wp_ts;
        TileSheet   LF;

        SimpleMapV1 map;
        TVPC        tvpc;

        private Label label1;

        public Form1() {
            this.tvpc = new TVPC(512, 512, 32, 32, ScrollConstraint.CenterTile);
            this.tvpc.padding_px = 0;
            this.tvpc.Location = new Point(10, 10);
            //this.tvpc.Anchor = AnchorStyles.Left | AnchorStyles.Right;  // Not working right...may indicate a bug in TVPC somewhere...
            this.tvpc.TabIndex = 0;

            this.label1 = new Label();
            this.label1.BackColor = SystemColors.ControlDark;
            this.label1.Location  = new System.Drawing.Point(12, 512 + 10 + 10);
            this.label1.Size      = new System.Drawing.Size(600, 2*13);
            this.label1.TabIndex  = 1;
            // label1.Text Is set later in Accumulate()

            this.Controls.Add(this.label1);
            this.Controls.Add(this.tvpc);

            this.ClientSize = new System.Drawing.Size(800, 600);  // Results in .Size of (808 x 634) with titlebar, window borders, etc (depending on theme settings)
            this.Text = "Main Window Title";
            this.Shown += this.OnShown;  // Will execute once, AFTER Load and Activate events
            // Note: The order of (Load, Activated, Shown) can be altered, if a MessageBox() is invoked, therefore: do not use MessageBox()
            this.Paint += this.OnPaint;  // Using this to test TileSprite.GDI_Draw_Tile()

            Application.Idle += Application_Idle;
            sw.Start(); // start the Stopwatch, which is used to alter the label1.Text via Accumulate() and Animate()

            // Set up key handling by the Form:
            // (Some mode-specific key handling may still be done by individual controls)
            // In addition to the setup here, contained controls (such as this.tvpc)
            // may need to handle the PreviewKeyDown event, as seen in TVPC.cs OnPreviewKeyDown()
            this.KeyPreview  = true;
            this.KeyDown    += new KeyEventHandler(OnKeyDown);
            // When additional controls are added into this Form, revisit this if needed...

        } // Form1()

        void OnShown(object sender, EventArgs ee) {
            // Note: Events are fired in the order (Load, Activated, Shown),
            //     It is said that using a MessageBox() can perturb the order of these events,
            //     causing Shown to occur before Load.  
            //     http://stackoverflow.com/questions/3070163/order-of-form-load-form-shown-and-form-activated-events
            //     It is also said that "depending on the order of events fired" is undesirable / bad style;
            //     so perhaps once I understand what the idiomatic alternative would be,
            //     this should be changed.
            // 
            // To call any GL methods (such as setting the GL Viewport, or loading textures)
            // the OpenGL system must be initialized, which occurs upon the 'Load' event
            // firing for a Form which contains a GLControl.
            //     http://www.opentk.com/doc/chapter/2/glcontrol
            //     See also, regarding OpenTK.Graphics.GraphicsContext:
            //         http://www.opentk.com/book/export/html/140
            // 
            // For this reason, the GL setup, and GL texture loading (via TileSheet constructor calls) 
            // code has been moved here, in a method we set up to be called upon the 'Shown' event
            // (which is fired upon the first display of this Form).

            ts = new TileSheet(@"Main/U4.B_enhanced-32x32.png", 16, 16);  // causes GL textures to be loaded, needs some GL setup prior...
            f1234 = new TileSheet(@"Main/example_all_facings.4_frames.intra_1.png", 4, 9, 32, 32, 1, 1, 1, 1);
            //ui_ts = new TileSheet(@"Main/bright_marquee.frame_1.png", 4, 4);
            //ui_ts = new TileSheet(@"Main/bright_marquee.frame_1.alpha.png", 4, 4);
            //ui_ts = new TileSheet(@"Main/bright_marquee.frame_1.alpha.2.png", 4, 4);
            ui_ts = new TileSheet(@"Main/bright_marquee.frame_1234.png", 4, 1);
            wp_ts = new TileSheet(@"Main/whirlpool_bright.png", 4, 1);

            // TODO: 
            // After setting up all these AnimTileSprite instances, the utility is clear for
            // various constructor overloads which infer the wanted StaticTileSprite IDs...
            AnimTileSprite anim_blue_wiz = new AnimTileSprite(ts, 32, 33);  // Note the one-index constructor
            AnimTileSprite anim_red_wiz  = new AnimTileSprite(ts, ts[0, 14], ts[1, 14], ts[2, 14], ts[3, 14]);  // Note the 2-index constructor

            // Counters for 3 frames (A,B,C) and for 4 frames (1,2,3,4)
            // This illustrates why the master frame cycle need be the Least Common Multiple of (3,4) 
            // (or whatever other set of ITileSprite.num_frames values).
            AnimTileSprite count_ABC     = new AnimTileSprite(ts, ts[0, 6], ts[1, 6], ts[2, 6]);
            AnimTileSprite count_1234    = new AnimTileSprite(f1234, f1234[0, 0], f1234[1, 0], f1234[2, 0], f1234[3, 0]);

            AnimTileSprite whirlpool = new AnimTileSprite(wp_ts, 0, 1, 2, 3);

            LF = new TileSheet(@"Main/lava.wave_down.speed_4.frames_8.png", 8, 1);  // LF == LavaFlow
            AnimTileSprite lava_flow = new AnimTileSprite(LF, 0, 1, 2, 3, 4, 5, 6, 7);

            // TileSheet TW = new TileSheet(@"Main/example_wave_test.intra_1.png", 1, 9);  // Will need WaveTileSprite to support this...

            int[] path_rect_5x4 = new int[]
            { // 5 = grass, 7 = trees, 58 = boulder
               58,  5,  5,  7,  5,
                5,  5,  5,  7,  7,
                7,  7,  7,  7,  0,
                5,  5,  7,  5,  5,
            };

            int lava_ID = ts[12, 4].ID;  // Lava
            //DenseGrid map_16x64 = new DenseGrid(16, 64, lava_ID);  // StaticTileSprite lava
            DenseGrid map_16x64 = new DenseGrid(16, 64, lava_flow.ID);  // AnimTileSprite flowing lava

            DenseGrid flip_none = new DenseGrid(5, 4, path_rect_5x4);  // Test with width != height, the better to see the rotations and flips
            DenseGrid flip_we   = flip_none.Flip_WE();
            DenseGrid flip_ns   = flip_none.Flip_NS();
            DenseGrid flip_wens = flip_we.Flip_NS();

            DenseGrid.BlitFromAOntoB(flip_none, map_16x64, 1, 1);
            DenseGrid.BlitFromAOntoB(flip_we,   map_16x64, 7, 1);
            DenseGrid.BlitFromAOntoB(flip_ns,   map_16x64, 1, 7);
            DenseGrid.BlitFromAOntoB(flip_wens, map_16x64, 7, 7);

            DenseGrid flip_none_rot090 = flip_none.Rotate090();
            DenseGrid flip_we_rot090   = flip_we.Rotate090();
            DenseGrid flip_ns_rot090   = flip_ns.Rotate090();
            DenseGrid flip_wens_rot090 = flip_wens.Rotate090();

            DenseGrid.BlitFromAOntoB(flip_none_rot090, map_16x64, 1, 52);
            DenseGrid.BlitFromAOntoB(flip_we_rot090,   map_16x64, 7, 52);
            DenseGrid.BlitFromAOntoB(flip_ns_rot090,   map_16x64, 1, 58);
            DenseGrid.BlitFromAOntoB(flip_wens_rot090, map_16x64, 7, 58);

            map = new SimpleMapV1(16, 64, ts);
            map.AddTerrainRegion(map_16x64, 0, 0);

            //tvp = new TileViewPort(this.tvp_control,
            //    15, 15,
            //    //ViewPortScrollingConstraint.EntireMap,
            //    ScrollConstraint.CenterTile,
            //    //ViewPortScrollingConstraint.EdgeCorner, 
            //    map, 0, 0);
            tvpc.scroll_constraint = ScrollConstraint.CenterTile;
            tvpc.set_center(map, 2, 2);

            // Add some elements to the Beings layer of the Map:  // TODO: Still using hard-coded Sprite ID values here...
            map.layers[MapLayers.Beings].set_contents_at_XY( 8,  7, 21);  // Horse

            map.layers[MapLayers.Beings].set_contents_at_XY( 8,  7, 21);  // Horse
            map.layers[MapLayers.Beings].set_contents_at_XY( 4, 15, 21);  // Horse
            map.layers[MapLayers.Beings].set_contents_at_XY( 8, 20, 33);  // Wizard
            map.layers[MapLayers.Beings].set_contents_at_XY( 3, 25, 70);  // Force field
            map.layers[MapLayers.Beings].set_contents_at_XY(10, 30, 29);  // Stair down
            map.layers[MapLayers.Beings].set_contents_at_XY( 9, 35, 30);  // Ruin
            map.layers[MapLayers.Beings].set_contents_at_XY( 6, 40, 45);  // Archer
            map.layers[MapLayers.Beings].set_contents_at_XY(12, 45, 23);  // Purple tiles
            map.layers[MapLayers.Beings].set_contents_at_XY( 5, 50, 19);  // Ship

            map.layers[MapLayers.Beings].set_contents_at_XY(2, 1, anim_blue_wiz.ID);  // Blue Wizard, animated (2 frames)
            map.layers[MapLayers.Beings].set_contents_at_XY(3, 3, anim_red_wiz.ID);   // Red  Wizard, animated (4 frames)

            map.layers[MapLayers.Beings].set_contents_at_XY(4, 1, count_ABC.ID);   // 3 frames (A,B,C)
            map.layers[MapLayers.Beings].set_contents_at_XY(5, 1, count_1234.ID);  // 4 frames (1,2,3,4)

            map.layers[MapLayers.Beings].set_contents_at_XY(0, 0, whirlpool.ID);

            // Add some elements to the UI_elements layer of the TileViewPort:

            //int reticle = ui_ts[3, 3].ID;  // avoiding hard-coding Sprite ID 272
            AnimTileSprite anim_reticle = new AnimTileSprite(ui_ts, 0, 1, 2, 3);
            int reticle = anim_reticle.ID;
            tvpc.layers[ViewPortLayers.UI_Elements].set_contents_at_XY(tvpc.center_x, tvpc.center_y, reticle);  // Center
            tvpc.layers[ViewPortLayers.UI_Elements].set_contents_at_XY(0,             0,             reticle);  // NW
            tvpc.layers[ViewPortLayers.UI_Elements].set_contents_at_XY(tvpc.max_x,    0,             reticle);  // NE
            tvpc.layers[ViewPortLayers.UI_Elements].set_contents_at_XY(0,             tvpc.max_y,    reticle);  // SW
            tvpc.layers[ViewPortLayers.UI_Elements].set_contents_at_XY(tvpc.max_x,    tvpc.max_y,    reticle);  // SE

        } // OnShown()


        int x_pixel_shift;
        public void OnKeyDown(object sender, KeyEventArgs ee) {
            // By default, certain key events are pre-processed by the Form,
            // before any contained Control gets that key event.
            // This occurs for (TAB, RETURN, ESC, UP ARROW, DOWN ARROW, LEFT ARROW, RIGHT ARROW),
            // which are of interest to us in our handling.
            //     
            // The solution is EITHER 
            //     - To handle key events in the control, and to implement
            //       an override for IsInputKey() on the control
            //       that wants the events, returning true for the wanted keys.
            //       http://msdn.microsoft.com/en-us/library/system.windows.forms.control.isinputkey.aspx
            // 
            //     - To handle key events in the form, and to implement 
            //       an event handler for PreviewKeyDown on control(s) which would otherwise "eat" the key events, 
            //       setting e.IsInputKey = true for the wanted keys. 
            //       http://msdn.microsoft.com/en-us/library/system.windows.forms.control.previewkeydown.aspx
            //
            // (Fairly sure now, that at least _most_ key handling should be in the main form;
            // however, there might be some contexts where control-centric key handling is wanted in addition.)
            //
            // At this point, I am not certain whether we will want to do key handling
            // exclusively in the form, or in (various) controls as well -- this
            // depends on how the UI is to be structured, and some controls I am planning
            // (and how usage via keyboard-only would work) might work best if controls
            // did some of the key handling.
            // 
            // 

            // TODO:
            // Currently, the ARROW keys are ignored (due to the above)
            // Also, keypad handling does not work with NUMLOCK off, since the numpad keys then produce ARROW key events and such.
            // 
            // On the other hand, key repeat works now, and the scrolling speed is excellent!

            // First attempts at a smooth-scroll mode:
            x_pixel_shift = 0;
            if ((ee.Modifiers & Keys.Shift) != 0) {
                if (ee.KeyCode == Keys.Left ||
                    ee.KeyCode == Keys.NumPad4) { // West
                        x_pixel_shift = +32;
                }
                if (ee.KeyCode == Keys.Right ||
                    ee.KeyCode == Keys.NumPad6) { // East
                        x_pixel_shift = -32;
                }
                return;
            } // Handling SHIFT for smooth-scroll left/right


            // Arrow keys and Number pad (with and without NumLock) are set to scroll the viewport 1 tile:
            if (ee.KeyCode == Keys.Down ||
                ee.KeyCode == Keys.NumPad2) { // South
                tvpc.y_origin++;
                tvpc.Invalidate();
            }
            if (ee.KeyCode == Keys.Up ||
                ee.KeyCode == Keys.NumPad8) { // North
                tvpc.y_origin--;
                tvpc.Invalidate();
            }
            if (ee.KeyCode == Keys.Left ||
                ee.KeyCode == Keys.NumPad4) { // West
                tvpc.x_origin--;
                tvpc.Invalidate();
            }
            if (ee.KeyCode == Keys.Right ||
                ee.KeyCode == Keys.NumPad6) { // East
                tvpc.x_origin++;
                tvpc.Invalidate();
            }

            // Diagonal Directions:
            if (ee.KeyCode == Keys.Home ||
                ee.KeyCode == Keys.NumPad7) { // NorthWest
                tvpc.y_origin--;
                tvpc.x_origin--;
                tvpc.Invalidate();
            }
            if (ee.KeyCode == Keys.PageUp ||
                ee.KeyCode == Keys.NumPad9) { // NorthEast
                tvpc.y_origin--;
                tvpc.x_origin++;
                tvpc.Invalidate();
            }
            if (ee.KeyCode == Keys.End ||
                ee.KeyCode == Keys.NumPad1) { // SouthWest
                tvpc.y_origin++;
                tvpc.x_origin--;
                tvpc.Invalidate();
            }
            if (ee.KeyCode == Keys.PageDown ||
                ee.KeyCode == Keys.NumPad3) { // SouthEast
                tvpc.y_origin++;
                tvpc.x_origin++;
                tvpc.Invalidate();
            }

        } // OnKeyDown()

        void Application_Idle(object sender, EventArgs e) {
            double milliseconds = ComputeTimeSlice();
            Accumulate(milliseconds);
            tvpc.Invalidate();  // Causes TVPC redraw (seems like we would want once per tvpc.frame update, but see below...)
        }

        private double ComputeTimeSlice() {
            sw.Stop();
            double timeslice = sw.Elapsed.TotalMilliseconds;
            sw.Reset();
            sw.Start();
            return timeslice;
        }

        double    accum_ms    = 0;
        int       idleCounter = 0;  // Counts number of Accumulate() calls since last tick
        int       num_quanta  = 0;

        const int cycle_period_ms   = 6000;  // Number of milliseconds per cycle of animation frames
        const int quanta_per_frame  = 8;     // N:1 speed relation between quanta:frame
        const int frames_LCM_period = 24;    // This should be the Least_Common_Multiple of all distinct (ITileSprite.num_frames) values
        const int quanta_period     = quanta_per_frame * frames_LCM_period;
        const int ms_per_quanta     = cycle_period_ms / quanta_period;  // M / N --> N animation updates per M milliseconds

        // TODO: 
        // - Smooth-scrolling of the viewport, 
        // - Smooth-scrolling of an individual tile moving to an adjacent tile (later)
        // 
        // For now, a prototype of smooth-scroll is hooked up to SHIFT+Left Arrow / SHIFT+Right Arrow

        int prev_frame = 0;
        private void Accumulate(double milliseconds) {
            idleCounter++;
            accum_ms += milliseconds;
            if (accum_ms < ms_per_quanta) { return; }

            // We need multiple animation rates (for the moment, two: quanta and frame)
            // for updating the displayed image of AnimTileSprite instances
            // on different (map or UI) layers of the TVPC:
            num_quanta++;
            tvpc.quanta = num_quanta;
            tvpc.frame = (num_quanta / quanta_per_frame) % frames_LCM_period;

            //tvpc.Invalidate();  // For OpenTK redraw -- strangely, if called here, we seem to not get Idle events unless mouse activity???
            if (prev_frame != tvpc.frame) {
                this.Invalidate();  // Cause GDI+ redraw upon frame update (gets choppy if called too-frequently)
                prev_frame = tvpc.frame;
            }

            /*
            // First attempt at a smooth-scroll mode:
            if (x_pixel_shift != 0) {
                int n_pixels   = 4;  // n_pixels per smooth scroll increment
                int offset     = (x_pixel_shift > 0) ? +n_pixels : -n_pixels;
                int val        = this.tvpc.tile_grid_offset_x_px + offset;
                x_pixel_shift -= offset;

                // ...This code shows smooth-scrolling, but it is not right yet.
                //    The x_origin needs to change in concert with the tile_grid_offset_x_px...

                int min = -this.tvpc.tile_width_px;
                int max = +this.tvpc.tile_width_px;
                this.tvpc.tile_grid_offset_x_px = Utility.clamp(min, max, val);
                // Next redraw should have the tile grid offset by n_pixels in the indicated direction,
                // and again the next tick, until x_pixel_shift is decremented to zero...
            }
            */
            label1.Text = String.Format("AnimRate:  ({0} frames == {1} quanta) / {2} ms\n  Q={3,4}, Frame={4,2}/{5,2} -- This Tick: {6,3} ms, with {7,3} Idle events",
                                        frames_LCM_period, quanta_period, cycle_period_ms,
                                        tvpc.quanta, tvpc.frame, frames_LCM_period, (int) accum_ms, idleCounter);
            idleCounter = 0;
            accum_ms -= ms_per_quanta;
        } // Accumulate()

        private void OnPaint(object sender, PaintEventArgs ee) {
            // Demonstrate the GDI_Draw_Tile() method on top of the main form
            // Not sure how often such a thing will be wanted for UI purposes, 
            // but it is nice to have the capability.

            Graphics         gg  = this.CreateGraphics();
            //StaticTileSprite spr = ui_ts[0, 0];  // 3,3 on the 4x4 marquee sheet
            AnimTileSprite   spr = new AnimTileSprite(ui_ts, 0, 1, 2, 3);
            AnimTileSprite   ani = new AnimTileSprite(ts, ts[0, 14], ts[1, 14], ts[2, 14], ts[3, 14]);
            // Might also get an Image Attributes value, rather than passing null for the last argument...
            int x1 = 10;
            int x2 = 10 + 32 + 10;                 // to the right of the first tile
            int y1 = 512 + 10 + 5 + 13 + 13 + 10;  // below the TVPC
            // Note that these will both be updated at 'frame' speed
            // (No need for "marching ants" via GDI+, and GDI+ rendering 
            // gets choppy when updated at much faster than 'frame' speed)
            ani.GDI_Draw_Tile(gg, x1, y1, null, tvpc.frame);  // Demonstrate an animated tile via GDI+
            spr.GDI_Draw_Tile(gg, x1, y1, null, tvpc.frame);  // Demonstrate transparency via GDI+

        } // Form1.OnPaint()

    } // class

} // namespace
