using System;
using System.Text;
using System.Collections.Generic;

public class FieldString : IObjField {
    public FieldType type { get { return FieldType.STRING; } }
    protected string val;

    public FieldString() { } // Zero-arg constructor for the benefit of subclasses

    public FieldString(string _val_) {
        val = _val_;
    } // FieldString(string)

    public int iv {
        get { Error.Throw("Called for {0}", type.keyword); return 0; }
        set { Error.Throw("Called for {0}", type.keyword); }
    }

    public string sv {
        get { return val; }
        set { val = value; }
    } // sv()

    public decimal dv {
        get { Error.Throw("Called for {0}", type.keyword); return 0; }
        set { Error.Throw("Called for {0}", type.keyword); }
    }

    public IList<int> ilist {
        get { Error.Throw("Called for {0}", type.keyword); return null; }
        set { Error.Throw("Called for {0}", type.keyword); }
    }

    public IList<string> slist {
        get { Error.Throw("Called for {0}", type.keyword); return null; }
        set { Error.Throw("Called for {0}", type.keyword); }
    }

    public IList<decimal> dlist {
        get { Error.Throw("Called for {0}", type.keyword); return null; }
        set { Error.Throw("Called for {0}", type.keyword); }
    }

    public override string ToString() {
        // TODO: Policy control over how strings are quoted, and support for escaping meta-characters (mainly \' \" \\)
        return String.Format("'{0}'", val);
    }

    public bool is_default_valued() {
        if ((val == null) || (val.Length == 0)) { return true; }
        return false;
    }

} // class FieldString
