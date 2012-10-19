using System;
using System.Text;
using System.Collections.Generic;

public class FieldListDecimal : IObjField {
    public FieldType type { get { return FieldType.LIST_DECIMAL; } }
    internal List<decimal> val;

    public FieldListDecimal() {
        val = new List<decimal>();
    }  // FieldListDecimal()

    public FieldListDecimal(params decimal[] values) {
        val = new List<decimal>(values);
    } // FieldListDecimal(params decimal[])

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
        get { Error.Throw("Called for {0}", type.keyword); return null; }
        set { Error.Throw("Called for {0}", type.keyword); }
    }

    public IList<decimal> dlist {
        get { return val; }
        set { val = (List<decimal>) value; }
    } // dlist()

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        sb.Append("[");
        string sep = "";  // We _pre_pend a separator before all but the _first_ element
        foreach (decimal dd in val) {
            sb.AppendFormat("{0}{1}M", sep, dd);
            sep = ", ";
        }
        sb.Append("]");
        return sb.ToString();
    }

    public bool is_default_valued() {
        if ((val == null) || (val.Count == 0)) { return true; }
        return false;
    }

} // class FieldListDecimal
