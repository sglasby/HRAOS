using System;
using System.Windows.Forms;

namespace OpenGLForm
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form main_window = new Form1();
            Application.Run(main_window);
        }

    } // class

} // namespace
