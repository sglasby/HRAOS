using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

partial class Script_Parser {
    // The file contains the methods for parse nodes specific to the OBJ entity.
    // That is to say, methods which gather info to call 'new Obj()'
    // and subsequently 'obj.add_field()'

    public int parse_OBJ_decl() {
        // <OBJ_decl> := OBJ '(' <OBJ_preamble> ')' '{' <OBJ_fields> '}'
        int total_tokens = 0;
        int nn = accept(TokenType.OBJ);
        if (nn == 0) { return 0; }
        total_tokens += nn;

        total_tokens += expect(TokenType.L_PAREN);
        total_tokens += parse_OBJ_preamble();
        total_tokens += expect(TokenType.R_PAREN);

        Archetype arch = tmp.parent_archetype;  // Must be defined
        string    tag  = tmp.obj_tag;           // May be null
        int       ID   = tmp.ID;                // Is either zero or a  positive integer

        tmp.parent_archetype = null;  // Resetting data after it's used, to avoid confusion when debugging...
        tmp.obj_tag = null;
        tmp.ID = 0;

        tmp.new_obj = new Obj(arch, tag, ID);
        //Form1.stdout.print("parse_obj_decl() -- new Obj(arch='{0}', tag='{1}', ID_arg='{2}')  got ID='{3}'\n", arch.tag, tag, ID, tmp.new_obj.ID);
        Form1.obj_list.Add(tmp.new_obj);  // TEMP: For debug/demo purposes; the references will be kept in the Object_Registrar...

        total_tokens += expect(TokenType.L_CURLY);
        total_tokens += parse_OBJ_fields();
        total_tokens += expect(TokenType.R_CURLY);

        return total_tokens;
    }

    public int parse_OBJ_preamble() {
        // <OBJ_preamble> := <OBJ_preamble_field> [','] <OBJ_preamble> |
        int total_tokens = 0;
        while (true) {
            int nn = parse_OBJ_preamble_field();
            if (nn == 0) { break; }
            total_tokens += nn;

            nn = accept(TokenType.COMMA);  // Yes, the comma is optional.  An eccentric choice, might revisit it...
            total_tokens += nn;
        }
        // Check for mandatory fields (just ARCHETYPE for now)
        if (tmp.parent_archetype == null) { error("OBJ preamble did not specify an ARCHETYPE"); }
        return total_tokens;
    }

    public int parse_OBJ_preamble_field() {
        // <OBJ_preamble_field> := <OBJ_parent_ARCH_field> | <OBJ_tag_field> | <OBJ_ID_field>
        int nn = parse_OBJ_parent_archetype_field();
        if (nn > 0) { return nn; }

        nn = parse_OBJ_tag_field();
        if (nn > 0) { return nn; }

        nn = parse_OBJ_ID_field();
        if (nn > 0) { return nn; }

        return 0;
    }

    public int parse_OBJ_parent_archetype_field() {
        // <obj_parent_archetype_field> := ARCHETYPE '=>' <tag_name>   // Semantic: Mandatory(1)
        int total_tokens = 0;
        int nn = accept(TokenType.ARCHETYPE);
        if (nn == 0) { return 0; }
        total_tokens += nn;

        // Paranoia in Parsing:  Max 1 instance of this field in preamble (parent checks for minimum of 1)
        if (tmp.parent_archetype != null) { error("Found duplicate ARCHETYPE field in OBJ preamble"); }

        nn = expect(TokenType.ARROW_COMMA);
        total_tokens += nn;

        nn = expect(TokenType.BARE_MULTI_WORD);  // TODO: Extract a method for this into the terminals file, perhaps rename valid_tag to defined_tab, etc etc...
        total_tokens += nn;
        string tag_name = tokens[tmp.token_index].extract_bare_multi_word();

        // Check for validity of tag:
        // TODO: Should we accept a bare ID here, for consistency???
        // TODO: Can this be extracted as a check-for-valid-tag method?  (Would want an arg for a context-specific error upon failure...)
        //       (Perhaps same question for the checks in the next 2 methods...)
        int ID = ObjectRegistrar.All.ID_for_tag(tag_name);
        if (ID == 0) { error("Got invalid ARCHETYPE tag '{0}' for OBJ", tag_name); }
        tmp.parent_archetype = ObjectRegistrar.All.Archetype_for_ID(ID);

        return total_tokens;
    }

