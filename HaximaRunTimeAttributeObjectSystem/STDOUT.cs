using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

public class STDOUT {
    public RichTextBox stdout;  // not-public, once needed methods are added...

    public STDOUT(RichTextBox tt) {
        stdout = tt;
    }

    public void print(string format, params object[] args) {
        string ss = string.Format(format, args);
        stdout.AppendText(ss);
    } // print()

} // class STDOUT
