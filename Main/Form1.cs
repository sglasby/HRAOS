using System;
using System.Diagnostics;  // for Stopwatch
using System.Drawing;
using System.Windows.Forms;

using OpenTK;
using OpenTK.Graphics.OpenGL;


namespace OpenGLForm
{
    // TODO: A better name for this Form would be a good thing...
    public partial class Form1 : Form
    {
        bool      loaded;
        Stopwatch sw = new Stopwatch(); // available to all event handlers

        private GLControl   glControl1;  // TODO: This goes away once TileViewPortControl _isa_ GLControl...
        TileViewPortControl tvp_control;
        TileViewPort        tvp;
        SimpleMapV1         map;
        TileSheet           ts;
        TileSheet           ui_ts;
        TileSheet           f1234;
        TileSheet           whirlpool_ts;

        private Label label1;
 
        public Form1() {
            // Set up glControl1, label1, etc before calling this.Controls.Add()

            this.glControl1 = new GLControl();
            this.glControl1.BackColor = System.Drawing.Color.Black;
            this.glControl1.Location  = new System.Drawing.Point(10, 10);
            this.glControl1.Size      = new System.Drawing.Size(512, 512);
            this.glControl1.TabIndex  = 0;
            //this.glControl1.Load     += new System.EventHandler(this.glControl1_Load);
            this.glControl1.Paint    += new System.Windows.Forms.PaintEventHandler(this.glControl1_Paint);

            tvp_control = new TileViewPortControl(glControl1);
            //tvp_control.BackColor = System.Drawing.SystemColors.ControlDark;
            //tvp_control.Location  = new System.Drawing.Point(0, 0);
            //tvp_control.Size      = new System.Drawing.Size(729, 764);

            this.label1 = new Label();
            this.label1.BackColor = SystemColors.ControlDark;
            this.label1.Location  = new System.Drawing.Point(12, 512 + 10 + 10);
            this.label1.Size      = new System.Drawing.Size(600, 13);
            this.label1.TabIndex  = 1;
            // label1.Text Is set later in Accumulate()

            //this.Size = new Size(800, 600);  // Perhaps more convenient to set ClientSize 
            this.ClientSize = new System.Drawing.Size(800, 600);  // Results in .Size of (808 x 634) with titlebar, window borders, etc
            this.Controls.Add(this.label1);
            this.Controls.Add(this.glControl1);
            this.Text   = "Main Window Title";
            this.Load  += this.glControl1_Load;
            this.Shown += this.OnShown;  // Will execute once, AFTER Load and Activate events
            // Note: The order of (Load, Activated, Shown) can be altered, if a MessageBox() is invoked, therefore: do not use MessageBox()
            this.Paint += this.OnPaint;  // Using this to test TileSprite.GDI_Draw_Tile()

            // TODO: Figure out whether we want key-handling attached to the Form or to a Control in the form 
            // (currently it is attached to glControl1, via glControl1_Load() setting this.glControl1.KeyUp += OnKeyPress)

            Application.Idle += Application_Idle;
            sw.Start(); // start the Stopwatch, which is used to alter the label1.Text via Accumulate() and Animate()
            this.glControl1.KeyDown += new KeyEventHandler(OnKeyDown);  // Handling KeyDown lets us get key repeat.
            // TODO: See notes in OnKeyDown()

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

            // TODO:
            // Code from the pre-merge TileViewPort demo which loads the "ui_ts" tilesheet 
            // (marquee tiles for the ViewPortLayers tiles) should be added back in...
            // Pre-requisite to that is refactoring the TileSprite class, and for the drawing code to use OpenGL texture IDs from TileSprite objects...
            string filename = @"U4.B_enhanced-32x32.png";
            ts = new TileSheet(filename, 16, 16);  // causes GL textures to be loaded, needs some GL setup prior...

            string f1234_filename = @"example_all_facings.4_frames.intra_1.png";
            f1234 = new TileSheet(f1234_filename, 4, 9, 32, 32, 1, 1, 1, 1);

            string ui_ts_filename = @"bright_marquee.frame_1.png";  // Sprite ID 272 is the reticle
            ui_ts = new TileSheet(ui_ts_filename, 4, 4);

            AnimTileSprite anim_blue_wiz = new AnimTileSprite(ts, ts[32], ts[33]);
            AnimTileSprite anim_red_wiz  = new AnimTileSprite(ts, ts[0,14], ts[1,14], ts[2,14], ts[3,14]);
            AnimTileSprite count_1234    = new AnimTileSprite(f1234, f1234[0,0], f1234[1,0], f1234[2,0], f1234[3,0]);

            int[] path_rect_5x4 = new int[]
            { // 5 = grass, 7 = trees, 58 = boulder
               58,  5,  5,  7,  5,
                5,  5,  5,  7,  7,
                7,  7,  7,  7,  0,
                5,  5,  7,  5,  5,
            };

            int lava_ID = ts[12,4].ID;  // Lava
            //DenseGrid map_16x64 = new DenseGrid(16, 64, lava_ID);
            DenseGrid map_16x64 = new DenseGrid(16, 64, count_1234.ID);

            DenseGrid flip_none = new DenseGrid(5, 4, path_rect_5x4);  // Test with width != height, the better to see the rotations and flips
            DenseGrid flip_we   = flip_none.Flip_WE();
            DenseGrid flip_ns   = flip_none.Flip_NS();
            DenseGrid flip_wens = flip_we.Flip_NS();

            DenseGrid.BlitFromAOntoB(flip_none, map_16x64, 1, 1);
            DenseGrid.BlitFromAOntoB(flip_we,   map_16x64, 7, 1);
            DenseGrid.BlitFromAOntoB(flip_ns,   map_16x64, 1, 7);
            DenseGrid.BlitFromAOntoB(flip_wens, map_16x64, 7, 7);

            DenseGrid flip_none_rot090 = flip_none.Rotate090();
            DenseGrid flip_we_rot090   = flip_we  .Rotate090();
            DenseGrid flip_ns_rot090   = flip_ns  .Rotate090();
            DenseGrid flip_wens_rot090 = flip_wens.Rotate090();

            DenseGrid.BlitFromAOntoB(flip_none_rot090, map_16x64, 1, 52);
            DenseGrid.BlitFromAOntoB(flip_we_rot090,   map_16x64, 7, 52);
            DenseGrid.BlitFromAOntoB(flip_ns_rot090,   map_16x64, 1, 58);
            DenseGrid.BlitFromAOntoB(flip_wens_rot090, map_16x64, 7, 58);

            map = new SimpleMapV1(16, 64, ts);
            map.AddTerrainRegion(map_16x64, 0, 0);

            tvp = new TileViewPort(this.tvp_control,
                15, 15,
                //ViewPortScrollingConstraint.EntireMap,
                ScrollConstraint.CenterTile,
                //ViewPortScrollingConstraint.EdgeCorner, 
                map, 0, 0);
            tvp.set_center(map, 2, 2);

            // Add some elements to the Beings layer of the Map:  // TODO: Still using hard-coded Sprite ID values here...
//          map.layers[MapLayers.Beings].set_contents_at_XY(2, 1, 33);  // Wizard
            map.layers[MapLayers.Beings].set_contents_at_XY(8, 7, 21);  // Horse

            map.layers[MapLayers.Beings].set_contents_at_XY(8, 7, 21);  // Horse
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


            // Add some elements to the UI_elements layer of the TileViewPort:
            
            int reticle = ui_ts[3, 3].ID;  // avoiding hard-coding Sprite ID 272
            tvp.layers[ViewPortLayers.UI_Elements].set_contents_at_XY(tvp.center_x(),      tvp.center_y(),       reticle);  // Center
            tvp.layers[ViewPortLayers.UI_Elements].set_contents_at_XY(0,                   0,                    reticle);  // NW
            tvp.layers[ViewPortLayers.UI_Elements].set_contents_at_XY(tvp.width_tiles - 1, 0,                    reticle);  // NE
            tvp.layers[ViewPortLayers.UI_Elements].set_contents_at_XY(0,                   tvp.height_tiles - 1, reticle);  // SW
            tvp.layers[ViewPortLayers.UI_Elements].set_contents_at_XY(tvp.width_tiles - 1, tvp.height_tiles - 1, reticle);  // SE

        } // OnShown()