    public int parse_OBJ_tag_field() {
        // <OBJ_tag_field> := TAG '=>' <tag_name>   // Semantic: Optional(0,1)
        int total_tokens = 0;
        int nn = accept(TokenType.TAG);
        if (nn == 0) { return 0; }
        total_tokens += nn;

        // Paranoia in Parsing:  Max 1 instance of this field in preamble
        if (tmp.obj_tag != null) { error("Found duplicate TAG field in OBJ preamble"); }

        nn = expect(TokenType.ARROW_COMMA);
        total_tokens += nn;

        nn = expect(TokenType.BARE_MULTI_WORD);
        total_tokens += nn;
        string tag_name = tokens[tmp.token_index].extract_bare_multi_word();

        // Check for validity of tag:  (should be unused thus far)
        if (ObjectRegistrar.All.ID_for_tag(tag_name) != 0) { error("Got already-used TAG '{0}' in OBJ preamble", tag_name); }
        tmp.obj_tag = tag_name;

        return total_tokens;
    }

    public int parse_OBJ_ID_field() {
        // <OBJ_ID_field> := ID '=>' <int_value>  // Semantic: Optional(0,1)
        int total_tokens = 0;
        int nn = accept(TokenType.ID);
        if (nn == 0) { return 0; }
        total_tokens += nn;

        // Paranoia in Parsing:  Max 1 instance of this field in preamble
        if (tmp.ID != 0) { error("Found duplicate ID field in OBJ preamble"); }

        nn = expect(TokenType.ARROW_COMMA);
        total_tokens += nn;

        nn = expect(TokenType.INT_VALUE);  // TODO: perhaps constrain to a type with value >= 0 ???
        total_tokens += nn;
        int ID = tokens[tmp.token_index].extract_int();
        // The new ID may be zero or any unused positive integer
        if (ID < 0) { error("Got negative integer for ID in OBJ preamble"); }
        if (ObjectRegistrar.All.is_valid_ID(ID)) { error("Got already in-use ID '{0}' in OBJ preamble", ID); }
        tmp.ID = ID;

        return total_tokens;
    }

    public int parse_OBJ_fields() {
        // <OBJ_fields> := <OBJ_field> <OBJ_fields> |
        int total_tokens = 0;
        while (true) {
            int nn = parse_OBJ_field();
            if (nn == 0) { break; }
            total_tokens += nn;
        }
        return total_tokens;
    }

    public int parse_OBJ_field() {
        // <OBJ_field>  := <field_name> '=>' <OBJ_field_contents> [',']
        int total_tokens = 0;
        int nn = parse_field_name();
        if (nn == 0) { return 0; }
        total_tokens += nn;

        nn = expect(TokenType.ARROW_COMMA);
        total_tokens += nn;

        nn = parse_OBJ_field_contents();
        if (nn == 0) { error("Invalid field_contents in OBJ field"); }
        total_tokens += nn;

        nn = accept(TokenType.COMMA);  // Yes, the comma is optional.  An eccentric choice, might revisit it...
        total_tokens += nn;

        return total_tokens;
    }

    /********************/

    public int parse_OBJ_field_contents() {
        // <OBJ_field_contents>  := <OBJ_scalar_contents> | <OBJ_list_contents> |
        int nn = parse_OBJ_scalar_contents();
        if (nn > 0) { return nn; }
        nn = parse_OBJ_list_contents();
        if (nn > 0) { return nn; }
        return 0;
    }

    public int parse_OBJ_scalar_contents() {
        // <OBJ_scalar_contents> := <OBJ_int_contents> | <OBJ_string_contents> | <OBJ_decimal_contents> | <OBJ_ID_contents>
        int nn = parse_OBJ_int_contents();
        if (nn > 0) { return nn; }
        nn = parse_OBJ_string_contents();
        if (nn > 0) { return nn; }
        nn = parse_OBJ_decimal_contents();
        if (nn > 0) { return nn; }
        nn = parse_OBJ_ID_contents();
        if (nn > 0) { return nn; }
        return 0;
    }

