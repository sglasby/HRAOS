using System;
using System.Text;
using System.Collections.Generic;

public class Obj : IHaximaSerializeable {
    public int ID         { get; set; }
    public string autotag { get; set; }
    public string tag     { get; set; }
    // TODO: Implicit tag composed from sigil+prefix+ID, as well as zero or more manually-set custom tags
    // -- The above may represent an earlier, outmoded notion.  autotag + zero-or-one manually-set tag, seems sufficient?
    public Archetype archetype { get; private set; }

    private Dictionary<string, IObjField> fields;

    public Obj(Archetype _arch_, string _tag_, int _ID_) {
        // Null archetype is uncommon, but valid -- verify this is still true, is there a reasonable use for an Obj with null archetype?
        // Null tag is common, and valid (tag.get returns implicit composed tag...)
        register(_ID_);
        archetype = _arch_;
        tag       = _tag_;
        autotag   = String.Format("{0}-{1}", "OBJ", ID);  // Likely move this into the autotag.get block...performance/memory to do it on demand versus here???

        ObjectRegistrar.All.register_tag(autotag, ID);
        if (tag != null) {
            ObjectRegistrar.All.register_tag(tag, ID);
        }

        fields = new Dictionary<string, IObjField>();  // Starts with zero fields
    } // Obj(archetype, tag)

    // TODO: Need to define a destructor/finalizer/cleanup method to un-register a defunct Obj

    public void register(int new_ID) {
        this.ID = ObjectRegistrar.Objs.register_object_as(this, typeof(Obj), new_ID);
    }

    public void unregister() {
        ObjectRegistrar.Objs.unregister_object(this);
    }

    public IObjField this[string field_name] {
        // TODO: 2010/04/08 
        // Determined that get and set BOTH need to ALWAYS auto-vivify the field.
        // This is because someobj["some_field"].iv = 42 
        // calls OBJ indexer property GET, and then FIELD property SET on the field that returns.
        // 
        // In future, maybe some means of avoiding "needless" auto-vivification on get can be found,
        // but it may be a non-critical optimization anyways -- that needs study to justify optimization effort
        get {
            if (field_name == null) { Error.BadArg("Got null field_name"); }
            if (!Token.is_bare_multi_word(field_name)) { Error.BadArg("Got invalid field_name '{0}'", field_name); }

            if (!fields.ContainsKey(field_name)) {
                // If the field is not present, auto-vivify the field from the Archetype's corresponding field
                IArchetypeField arch_field = archetype[field_name];
                if (arch_field == null) {
                    // TODO: Policy control over behavior upon .get of a field not present in the Archetype.
                    // Default will likely be to auto-vivify and return default value?
                    //Form1.stdout.print("ObjField.get: found null archetype field for field_name '{0}', auto-vivifying.\n", field_name);
                    fields[field_name] = new FieldTempNull(this, field_name);
                    // Let's see if the FieldTempNull approach works...wow, surprisingly well at near-first test!

                    // Hmmm...if we don't have an Archetype field to get an IObjField from, how do we know what type to instantiate?
                    // Seems it will have to be fatal?  I'd rather be able to auto-vivify, but this gets called _before_ the .set ...argh...
                    //Error.Throw("Somehow got null archetype arch_field for field_name '{0}'", field_name);
                }
                else {
                    fields[field_name] = arch_field.default_value;  // TODO: Verify that this is a "deep" copy, or deep enough at any rate...
                }
            }
            return fields[field_name];
        }
        set {
            if (field_name == null) { Error.BadArg("Got null field_name"); }
            if (!Token.is_bare_multi_word(field_name)) { Error.BadArg("Got invalid field_name '{0}'", field_name); }

            if (archetype[field_name] != null) {
                SemanticTypes type_of_new_field  = value.type.semantic_type;
                SemanticTypes type_of_arch_field = archetype[field_name].type.semantic_type;
                if (type_of_new_field != type_of_arch_field) {
                    Error.BadArg("ObjField.set: Called for Obj '{0}' with Archetype '{1}' on field_name '{2}', " +
                                 "with strange IObjField having semantic type '{3}' " +
                                 "which differs from the archetype field semantic type '{4}'",
                                 autotag, archetype.tag, field_name, type_of_new_field, type_of_arch_field);
                }
            }
            //else
            //{
            //    Form1.stdout.print("Obj.this[].set called for field '{0}', archetype does not contain that field\n", field_name);
            //}

            if (fields.ContainsKey(field_name) && fields[field_name].type != FieldType.EMPTY) {
                // Call to set for an already-present field.
                // This is OK if the already-present field is a FieldTempNull... ???

                // TODO: Should be controlled by a policy variable.
                // A likely default would be to warn and replace the existing attribute.
                Error.BadArg("Got already-present Obj parse_field_name '{0}'", field_name);
            }
            fields[field_name] = value;
        }
    } // this[field_name]

