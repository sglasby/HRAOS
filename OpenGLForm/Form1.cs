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
using OpenGL_Control_experiment;

namespace OpenGLForm
{
    public partial class Form1 : Form
    {
        bool loaded;

        public string                ts_filename = "U4.B_enhanced-32x32.png";
        public System.Drawing.Bitmap bm_sheet;
        Stopwatch sw = new Stopwatch(); // available to all event handlers
        Subject subject;

        public Form1()
        {
            InitializeComponent();
            bm_sheet = new Bitmap(ts_filename);
            subject = new Subject();
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
            subject.LoadTextures();

            Application.Idle += Application_Idle; // press TAB twice after +=
            this.glControl1.KeyUp += new KeyEventHandler(OnKeyPress);

            sw.Start(); // start at application boot
        }


        public void OnKeyPress(object sender, KeyEventArgs ee)
        {
            //MessageBox.Show(ee.KeyCode.ToString(), "Your input");

            // Orthogonal Directions:
            switch (ee.KeyCode) {
                case Keys.Left:
                case Keys.NumPad4:
                    subject.angle -= 15.0f;
                    glControl1.Invalidate();
                    break;

                case Keys.Right:
                case Keys.NumPad6:
                    subject.angle += 15.0f;
                    glControl1.Invalidate();
                    break;

                case Keys.Q:
                    subject.tiling_mode = TilingModes.Square;
                    break;

                case Keys.N:
                case Keys.S:
                    subject.tiling_mode = TilingModes.Hex_NS;
                    break;

                case Keys.E:
                case Keys.W:
                    subject.tiling_mode = TilingModes.Hex_WE;
                    break;
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
            subject.Render();
           

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