    public int parse_OBJ_list_contents() {
        // <OBJ_list_contents> := <OBJ_list_int_contents> | <OBJ_list_string_contents> | <OBJ_list_decimal_contents> | <OBJ_list_ID_contents>
        int nn = parse_OBJ_list_int_contents();
        if (nn > 0) { return nn; }
        nn = parse_OBJ_list_string_contents();
        if (nn > 0) { return nn; }
        nn = parse_OBJ_list_decimal_contents();
        if (nn > 0) { return nn; }
        nn = parse_OBJ_list_ID_contents();
        if (nn > 0) { return nn; }
        return 0;
    }

    /********************/

    public int parse_OBJ_int_contents() {
        // <OBJ_int_contents> := <int_value> | <int_type> [',' <int_value> ]
        IArchetypeField arch_field = tmp.new_obj.archetype[tmp.field_name];
        int nn;
        nn = parse_int_value();
        if (nn > 0) {
            // An untyped field, set the expected field type
            if (arch_field != null) {
                // From the archetype, if possible
                if (arch_field.type != FieldType.INT) { back_up_parser(nn); return 0; }
                tmp.type_of_field = arch_field.type;
            }
            else {
                // Otherwise we expect INT (and untyped fields intended to be ID or other derived-from-INT are thus consumed)
                tmp.type_of_field = FieldType.INT;
                warn_add_OBJ_field_defaulting_to_basic_field_type(tmp.field_name, tmp.new_obj.archetype.tag, FieldType.INT);
            }
        }
        else {
            nn = parse_int_type();
            if (nn == 0) { return 0; }
            // A typed field.  Type may optionally be followed by a value.
            nn += accept(TokenType.COMMA);
            nn += parse_int_value();
        }
        // Check that INT is the archetype field type, and we're ready to add the Obj field
        if (arch_field == null) {
            warn_add_OBJ_field_not_in_archetype(tmp.field_name, tmp.new_obj.archetype.tag);
        }
        else if (arch_field.type != tmp.type_of_field) {
            warn_add_OBJ_field_mismatch_with_archetype_field(tmp.new_obj, tmp.field_name, tmp.type_of_field);
            tmp.clear_field_data();
            return nn;  // We will consume the rest of the declaration syntax, ignoring these contents
        }
        tmp.new_obj.add_field(tmp.field_name, tmp.type_of_field);
        tmp.new_obj[tmp.field_name].iv = tmp.int_val;

        //Form1.stdout.print("    OBJ add_field(name={0},type={1},val={2})\n", tmp.field_name, tmp.type_of_field, tmp.int_val);
        tmp.clear_field_data();
        return nn;
    }

    public int parse_OBJ_string_contents() {
        // <OBJ_string_contents> := <string_value> | <string_type> [',' <string_value> ]
        IArchetypeField arch_field = tmp.new_obj.archetype[tmp.field_name];
        int nn;
        nn = parse_string_value();
        if (nn > 0) {
            // An untyped field, set the expected field type
            if (arch_field != null) {
                if (arch_field.type != FieldType.STRING) { back_up_parser(nn); return 0; }
                tmp.type_of_field = arch_field.type;
            }
            else {
                tmp.type_of_field = FieldType.STRING;
                warn_add_OBJ_field_defaulting_to_basic_field_type(tmp.field_name, tmp.new_obj.archetype.tag, FieldType.STRING);
            }
        }
        else {
            nn = parse_string_type();
            if (nn == 0) { return 0; }
            // A typed field.  Type may optionally be followed by a value.
            nn += accept(TokenType.COMMA);
            nn += parse_string_value();
        }
        // Check that STRING is the archetype field type, and we're ready to add the Obj field
        if (arch_field == null) {
            warn_add_OBJ_field_not_in_archetype(tmp.field_name, tmp.new_obj.archetype.tag);
        }
        else if (arch_field.type != tmp.type_of_field) {
            warn_add_OBJ_field_mismatch_with_archetype_field(tmp.new_obj, tmp.field_name, tmp.type_of_field);
            tmp.clear_field_data();
            return nn;  // We will consume the rest of the declaration syntax, ignoring these contents

        }
        tmp.new_obj.add_field(tmp.field_name, tmp.type_of_field);
        tmp.new_obj[tmp.field_name].sv = tmp.string_val;

        //Form1.stdout.print("    OBJ add_field(name={0},type={1},val={2})\n", tmp.field_name, tmp.type_of_field, tmp.decimal_val);
        tmp.clear_field_data();
        return nn;
    }

