using System;
using System.Text;
using System.Collections.Generic;


public interface IArchetypeField {
    string    name          { get; set; }
    FieldType type          { get; set; }
    IObjField default_value { get; set; }

    string serialize(int widest_field, int widest_type);
} // interface IArchetypeNamedField


public class ArchetypeField : IArchetypeField {
        // TODO: More archetype data, such as policy settings for load/save, field auto-vivify, 
        //       per-Archetype-field validation logic, etc...
        //       Thus, possibly min_value, max_value, etc...
        public string name { get; set; }
        public FieldType type { get; set; }
        public IObjField default_value { get; set; }

        public ArchetypeField(string _name_, FieldType _type_) {
            if (_name_ == null) { Error.BadArg("Got null archetype parse_field name"); }
            if (!Token.is_bare_multi_word(_name_)) { Error.BadArg("Got invalid parse_field name '{0}'", _name_); }
            if (_type_ == null) { Error.BadArg("Got null FieldType"); }

            name = _name_;
            type = _type_;

            // Initialize default_value with a suitable IObjField instance
            // 
            // Yes, doing an switch on types in an OO system is often a code smell.
            // Does anyone have an aesthetically better means of implementing Duck-Typing in C# prior to C# 4.0 ?
            // 
            switch (_type_.semantic_type) {
                case SemanticTypes.INT:
                    default_value = new FieldInt();
                    break;
                case SemanticTypes.STRING:
                    default_value = new FieldString();
                    break;
                case SemanticTypes.DECIMAL:
                    default_value = new FieldDecimal();
                    break;
                case SemanticTypes.ID:
                    default_value = new FieldID();
                    break;

                case SemanticTypes.LIST_INT:
                    default_value = new FieldListInt();
                    break;
                case SemanticTypes.LIST_STRING:
                    default_value = new FieldListString();
                    break;
                case SemanticTypes.LIST_DECIMAL:
                    default_value = new FieldListDecimal();
                    break;
                case SemanticTypes.LIST_ID:
                    default_value = new FieldListID();
                    break;

                default:
                    Error.BadArg("Got unknown field type '{0}'", _type_);
                    break;
            }
            // This method sets up an IObjField with the default C# value for the storage type 
            // (0, "", 0.0M, or an empty list of one of these).
            // The additional constructors below (with additional args) are called
            // when a default field value (or a non-empty default list) is specified, 
            // which is possibly the more common case.

        } // ArchetypeField(name, type)

        public override string ToString() {
            return String.Format("ArchetypeField {0}, type={1}, default={2}", name, type.ToString(), default_value.ToString());
        }

        public string serialize(int widest_field, int widest_type) {
            string format_string;
            // TODO: Whether to elide default-valued field values should be policy-controlled...
            //if (default_value.is_default_valued())
            //{
            //    // format_string = "{0,-12}";  // Without dynamic field widths, not as nice
            //    format_string = "{0,-" + widest_type + "}";
            //    return String.Format(format_string, type.ToString() );
            //}

            // format_string = "{0,-12}, {1}";  // Without dynamic field widths, not as nice
            // Hmmm...currently this produces output like 'ID     , 42'
            // It might be nice to snug up the comma, or perhaps to simply replace it with '=' or 'default=' ...
            format_string = "{0,-" + widest_type + "}, {1}";
            return String.Format(format_string, type.ToString(), default_value.ToString());
        }




    } // class ArchetypeField

