using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Img = System.Drawing.Imaging;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using System.Collections.Generic;

namespace OpenGLForm
{
    public partial class Form1 : Form
    {
        bool loaded;

        public string                ts_filename = "U4.B_enhanced-32x32.png";
        public System.Drawing.Bitmap bm_sheet;
        Stopwatch sw = new Stopwatch(); // available to all event handlers
        TileViewPortControl tvp_control;
        TileViewPort subject;
        SimpleMapV1 map;
        TileSheet ts;
 
        public Form1()
        {
            InitializeComponent();

            tvp_control = new TileViewPortControl(glControl1);
            //tvp_control.BackColor = System.Drawing.SystemColors.ControlDark;
            //tvp_control.Location = new System.Drawing.Point(0, 0);
            //tvp_control.Size = new System.Drawing.Size(729, 764);

            string filename = @"U4.B_enhanced-32x32.png";
            ts = new TileSheet(filename, 16, 16);

            map = new SimpleMapV1(16, 128, ts);
//            map.AddTerrainRegion(map_16x128, 0, 0);

            subject = new TileViewPort(this.tvp_control,
                9, 9,
                //ViewPortScrollingConstraint.EntireMap,
                ViewPortScrollingConstraint.CenterTile,
                //ViewPortScrollingConstraint.EdgeCorner, 
                map, 0, 0);
            subject.set_center(map, 2, 2);
        }

        private int CreateTextureFromBitmap(Bitmap bitmap)
        {
            Img.BitmapData data = bitmap.LockBits(
              new Rectangle(0, 0, bitmap.Width, bitmap.Height),
              Img.ImageLockMode.ReadOnly,
              Img.PixelFormat.Format32bppArgb);

            int tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);
            
            GL.TexImage2D(
              TextureTarget.Texture2D,
              0,
              PixelInternalFormat.Rgba,
              data.Width, data.Height,
              0,
              PixelFormat.Bgra,
              PixelType.UnsignedByte,
              data.Scan0);
            bitmap.UnlockBits(data);

            GL.TexParameter(TextureTarget.Texture2D,
                            TextureParameterName.TextureMinFilter,
                            (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D,
                            TextureParameterName.TextureMagFilter,
                            (int)TextureMagFilter.Nearest);

            return tex;
        }

        private void glControl1_Load(object sender, EventArgs e)
        {

            SetupViewport();

            glControl1.Width  = 512;
            glControl1.Height = 512;


            //TexUtil.InitTexturing();
            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            loaded = true;
            GL.ClearColor(Color.SkyBlue); // Yay! .NET Colors can be used directly!
            tvp_control.LoadTextures();

            Application.Idle += Application_Idle; // press TAB twice after +=
            this.glControl1.KeyUp += new KeyEventHandler(OnKeyPress);

            sw.Start(); // start at application boot
        }


        public void OnKeyPress(object sender, KeyEventArgs ee)
        {
            //MessageBox.Show(ee.KeyCode.ToString(), "Your input");

            bool need_invalidate = true;

            // Orthogonal Directions:
            switch (ee.KeyCode) {

                default:
                    need_invalidate = false;
                    break;
            }

            if (need_invalidate)
            {
                glControl1.Invalidate();
            }
        } // OnKeyPress()


        private object CreateTextureFromFile(string p)
        {
            throw new NotImplementedException();
        }

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
        int    idleCounter = 0;
        private void Accumulate(double milliseconds)
        {
            idleCounter++;
            accumulator += milliseconds;
            if (accumulator > 1000)
            {
                label1.Text  = idleCounter.ToString();
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
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, width, 0, height, -1, 1); // Bottom-left corner pixel has coordinate (0, 0)
            GL.Viewport(0, 0, width, height); // Use all of the glControl painting area
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            if (!loaded)
                return;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            tvp_control.Render();
           

            glControl1.SwapBuffers();
        }




        int x = 0;
        private void glControl1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                x++;
                glControl1.Invalidate();
            }
        }

        private void glControl1_Resize(object sender, EventArgs e)
        {
            SetupViewport();
            glControl1.Invalidate();
        }     
	
    }
}