    public int parse_OBJ_decimal_contents() {
        // <OBJ_decimal_contents> := <decimal_value> | <decimal_type> [',' <decimal_value> ]
        IArchetypeField arch_field = tmp.new_obj.archetype[tmp.field_name];
        int nn;
        nn = parse_decimal_value();
        if (nn > 0) {
            // An untyped field, set the expected field type
            if (arch_field != null) {
                if (arch_field.type != FieldType.DECIMAL) { back_up_parser(nn); return 0; }
                tmp.type_of_field = arch_field.type;
            }
            else {
                tmp.type_of_field = FieldType.DECIMAL;
                warn_add_OBJ_field_defaulting_to_basic_field_type(tmp.field_name, tmp.new_obj.archetype.tag, FieldType.DECIMAL);
            }
        }
        else {
            nn = parse_decimal_type();
            if (nn == 0) { return 0; }
            // A typed field.  Type may optionally be followed by a value.
            nn += accept(TokenType.COMMA);
            nn += parse_decimal_value();
        }
        // Check that DECIMAL is the archetype field type, and we're ready to add the Obj field
        if (arch_field == null) {
            warn_add_OBJ_field_not_in_archetype(tmp.field_name, tmp.new_obj.archetype.tag);
        }
        else if (arch_field.type != tmp.type_of_field) {
            warn_add_OBJ_field_mismatch_with_archetype_field(tmp.new_obj, tmp.field_name, tmp.type_of_field);
            tmp.clear_field_data();
            return nn;  // We will consume the rest of the declaration syntax, ignoring these contents

        }
        tmp.new_obj.add_field(tmp.field_name, tmp.type_of_field);
        tmp.new_obj[tmp.field_name].dv = tmp.decimal_val;

        //Form1.stdout.print("    OBJ add_field(name={0},type={1},val={2})\n", tmp.field_name, tmp.type_of_field, tmp.decimal_val);
        tmp.clear_field_data();
        return nn;
    }

    public int parse_OBJ_ID_contents() {
        // <OBJ_ID_contents> := <ID_value> | <ID_type> [',' <ID_value> ]
        IArchetypeField arch_field = tmp.new_obj.archetype[tmp.field_name];
        int nn;
        nn = parse_ID_value();
        if (nn > 0) {
            // TODO: 
            // Given that (due to the grammar and the ordering in parse_OBJ_scalar_contents() )
            // parse_OBJ_int_contents() will always be called before this method,
            // under exactly what circumstances will this be called?
            // 
            // Needs careful thought.  For now, handling this like for other types...
            if (arch_field != null) {
                if (arch_field.type != FieldType.ID) { back_up_parser(nn); return 0; }
                tmp.type_of_field = arch_field.type;
            }
            else {
                // It _seems_ that this case will NEVER get hit, 
                // as any (untyped + not-in-archetype fields) would get grabbed by parse_OBJ_int_contents().
                // TODO: Review this carefully...
                tmp.type_of_field = FieldType.ID;
                warn_add_OBJ_field_defaulting_to_basic_field_type(tmp.field_name, tmp.new_obj.archetype.tag, FieldType.ID);
            }
        }
        else {
            nn = parse_ID_type();
            if (nn == 0) { return 0; }
            // A typed field.  Type may optionally be followed by a value.
            nn += accept(TokenType.COMMA);
            nn += parse_ID_value();
        }
        // Check that ID is the archetype field type, and we're ready to add the Obj field
        if (arch_field == null) {
            warn_add_OBJ_field_not_in_archetype(tmp.field_name, tmp.new_obj.archetype.tag);
        }
        else if (arch_field.type != tmp.type_of_field) {
            warn_add_OBJ_field_mismatch_with_archetype_field(tmp.new_obj, tmp.field_name, tmp.type_of_field);
            tmp.clear_field_data();
            return nn;  // We will consume the rest of the declaration syntax, ignoring these contents

        }
        tmp.new_obj.add_field(tmp.field_name, tmp.type_of_field);
        tmp.new_obj[tmp.field_name].iv = tmp.int_val;

        //Form1.stdout.print("    OBJ add_field(name={0},type={1},val={2})\n", tmp.field_name, tmp.type_of_field, tmp.int_val);
        tmp.clear_field_data();
        return nn;

    }

