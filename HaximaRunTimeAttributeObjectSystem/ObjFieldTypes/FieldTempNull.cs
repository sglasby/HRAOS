using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class FieldTempNull : IObjField {
    public  FieldType type       { get { return FieldType.EMPTY; } }
    private Obj       parent_obj { get; set; }
    private string    field_name { get; set; }

    public FieldTempNull(Obj parent_arg, string field_arg) {
        // A FieldTempNull is a placeholder object which exists so that the operation 
        // of setting a new field value without manually calling add_field() can be accomplished.
        // 
        // This works by putting a FieldTempNull in someObj[someFieldName] 
        // when the .get on that is called for a not-present key,
        // then _replacing_ the FieldTempNull with a newly-create IObjField 
        // of one of the "real" types when the .set is called on the FieldTempNull.
        // 
        // As such, it needs to know the parent Obj and the field name by which it is accessed, unlike other IObjField types.
        // 
        // The surprising thing is, how well this scheme seemed to work upon the first proper trial.
        // 
        // TODO: Figure out whether there is some problem with this eccentric-but-seemingly-functional scheme...
        // This seems to approximately match  the "lazy initialization" pattern, per http://en.wikipedia.org/wiki/Lazy_initialization
        // Is also seems partially similar to the "null object"         pattern, per http://en.wikipedia.org/wiki/Null_Object_pattern
        // It also seems somewhat like the        "state pattern"       pattern, per http://en.wikipedia.org/wiki/State_pattern
        // 
        parent_obj = parent_arg;
        field_name = field_arg;
    }

    public int iv {
        get { return 0; }
        set {
            parent_obj[field_name] = new FieldInt(value);
            // Since the only reference to 'this' has just been severed, 
            // 'this' should be eligible for garbage collection after we exit this method.
        }
    } // iv()

    public string sv {
        get { return ""; }
        set {
            parent_obj[field_name] = new FieldString(value);
        }
    }

    public decimal dv {
        get { return 0M; }
        set {
            parent_obj[field_name] = new FieldDecimal(value);
        }
    }

    public IList<int> ilist {
        get { return null; }  // Fewer means of mis-use than returning a new empty list
        set {
            FieldListInt new_field = new FieldListInt();
            parent_obj[field_name] = new_field;
            new_field.val = (List<int>) value;
        }
    }

    public IList<string> slist {
        get { return null; }
        set {
            FieldListString new_field = new FieldListString();
            parent_obj[field_name] = new_field;
            new_field.val = (List<string>) value;
        }
    }

    public IList<decimal> dlist {
        get { return null; }
        set {
            FieldListDecimal new_field = new FieldListDecimal();
            parent_obj[field_name] = new_field;
            new_field.val = (List<decimal>) value;
        }
    }

    public override string ToString() {
        return "";
    }

    public bool is_default_valued() {
        return true;
    }

} // class FieldTempNull
