using System;
using System.Text;
using System.Collections.Generic;

    // Note: Same inheritance-of-implemented-interface thing as in FieldID.
public class FieldListID : FieldListInt, IObjField {
    public new FieldType type { get { return FieldType.LIST_ID; } }

    public FieldListID() {
        val = new List<int>();
    } // FieldListID()

    public FieldListID(params int[] values) {
        foreach (int id in values) {
            // validate: check each ID in array is valid
            // if (id is not valid) {Error.Throw("Called with invalid ID {0}", id); }
        }
        val = new List<int>(values);
    } // FieldListID(params int[])

    public new IList<int> ilist {
        get { return val; }
        set {
            if (value == null) { Error.Throw("Called with null list"); }
            foreach (int id in value) {
                // validate: check each ID in array is valid
                if (id == 0) { break; }
                IHaximaSerializeable obj = ObjectRegistrar.All.object_for_ID(id);
                if (obj == null) { Error.BadArg("Called with invalid object ID {0}", id); }
            }
            val = (List<int>) value;
        }
    } // ilist()

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        sb.Append("[");
        string sep = "";  // We _pre_pend a separator before all but the _first_ element
        foreach (int ii in val) {
            // TODO: 
            // Policy control over how ID values are serialized,
            // defaulting to preferring (tag, auto_tag, integer ID value) in order.
            IHaximaSerializeable obj = ObjectRegistrar.All.object_for_ID(ii);
            string tag_value;
            if      (ii == 0 || obj == null) { tag_value = ii.ToString();  }
            else if (obj.tag != null)        { tag_value = obj.tag;        }
            else if (obj.autotag != null)    { tag_value = obj.autotag;    }
            else                             { tag_value = val.ToString(); }

            sb.AppendFormat("{0}{1}", sep, tag_value);
            sep = ", ";
        }
        sb.Append("]");
        return sb.ToString();
    }

} // class FieldListID
