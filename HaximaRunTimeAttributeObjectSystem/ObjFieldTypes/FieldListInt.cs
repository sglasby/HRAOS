using System;
using System.Text;
using System.Collections.Generic;

public class FieldListInt : IObjField {
    public FieldType type { get { return FieldType.LIST_INT; } }
    internal List<int> val;

    public FieldListInt() {
        val = new List<int>();
    }  // FieldListInt()

    public FieldListInt(params int[] values) {
        val = new List<int>(values);
    } // FieldListInt(params int[])

    public int iv {
        get { Error.Throw("Called for {0}", type.keyword); return 0; }
        set { Error.Throw("Called for {0}", type.keyword); }
    }

    public string sv {
        get { Error.Throw("Called for {0}", type.keyword); return null; }
        set { Error.Throw("Called for {0}", type.keyword); }
    }

    public decimal dv {
        get { Error.Throw("Called for {0}", type.keyword); return 0; }
        set { Error.Throw("Called for {0}", type.keyword); }
    }

    public IList<int> ilist {
        get { return val; }
        set { val = (List<int>) value; }
    } // ilist()

    public IList<string> slist {
        get { Error.Throw("Called for {0}", type.keyword); return null; }
        set { Error.Throw("Called for {0}", type.keyword); }
    }

    public IList<decimal> dlist {
        get { Error.Throw("Called for {0}", type.keyword); return null; }
        set { Error.Throw("Called for {0}", type.keyword); }
    }

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        sb.Append("[");
        string sep = "";  // We _pre_pend a separator before all but the _first_ element
        foreach (int ii in val) {
            sb.AppendFormat("{0}{1}", sep, ii);
            sep = ", ";
        }
        sb.Append("]");
        return sb.ToString();
    }

    public bool is_default_valued() {
        if ((val == null) || (val.Count == 0)) { return true; }
        return false;
    }

} // class FieldListInt