    public void add_field(string field_name, FieldType type) {
        IObjField field = null;  // or perhaps: new FieldTempNull(this, field_name);
        switch (type.semantic_type) {
            case SemanticTypes.INT:
                field = new FieldInt();
                break;
            case SemanticTypes.STRING:
                field = new FieldString();
                break;
            case SemanticTypes.DECIMAL:
                field = new FieldDecimal();
                break;
            case SemanticTypes.ID:
                field = new FieldID();
                break;

            case SemanticTypes.LIST_INT:
                field = new FieldListInt();
                break;
            case SemanticTypes.LIST_STRING:
                field = new FieldListString();
                break;
            case SemanticTypes.LIST_DECIMAL:
                field = new FieldListDecimal();
                break;
            case SemanticTypes.LIST_ID:
                field = new FieldListID();
                break;

            default:
                Error.BadArg("Got unknown field type '{0}'", type);
                break;
        }
        fields[field_name] = field;
    } // add_field()

    public void remove_field(string field_name) {
        if (field_name == null) { Error.BadArg("Got null field_name"); }
        if (!Token.is_bare_multi_word(field_name)) { Error.BadArg("Got invalid field_name '{0}'", field_name); }
        if (!fields.ContainsKey(field_name)) {
            // TODO: Should be controlled by a policy variable.
            //       A likely default would be to warn and continue.
            Error.BadArg("Called for not-present parse_field_name '{0}'", field_name);
        }
        fields[field_name] = null;
    } // remove_field()


    // ...What other methods are useful?
    // - support an iterator interface to loop through all fields?
    // - serialize() method...

    public string serialize() {
        StringBuilder sb = new StringBuilder();
        if (tag != null) {
            sb.AppendFormat("OBJ (TAG => {0}, ID => {1}, ARCHETYPE => {2})", tag, ID, archetype.tag);
        }
        else {
            // Omit tag if not set
            sb.AppendFormat("OBJ (ID => {0}, ARCHETYPE => {1})", ID, archetype.tag);
        }
        // TODO: Might be nice to have an option to print empty {} together...
        sb.AppendLine(" {");
        foreach (string kk in fields.Keys) {
            // May want to control the order these are sorted in...
            int ww_ff = width_widest_field();
            int ww_tt = width_widest_type();
            // TODO:
            if (archetype[kk] != null) {
                string field_format_string = "  {0,-" + ww_ff + "} => {1}\n";
                sb.AppendFormat(field_format_string, kk, fields[kk].ToString());
            }
            else {
                // This is a field not present in the Archetype, so specify a type field:
                int ww_NIA_types = width_widest_type_not_in_archetype();
                string field_format_string = "  {0,-" + ww_ff + "} => {1,-" + ww_NIA_types + "}, {2}\n";
                sb.AppendFormat(field_format_string, kk, fields[kk].type, fields[kk].ToString());
            }
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    public int width_widest_field() {
        int ww = 0;
        foreach (string kk in fields.Keys) {
            if (kk.Length > ww) { ww = kk.Length; }
        }
        //Form1.stdout.print(">>> archetype {0} widest={1}\n", tag, ww);
        return ww;
    }

    public int width_widest_type() {
        int ww = 0;
        foreach (string kk in fields.Keys) {
            int length = fields[kk].type.keyword.Length;
            if (length > ww) { ww = length; }
        }
        return ww;
    }

    public int width_widest_type_not_in_archetype() {
        int ww = 0;
        foreach (string kk in fields.Keys) {
            if (archetype[kk] != null) { continue; }
            int length = fields[kk].type.keyword.Length;
            if (length > ww) { ww = length; }
        }
        return ww;
    }

} // class Obj
