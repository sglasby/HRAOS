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
            this.label1.Size      = new System.Drawing.Size(80, 13);
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

            int[] path_rect_5x4 = new int[]
            { // 5 = grass, 7 = trees, 58 = boulder
               58,  5,  5,  7,  5,
                5,  5,  5,  7,  7,
                7,  7,  7,  7,  0,
                5,  5,  7,  5,  5,
            };

            DenseGrid map_16x64 = new DenseGrid(16, 64, 77);
            DenseGrid flip_none = new DenseGrid(5, 4, path_rect_5x4);  // Test with width != height, the better to see the rotations and flips
            DenseGrid flip_we   = flip_none.Flip_WE();
            DenseGrid flip_ns   = flip_none.Flip_NS();
            DenseGrid flip_wens = flip_we.Flip_NS();

            DenseGrid.BlitFromAOntoB(flip_none, map_16x64, 1, 1);
            DenseGrid.BlitFromAOntoB(flip_we,   map_16x64, 7, 1);
            DenseGrid.BlitFromAOntoB(flip_ns,   map_16x64, 1, 7);
            DenseGrid.BlitFromAOntoB(flip_wens, map_16x64, 7, 7);

            map = new SimpleMapV1(16, 64, ts);
            map.AddTerrainRegion(map_16x64, 0, 0);

            tvp = new TileViewPort(this.tvp_control,
                15, 15,
                //ViewPortScrollingConstraint.EntireMap,
                ScrollConstraint.CenterTile,
                //ViewPortScrollingConstraint.EdgeCorner, 
                map, 0, 0);
            tvp.set_center(map, 2, 2);

        } // OnShown()

        private void glControl1_Load(object sender, EventArgs e)
        {
            SetupViewport();

            //GL.Disable(EnableCap.CullFace);  // optional for our purposes
            //GL.Enable(EnableCap.Blend);      // optional for our purposes
            //GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);  // optional for our purposes

            GL.Enable(EnableCap.Texture2D);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);  // TVP works without this, check docs for details of purpose...

            loaded = true;

            Application.Idle      += Application_Idle;
            this.glControl1.KeyUp += OnKeyPress;  // Might want to attach keyhandling method to the form, rather than the control...

            sw.Start(); // start the Stopwatch, which is used to alter the label1.Text via Accumulate() and Animate()
        } // glControl1_Load()

        public void OnKeyPress(object sender, KeyEventArgs ee)
        {
            //MessageBox.Show(ee.KeyCode.ToString(), "Your input");

            // Orthogonal Directions:
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
            if (ee.KeyCode == Keys.NumPad7)
            { // NorthWest
                tvp.y_origin--;
                tvp.x_origin--;
                tvp.Invalidate();
            }
            if (ee.KeyCode == Keys.NumPad9)
            { // NorthEast
                tvp.y_origin--;
                tvp.x_origin++;
                tvp.Invalidate();
            }
            if (ee.KeyCode == Keys.NumPad1)
            { // SouthWest
                tvp.y_origin++;
                tvp.x_origin--;
                tvp.Invalidate();
            }
            if (ee.KeyCode == Keys.NumPad3)
            { // SouthEast
                tvp.y_origin++;
                tvp.x_origin++;
                tvp.Invalidate();
            }
        } // OnKeyPress()

        void Application_Idle(object sender, EventArgs e)
        {
            double milliseconds = ComputeTimeSlice();
            Accumulate(milliseconds);
            Animate(milliseconds);
        }

        float rotation = 0;
        private void Animate(double milliseconds)
        {
            float deltaRotation = (float) milliseconds / 20.0f;
            rotation += deltaRotation;
            glControl1.Invalidate();
        }

        double accumulator = 0;
        int    idleCounter = 0;  // Number of milliseconds between each Paint event?
        private void Accumulate(double milliseconds)
        {
            idleCounter++;
            accumulator += milliseconds;
            if (accumulator > 1000)
            {
                label1.Text  = idleCounter.ToString();  // Change text to show that we are handling Paint events
                accumulator -= 1000;
                idleCounter  = 0; // don't forget to reset the counter!
            }
        }

        private double ComputeTimeSlice()
        {
            sw.Stop();
            double timeslice = sw.Elapsed.TotalMilliseconds;
            sw.Reset();
            sw.Start();
            return timeslice;
        }

        private void SetupViewport()
        {
            int width  = glControl1.Width;
            int height = glControl1.Height;
            GL.MatrixMode(MatrixMode.Projection);  // Why MatrixMode.Projection here, and .Modelview in glControl1_Paint() ?
            GL.LoadIdentity();
            GL.Ortho(0, width, 0, height, -1, 1); // Bottom-left corner pixel has coordinate (0, 0)
            GL.Viewport(0, 0, width, height); // Use all of the glControl painting area
        } // SetupViewport()

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            if (!loaded)
                return;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.MatrixMode(MatrixMode.Modelview);    // Why MatrixMode.Modelview here, and .Projection in SetupViewport() ?
            GL.LoadIdentity();
            tvp_control.Render();

            glControl1.SwapBuffers();
        } // glControl1_Paint()

        private void glControl1_Resize(object sender, EventArgs e)
        {
            SetupViewport();
            glControl1.Invalidate();
        }

        private void OnPaint(object sender, PaintEventArgs ee) {
            // Demonstrate the GDI_Draw_Tile() method on top of the main form
            // Not sure how often such a thing will be wanted for UI purposes, 
            // but it is nice to have the capability.

            Graphics   gg  = this.CreateGraphics();
            TileSprite spr = ts[8, 1];  // Balloon
            // Might also get an Image Attributes value, rather than passing null for the last argument...
            spr.GDI_Draw_Tile(gg, 10, 512 + 10 + 10 + 13 + 10, null);

        } // Form1.OnPaint()

    } // class

} // namespace
