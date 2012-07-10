namespace OpenGLForm
{
    // TODO: 
    // Move all code from this file to Form1.cs, remove the designer file.
    // _Perhaps_ keep the bits in the TileViewPortControl class which 
    // make it possible to use in the Visual Studio GUI Designer tool,
    // but I have no plans to use the Designer in other than a 
    // "fiddle around in a test project, write down coordinates, throw away test project" fashion.
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.glControl1 = new OpenTK.GLControl();
            this.label1     = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // glControl1
            // 
            this.glControl1.BackColor = System.Drawing.Color.Black;
            this.glControl1.Location  = new System.Drawing.Point(10, 10);
            this.glControl1.Name      = "glControl1";
            this.glControl1.Size      = new System.Drawing.Size(512, 512);  // These values are over-written in Form1.cs anyways
            this.glControl1.TabIndex  = 0;
            this.glControl1.Load     += new System.EventHandler(this.glControl1_Load);
            this.glControl1.Paint    += new System.Windows.Forms.PaintEventHandler(this.glControl1_Paint);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 596);
            this.label1.Name     = "label1";
            this.label1.Size     = new System.Drawing.Size(71, 13);
            this.label1.TabIndex = 1;
            this.label1.Text     = "This is a label";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize          = new System.Drawing.Size(719, 627);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.glControl1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private OpenTK.GLControl           glControl1;
        private System.Windows.Forms.Label label1;
    }
}

