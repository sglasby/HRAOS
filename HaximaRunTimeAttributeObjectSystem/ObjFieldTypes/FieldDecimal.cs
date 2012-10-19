using System;
using System.Text;
using System.Collections.Generic;

public class FieldDecimal : IObjField {
    public FieldType type { get { return FieldType.DECIMAL; } }
    protected decimal val;

    public FieldDecimal() { }  // Zero-arg constructor for the benefit of subclasses

    public FieldDecimal(decimal _val_) {
        val = _val_;
    } // FieldDecimal(decimal)

    public int iv {
        get { Error.Throw("Called for {0}", type.keyword); return 0; }
        set { Error.Throw("Called for {0}", type.keyword); }
    }

    public string sv {
        get { Error.Throw("Called for {0}", type.keyword); return null; }
        set { Error.Throw("Called for {0}", type.keyword); }
    }

    public decimal dv {
        get { return val; }
        set { val = value; }
    } // dv()

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
        return String.Format("{0}M", val);
    }

    public bool is_default_valued() {
        if (val == 0) { return true; }
        return false;
    }

} // class FieldDecimal