        private void glControl1_Load(object sender, EventArgs e)
        {
            SetupViewport();
            //GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.Texture2D);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            // In theory, these (plus sheet.MakeTransparent(Color.Magenta) in TileSheet.cs) should enable transparency...
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

            loaded = true;
        } // glControl1_Load()

        private void SetupViewport() {
            // This exists here, in part because there are two callers:  glControl1_Load() and glControl1_Resize()
            // TODO: This code may belong in TileViewPortControl (when it isa GLControl)
            int width  = glControl1.Width;
            int height = glControl1.Height;
            GL.MatrixMode(MatrixMode.Projection);  // Why MatrixMode.Projection here, and .Modelview in glControl1_Paint() ?
            GL.LoadIdentity();
            GL.Ortho(0, width, height, 0, 0, 1); // OpenGL origin coordinate (0,0) is at bottom left; we want origin at top left
            GL.Viewport(0, 0, width, height);    // Use all of the GLControl area for the GL Viewport
        } // SetupViewport()

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
            //       an event handler for PreviewKeyDown on the form, 
            //       setting e.IsInputKey = true for the wanted keys. 
            //       http://msdn.microsoft.com/en-us/library/system.windows.forms.control.previewkeydown.aspx
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
            // Also, keypad handling does not work with NUMLOCK off, since the nupad keys then produce ARROW key events and such.
            // 
            // On the other hand, key repeat works now, and the scrolling speed is excellent!