    /********************/

    public int parse_OBJ_list_int_contents() {
        // <OBJ_list_int_contents> := <int_list_value> | <int_list_type> [',' <int_list_value> ]
        IArchetypeField arch_field = tmp.new_obj.archetype[tmp.field_name];
        int nn;
        tmp.list_int = new List<int>();
        nn = parse_int_list_value();
        if (nn > 0) {
            // An untyped field, set the expected field type
            if (arch_field != null) {
                if (arch_field.type != FieldType.LIST_INT) { back_up_parser(nn); return 0; }
                tmp.type_of_field = arch_field.type;
            }
            else {
                tmp.type_of_field = FieldType.LIST_INT;
                warn_add_OBJ_field_defaulting_to_basic_field_type(tmp.field_name, tmp.new_obj.archetype.tag, FieldType.LIST_INT);
            }
        }
        else {
            nn = parse_int_list_type();
            if (nn == 0) { return 0; }
            // A typed field.  Type may optionally be followed by a value.
            nn += accept(TokenType.COMMA);
            nn += parse_int_list_value();
        }
        // Check that LIST_INT is the archetype field type, and we're ready to add the Obj field
        if (arch_field == null) {
            warn_add_OBJ_field_not_in_archetype(tmp.field_name, tmp.new_obj.archetype.tag);
        }
        else if (arch_field.type != tmp.type_of_field) {
            warn_add_OBJ_field_mismatch_with_archetype_field(tmp.new_obj, tmp.field_name, tmp.type_of_field);
            tmp.clear_field_data();
            return nn;  // We will consume the rest of the declaration syntax, ignoring these contents

        }
        tmp.new_obj.add_field(tmp.field_name, tmp.type_of_field);
        tmp.new_obj[tmp.field_name].ilist = tmp.list_int;
        print_debug_for_add_list_field(tmp.field_name, tmp.type_of_field, tmp.list_int, null, null);
        tmp.clear_field_data();
        return nn;
    }

    public int parse_OBJ_list_string_contents() {
        // <OBJ_list_string_contents> := <string_list_value> | <string_list_type> [',' <string_list_value> ]
        IArchetypeField arch_field = tmp.new_obj.archetype[tmp.field_name];
        int nn;
        tmp.list_string = new List<string>();
        nn = parse_string_list_value();
        if (nn > 0) {
            // An untyped field, set the expected field type
            if (arch_field != null) {
                if (arch_field.type != FieldType.LIST_STRING) { back_up_parser(nn); return 0; }
                tmp.type_of_field = arch_field.type;
            }
            else {
                tmp.type_of_field = FieldType.LIST_STRING;
                warn_add_OBJ_field_defaulting_to_basic_field_type(tmp.field_name, tmp.new_obj.archetype.tag, FieldType.LIST_STRING);
            }
        }
        else {
            nn = parse_string_list_type();
            if (nn == 0) { return 0; }
            // A typed field.  Type may optionally be followed by a value.
            nn += accept(TokenType.COMMA);
            nn += parse_string_list_value();
        }
        // Check that LIST_STRING is the archetype field type, and we're ready to add the Obj field
        if (arch_field == null) {
            warn_add_OBJ_field_not_in_archetype(tmp.field_name, tmp.new_obj.archetype.tag);
        }
        else if (arch_field.type != tmp.type_of_field) {
            warn_add_OBJ_field_mismatch_with_archetype_field(tmp.new_obj, tmp.field_name, tmp.type_of_field);
            tmp.clear_field_data();
            return nn;  // We will consume the rest of the declaration syntax, ignoring these contents

        }
        tmp.new_obj.add_field(tmp.field_name, tmp.type_of_field);
        tmp.new_obj[tmp.field_name].slist = tmp.list_string;
        print_debug_for_add_list_field(tmp.field_name, tmp.type_of_field, null, tmp.list_string, null);
        tmp.clear_field_data();
        return nn;
    }

