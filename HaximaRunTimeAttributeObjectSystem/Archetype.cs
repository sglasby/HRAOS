using System;
using System.Text;
using System.Collections.Generic;

public class Archetype : IHaximaSerializeable {
    public int ID { get; set; }
    public string autotag { get; set; }
    private string _tag_;
    public string tag {
        get { return _tag_; }
        set {
            if (!Token.is_bare_multi_word(value)) { Error.BadArg("Got invalid tag name '{0}'.", value); }
            // TODO: If subsequent analysis does not demonstrate that the check below catches actual problems, get rid of it?
            if (Token.is_valid_auto_tag(value)) { Error.BadArg("Got potentially-colliding tag name '{0}' in autotag form.", value); }
            _tag_ = value;
        }
    }

    // TODO: one or more fields referring to other archetypes, for type heirarchies/grouping/etc...

    // TODO: is_abstract
    // Some Archetypes do not represent Obj types, but something more Platonic.
    // The is_abstract field allows the Obj constructor to distinguish, and throw.
    //public bool is_abstract { get; set; }
    // May make more sense to have Archetype always return 0, and subclass ArchetypeAbstract (or similar) to return 1...
    // ...or to simply define an Archetype named 'Platonic', and have Obj instances of it to represent such Platonic entities...

    // TODO: Dictionary<string, ISomethingPolicy> policies;  // To hold various "policy" information

    private Dictionary<string, IArchetypeField> fields;

    public void register(int new_ID) {
        this.ID = ObjectRegistrar.Archetypes.register_object_as(this, typeof(Archetype), new_ID);
    }

    public void unregister() {
        ObjectRegistrar.Archetypes.unregister_object(this);
    }

    public Archetype(string _tag_, int _ID_) {
        // A valid tag is non-null, non-empty, and a valid single/multi-word identifier within the embedded script language
        // The ID we are passed is either 0 (meaning we get one assigned), or non-zero, which gets used.
        if (_tag_ == null) { Error.BadArg("Got null name"); }
        if (!Token.is_bare_multi_word(_tag_)) { Error.BadArg("Got invalid tag '{0}'", _tag_); }

        register(_ID_);
        tag = _tag_;
        autotag = String.Format("{0}-{1}", "ARCH", ID);
        ObjectRegistrar.All.register_tag(autotag, ID);
        if (tag != null) {
            ObjectRegistrar.All.register_tag(tag, ID);
        }
        fields = new Dictionary<string, IArchetypeField>();  // Starts with zero fields
    } // Archetype()

    // TODO: Need to define a destructor/finalizer/cleanup method to un-register a defunct Archetype

    public IArchetypeField this[string field_name] {
        get {
            if (field_name == null) { Error.BadArg("Got null field_name"); }
            if (!Token.is_bare_multi_word(field_name)) { Error.BadArg("Got invalid field_name '{0}'", field_name); }

            if (!fields.ContainsKey(field_name)) {
                // This gets called by Obj[field_name].get, so must be harmless
                return null;
            }
            return fields[field_name];
        }
        set {
            if (field_name == null) { Error.BadArg("Got null field_name"); }
            if (!Token.is_bare_multi_word(field_name)) { Error.BadArg("Got invalid field_name '{0}'", field_name); }

            if (fields.ContainsKey(field_name)) {
                // Call to set for an already-present field.
                // TODO: Should be controlled by a policy variable.
                // A likely default would be to warn, and then replace the existing attribute.
                Error.BadArg("Got already-present Archetype field_name '{0}'", field_name);
            }
            fields[field_name] = value;  // Value is an IArchetypeNamedField
        }
    } // this[field_name]

    public void remove_field(string field_name) {
        if (field_name == null) { Error.BadArg("Got null parse_field_name"); }
        if (!Token.is_bare_multi_word(field_name)) { Error.BadArg("Got invalid parse_field_name '{0}'", field_name); }
        if (!fields.ContainsKey(field_name)) {
            // TODO: Should be controlled by a policy variable.
            //       A likely default would be to warn and continue.
            Error.BadArg("Called for not-present parse_field_name '{0}'", field_name);
        }
        fields.Remove(field_name);
    } // remove_field()

    public void add_field(string field_name, FieldType type) {
        ArchetypeField af = new ArchetypeField(field_name, type);
        fields[field_name] = af;
    } // add_field(name, type)

