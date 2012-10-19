using System;
using System.Text;
using System.Collections.Generic;

    // Much to my surprise, it turns out that the fact that 
    // the base class implements IFoo does NOT mean that this fact is inherited!
    // 
    // In the absense of ', IObjField' this manifests as a bug where
    //     someArchetype[some_field].default_value.type == ID
    //     but                  someObj[someField].type == INT
public class FieldID : FieldInt, IObjField {
    public new FieldType type { get { return FieldType.ID; } }

    public FieldID() {
        //Form1.stdout.print("FieldID() my GetType()={0}, type={1}\n", GetType(), type);
        val = 0;
    } // FieldID()

    public FieldID(int _val_) {
        // BUG: (fixed by adding ", IObjField" to the inheritance list...
        // 
        // Somehow, after manually adding an ID field to an Archetype, 
        // the auto-vivification of the corresponding Obj field
        //     results in Obj.archetype[that_field].type == ID,
        //     while                Obj[that_field].type == INT
        // 
        // Methinks something not-wanted is happening with the constructor, since FieldID inherits from FieldInt.
        // The brute force solution would seem to be to make this inheritance not so,
        // but that seems a shame as
        // 1 - A bit more typing for each new Semantic type implemented, as they need a not-inherited class, if so
        // 2 - Surely this should work in the first place, as FieldInt.type and FieldID.type use virtual and override, and so forth...

        //Form1.stdout.print("FieldID(val) my type={0}, val={1}\n", type, _val_);

        // Object_Registrar.All.is_valid_or_zero_ID(ID_value)  // standardize on this method for a valid-ID check?

        //IHaximaSerializeable obj = Object_Registrar.All.object_for_ID(_val_);  // or better to give a more precise error thusly?
        //if (obj == null) { Error.BadArg("Called with invalid object ID {0}", _val_); }
        iv = _val_;  // Should trigger the validation logic in the .set()

        //val = _val_;
    } // FieldID(int)

    public new int iv {
        get { return val; }
        set {
            if (value == 0) { val = value; return; }
            IHaximaSerializeable obj = ObjectRegistrar.All.object_for_ID(value);
            if (obj == null) { Error.BadArg("Called with invalid object ID {0}", value); }
            val = value;
        }
    } // iv()

    public override string ToString() {
        // TODO: 
        // Policy control over how ID values are serialized,
        // defaulting to preferring (tag, auto_tag, integer ID value) in order.
        IHaximaSerializeable obj = ObjectRegistrar.All.object_for_ID(val);
        string tag_value;
        if      (val == 0 || obj == null) { tag_value = val.ToString(); }
        else if (obj.tag != null)         { tag_value = obj.tag;        }
        else if (obj.autotag != null)     { tag_value = obj.autotag;    }
        else                              { tag_value = val.ToString(); }
        return String.Format("{0}", tag_value);
    }

} // class FieldID