    public int parse_OBJ_list_decimal_contents() {
        // <OBJ_list_decimal_contents> := <decimal_list_value> | <decimal_list_type> [',' <decimal_list_value>]
        IArchetypeField arch_field = tmp.new_obj.archetype[tmp.field_name];
        int nn;
        tmp.list_decimal = new List<decimal>();
        nn = parse_decimal_list_value();
        if (nn > 0) {
            // An untyped field, set the expected field type
            if (arch_field != null) {
                if (arch_field.type != FieldType.LIST_DECIMAL) { back_up_parser(nn); return 0; }
                tmp.type_of_field = arch_field.type;
            }
            else {
                tmp.type_of_field = FieldType.LIST_DECIMAL;
                warn_add_OBJ_field_defaulting_to_basic_field_type(tmp.field_name, tmp.new_obj.archetype.tag, FieldType.LIST_DECIMAL);
            }
        }
        else {
            nn = parse_decimal_list_type();
            if (nn == 0) { return 0; }
            // A typed field.  Type may optionally be followed by a value.
            nn += accept(TokenType.COMMA);
            nn += parse_decimal_list_value();
        }
        // Check that LIST_DECIMAL is the archetype field type, and we're ready to add the Obj field
        if (arch_field == null) {
            warn_add_OBJ_field_not_in_archetype(tmp.field_name, tmp.new_obj.archetype.tag);
        }
        else if (arch_field.type != tmp.type_of_field) {
            warn_add_OBJ_field_mismatch_with_archetype_field(tmp.new_obj, tmp.field_name, tmp.type_of_field);
            tmp.clear_field_data();
            return nn;  // We will consume the rest of the declaration syntax, ignoring these contents

        }
        tmp.new_obj.add_field(tmp.field_name, tmp.type_of_field);
        tmp.new_obj[tmp.field_name].dlist = tmp.list_decimal;
        print_debug_for_add_list_field(tmp.field_name, tmp.type_of_field, null, null, tmp.list_decimal);
        tmp.clear_field_data();
        return nn;
    }

    public int parse_OBJ_list_ID_contents() {
        // <OBJ_list_ID_contents> := <ID_list_value> | <ID_list_type> [',' <ID_list_value> ]
        IArchetypeField arch_field = tmp.new_obj.archetype[tmp.field_name];
        int nn;
        tmp.list_int = new List<int>();
        nn = parse_ID_list_value();
        if (nn > 0) {
            // An untyped field, set the expected field type
            if (arch_field != null) {
                if (arch_field.type != FieldType.LIST_ID) { back_up_parser(nn); return 0; }
                tmp.type_of_field = arch_field.type;
            }
            else {
                // It _seems_ that this will never be hit, for the same reasons as the analogous block in parse_OBJ_ID_contents()...
                tmp.type_of_field = FieldType.LIST_ID;
                warn_add_OBJ_field_defaulting_to_basic_field_type(tmp.field_name, tmp.new_obj.archetype.tag, FieldType.LIST_ID);
            }
        }
        else {
            nn = parse_ID_list_type();
            if (nn == 0) { return 0; }
            // A typed field.  Type may optionally be followed by a value.
            nn += accept(TokenType.COMMA);
            nn += parse_ID_list_value();
        }
        // Check that LIST_ID is the archetype field type, and we're ready to add the Obj field
        if (arch_field == null) {
            warn_add_OBJ_field_not_in_archetype(tmp.field_name, tmp.new_obj.archetype.tag);
        }
        else if (arch_field.type != tmp.type_of_field) {
            warn_add_OBJ_field_mismatch_with_archetype_field(tmp.new_obj, tmp.field_name, tmp.type_of_field);
            tmp.clear_field_data();
            return nn;  // We will consume the rest of the declaration syntax, ignoring these contents

        }
        tmp.new_obj.add_field(tmp.field_name, tmp.type_of_field);
        tmp.new_obj[tmp.field_name].ilist = tmp.list_int;
        print_debug_for_add_list_field(tmp.field_name, tmp.type_of_field, tmp.list_int, null, null);
        tmp.clear_field_data();

        return nn;
    }

    /********************/

} // partial class Script_Parser