    public void add_field(string field_name, FieldType type, int value) {
        if (!(type.semantic_type == SemanticTypes.INT ||
              type.semantic_type == SemanticTypes.ID)) { Error.Throw("parse_INT method called for semantic type {0}", type.semantic_type); }

        add_field(field_name, type);
        fields[field_name].default_value.iv = value;
    } // add_field(name, type, int)

    public void add_field(string field_name, FieldType type, string value) {
        if (!(type.semantic_type == SemanticTypes.STRING)) { Error.Throw("STRING method called for semantic type {0}", type.semantic_type); }

        add_field(field_name, type);
        fields[field_name].default_value.sv = value;
    } // add_field(name, type, string)

    public void add_field(string field_name, FieldType type, decimal value) {
        if (!(type.semantic_type == SemanticTypes.DECIMAL)) { Error.Throw("DECIMAL method called for semantic type {0}", type.semantic_type); }

        add_field(field_name, type);
        fields[field_name].default_value.dv = value;
    } // add_field(name, type, string)

    public void add_field(string field_name, FieldType type, List<int> values) {
        // This serves LIST_INT and LIST_ID and other semantic types sharing the LIST_INT storage type
        if (!(type.semantic_type == SemanticTypes.LIST_INT ||
              type.semantic_type == SemanticTypes.LIST_ID)) { Error.Throw("parse_LIST_INT method called for semantic type {0}", type.semantic_type); }

        if (type.semantic_type == SemanticTypes.ID) {
            foreach (int id in values) {
                // TODO: fix this
                // if (id is not valid) {Error.Throw("Called with invalid ID {0}", id); }
            }
        }
        add_field(field_name, type);
        fields[field_name].default_value.ilist = values;
    } // add_field(name, type, list_int_values)

    public void add_field(string field_name, FieldType type, List<string> values) {
        // This serves LIST_STRING and other semantic types sharing the LIST_STRING storage type
        if (!(type.semantic_type == SemanticTypes.LIST_STRING)) { Error.Throw("LIST_STRING method called for semantic type {0}", type.semantic_type); }

        add_field(field_name, type);
        fields[field_name].default_value.slist = values;
    } // add_field(name, type, list_string_values)

    public void add_field(string field_name, FieldType type, List<decimal> values) {
        // This serves LIST_DECIMAL and other semantic types sharing the LIST_DECIMAL storage type
        if (!(type.semantic_type == SemanticTypes.LIST_DECIMAL)) { Error.Throw("LIST_DECIMAL method called for semantic type {0}", type.semantic_type); }

        add_field(field_name, type);
        fields[field_name].default_value.dlist = values;
    } // add_field(name, type, list_decimal_values)

    // ...What other methods are useful?
    // TODO: Add such methods:
    // - An iterator to go through all fields.
    //   Hmmm...currently serialize() just does: foreach (string kk in fields.Keys) {...}
    // ...

    public override string ToString() {
        return String.Format("ARCHETYPE {0}, ID={1}", tag, ID);
    }

    public string serialize() {
        StringBuilder sb = new StringBuilder();
        sb.AppendFormat("ARCHETYPE {0}, ID={1}", tag, ID);  // Note: We always serialize with explicit ID
        // TODO: Might be nice to have an option to print empty {} together...
        sb.AppendLine(" {");
        foreach (string kk in fields.Keys) {
            // TODO: May want to control the order these are sorted in...
            int ww_ff = width_widest_field_name();
            int ww_tt = width_widest_type_name();
            string field_format_string = "  {0,-" + ww_ff + "} => {1}\n";
            sb.AppendFormat(field_format_string, kk, fields[kk].serialize(ww_ff, ww_tt));
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    public int width_widest_field_name() {
        int ww = 0;
        foreach (string kk in fields.Keys) {
            if (kk.Length > ww) { ww = kk.Length; }
        }
        //Form1.stdout.print(">>> archetype {0} widest={1}\n", tag, ww);
        return ww;
    }

    public int width_widest_type_name() {
        int ww = 0;
        foreach (string kk in fields.Keys) {
            int length = fields[kk].type.keyword.Length;
            if (length > ww) { ww = length; }
        }
        return ww;
    }

} // class Archetype
