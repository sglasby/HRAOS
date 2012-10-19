using System;
using System.Windows.Forms;

public static class Program {

    [STAThread]
    public static void Main() {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Form main_window = new Form1();
        Application.Run(main_window);
    }

} // class