            if (ee.KeyCode == Keys.Down ||
                ee.KeyCode == Keys.NumPad2)
            { // South
                tvp.y_origin++;
                tvp.Invalidate();
            }
            if (ee.KeyCode == Keys.Up ||
                ee.KeyCode == Keys.NumPad8)
            { // North
                tvp.y_origin--;
                tvp.Invalidate();
            }
            if (ee.KeyCode == Keys.Left ||
                ee.KeyCode == Keys.NumPad4)
            { // West
                tvp.x_origin--;
                tvp.Invalidate();
            }
            if (ee.KeyCode == Keys.Right ||
                ee.KeyCode == Keys.NumPad6)
            { // East
                tvp.x_origin++;
                tvp.Invalidate();
            }

            // Diagonal Directions:
            if (ee.KeyCode == Keys.Home ||
                ee.KeyCode == Keys.NumPad7)
            { // NorthWest
                tvp.y_origin--;
                tvp.x_origin--;
                tvp.Invalidate();
            }
            if (ee.KeyCode == Keys.PageUp ||
                ee.KeyCode == Keys.NumPad9)
            { // NorthEast
                tvp.y_origin--;
                tvp.x_origin++;
                tvp.Invalidate();
            }
            if (ee.KeyCode == Keys.End ||
                ee.KeyCode == Keys.NumPad1)
            { // SouthWest
                tvp.y_origin++;
                tvp.x_origin--;
                tvp.Invalidate();
            }
            if (ee.KeyCode == Keys.PageDown ||
                ee.KeyCode == Keys.NumPad3)
            { // SouthEast
                tvp.y_origin++;
                tvp.x_origin++;
                tvp.Invalidate();
            }

        } // OnKeyDown()

        void Application_Idle(object sender, EventArgs e)
        {
            double milliseconds = ComputeTimeSlice();
            Accumulate(milliseconds);
            glControl1.Invalidate();
        }

        int       frame       = 0;  // Current animation frame.  This concept may end up per-TileViewPort, or even per-ITileSprite...need to experiment...
        double    accum_ms    = 0;
        int       idleCounter = 0;  // Counts number of Accumulate() calls since last tick
        int       num_ticks   = 0;

        const int cycle_period = 3000;  // Number of milliseconds per cycle of animation frames
        const int periodicity  = 4;
        const int tick_time    = cycle_period / periodicity;  // N animation updates per 1000 milliseconds

        private void Accumulate(double milliseconds)
        {
            idleCounter++;
            accum_ms += milliseconds;
            if (accum_ms >= tick_time)
            {
                num_ticks++;
                frame = num_ticks % periodicity;
                label1.Text = String.Format("AnimRate=({0} frames / {1} ms) -- Tick={2,4}, Frame={3,2} -- This Tick: {4,3} ms, with {5,3} Idle events", 
                                            periodicity, cycle_period, num_ticks, frame, (int)accum_ms, idleCounter);
                idleCounter = 0;
                accum_ms   -= tick_time;
            }
        } // Accumulate()

        private double ComputeTimeSlice()
        {
            sw.Stop();
            double timeslice = sw.Elapsed.TotalMilliseconds;
            sw.Reset();
            sw.Start();
            return timeslice;
        }


        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            if (!loaded)
                return;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.MatrixMode(MatrixMode.Modelview);    // Why MatrixMode.Modelview here, and .Projection in SetupViewport() ?
            GL.LoadIdentity();
            tvp_control.Render(frame);

            GL.Flush();
            glControl1.SwapBuffers();
        } // glControl1_Paint()

        private void glControl1_Resize(object sender, EventArgs e)
        {
            // Hmmm...when exactly does the GLControl get resized?
            SetupViewport();
            glControl1.Invalidate();
        }

        private void OnPaint(object sender, PaintEventArgs ee) {
            // Demonstrate the GDI_Draw_Tile() method on top of the main form
            // Not sure how often such a thing will be wanted for UI purposes, 
            // but it is nice to have the capability.

            Graphics   gg  = this.CreateGraphics();
            StaticTileSprite spr = ts[8, 1];  // Balloon
            // Might also get an Image Attributes value, rather than passing null for the last argument...
            spr.GDI_Draw_Tile(gg, 10, 512 + 10 + 10 + 13 + 10, null);

        } // Form1.OnPaint()

    } // class

} // namespace
