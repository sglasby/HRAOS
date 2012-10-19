using System;
using System.Text;
using System.Collections.Generic;

public class FieldListString : IObjField {
    public FieldType type { get { return FieldType.LIST_STRING; } }
    internal List<string> val;

    public FieldListString() {
        val = new List<string>();
    } // FieldListString()

    public FieldListString(params string[] values) {
        val = new List<string>(values);
    } // FieldListString(params string[])

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
        get { Error.Throw("Called for {0}", type.keyword); return null; }
        set { Error.Throw("Called for {0}", type.keyword); }
    }

    public IList<string> slist {
        get { return val; }
        set { val = (List<string>) value; }
    } // slist()

    public IList<decimal> dlist {
        get { Error.Throw("Called for {0}", type.keyword); return null; }
        set { Error.Throw("Called for {0}", type.keyword); }
    }

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        sb.Append("[");
        string sep = "";  // We _pre_pend a separator before all but the _first_ element
        foreach (string ss in val) {
            sb.AppendFormat("{0}'{1}'", sep, ss);
            sep = ", ";
        }
        sb.Append("]");
        return sb.ToString();
    }

    public bool is_default_valued() {
        if ((val == null) || (val.Count == 0)) { return true; }
        return false;
    }

} // class FieldListString
