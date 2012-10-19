using System;
using System.Text;
using System.Collections.Generic;

public class FieldInt : IObjField {
    public virtual FieldType type { get { return FieldType.INT; } }
    protected int val;

    public FieldInt() { }  // Zero-arg constructor for the benefit of subclasses

    public FieldInt(int _val_) {
        val = _val_;
    } // FieldInt(int)

    public int iv {
        get { return val; }
        set { val = value; }
    } // iv()

    public string sv {
        get { Error.Throw("Called for {0}", type.keyword); return null; }
        set { Error.Throw("Called for {0}", type.keyword); }
    }

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
        return String.Format("{0}", val);
    }

    public bool is_default_valued() {
        if (val == 0) { return true; }
        return false;
    }

} // class FieldInt
