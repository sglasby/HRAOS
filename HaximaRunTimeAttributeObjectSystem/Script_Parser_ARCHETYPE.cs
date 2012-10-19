using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

partial class Script_Parser {
    // The file contains the methods for parse nodes specific to the ARCHETYPE entity.
    // That is to say, methods which gather info to call 'new Archetype()'
    // and subsequently 'arch.add_field()'


    public int parse_ARCH_decl() {
        // TODO: (After OBJ is integrated fully)
        // Change Archetype to use an OBJ-style preamble section...

        // <archetype_decl> := ARCHETYPE <tag_name> [<comma> <ID> '=' <int_value>] '{' <fields> '}'
        int total_tokens = 0;
        int nn = accept(TokenType.ARCHETYPE);
        if (nn == 0) { return 0; }
        total_tokens += nn;

        nn = parse_ARCH_tag_name();
        if (nn == 0) { error("Expected an archetype name"); }
        total_tokens += nn;

        int id = 0;
        nn = accept(TokenType.COMMA);
        if (nn > 0) {
            total_tokens += nn;
            total_tokens += expect(TokenType.ID);
            total_tokens += expect(TokenType.EQUAL_SIGN);
            total_tokens += expect(TokenType.INT_VALUE);
            id = tokens[tmp.token_index].extract_int();
        }
        tmp.new_archetype = new Archetype(tmp.archetype_tag, id);
        //Form1.stdout.print("new Archetype(tag='{0}', ID='{1}') got_ID={2}\n", tmp.new_archetype.tag, id, tmp.new_archetype.ID);

        Form1.arch_list.Add(tmp.new_archetype);  // TEMP: Just for debug/demo purposes, replace with something proper, or rely on the reference in the ID registry

        total_tokens += expect(TokenType.L_CURLY);
        total_tokens += parse_ARCH_fields();
        total_tokens += expect(TokenType.R_CURLY);

        return total_tokens;
    }

    public int parse_ARCH_tag_name() {
        // TODO: (After OBJ is integrated fully)
        // Change Archetype to use an OBJ-style preamble section...

        // <arch_tag_name> := <bare_multi_word>
        int nn = accept(TokenType.BARE_MULTI_WORD);
        if (nn > 0) {
            tmp.archetype_tag = tokens[tmp.token_index].extract_bare_multi_word();
        }
        return nn;
    }

    public int parse_ARCH_fields() {
        // <arch_fields> := <arch_field> <arch_fields> |
        int nn = 0;
        while (true) {
            int tokens_consumed = parse_ARCH_field();
            if (tokens_consumed == 0) { break; }
            nn += tokens_consumed;
        }
        return nn;
    }

    public int parse_ARCH_field() {
        // <arch_field>  := <field_name> '=>' <ARCH_field_contents>
        int nn = 0;
        nn += parse_field_name();
        if (nn == 0) { return 0; }  // Usually because we are after the last field in an ARCH_decl
        nn += expect(TokenType.ARROW_COMMA);
        nn += parse_ARCH_field_contents();
        return nn;
    }

    /********************/

    public int parse_ARCH_field_contents() {
        // <ARCH_field_contents> := <ARCH_scalar_contents> | <ARCH_list_contents>
        int nn = 0;
        nn = parse_ARCH_scalar_contents();
        if (nn == 0) {
            nn = parse_ARCH_list_contents();
        }
        return nn;
    }

    public int parse_ARCH_scalar_contents() {
        // <ARCH_scalar_contents> := <ARCH_int_contents>      | <ARCH_string_contents>      | <ARCH_decimal_contents>      | <ARCH_ID_contents>
        int nn = 0;
        nn = parse_ARCH_int_contents();
        if (nn == 0) {
            nn = parse_ARCH_string_contents();
        }
        if (nn == 0) {
            nn = parse_ARCH_decimal_contents();
        }
        if (nn == 0) {
            nn = parse_ARCH_ID_contents();
        }
        return nn;
    }

    public int parse_ARCH_list_contents() {
        // <ARCH_list_contents>   := <ARCH_list_int_contents> | <ARCH_list_string_contents> | <ARCH_list_decimal_contents> | <ARCH_list_ID_contents>
        int nn;
        nn = parse_ARCH_list_int_contents();
        if (nn > 0) { return nn; }

        nn = parse_ARCH_list_string_contents();
        if (nn > 0) { return nn; }

        nn = parse_ARCH_list_decimal_contents();
        if (nn > 0) { return nn; }

        nn = parse_ARCH_list_ID_contents();
        return nn;
    }

    /********************/

    public int parse_ARCH_int_contents() {
        // <ARCH_int_contents> := <int_type> [',' <int_value> ]
        int nn;
        nn = parse_int_type();
        if (nn == 0) { return 0; }
        int comma_nn = accept(TokenType.COMMA);
        if (comma_nn > 0) {
            nn += comma_nn;
            nn += parse_int_value();
        }
        else {
            // TODO: Archetype: If per-Semantic-type default values exist, here is where to set that default.
            tmp.int_val = 0;
        }
        //Form1.stdout.print("p: ARCH scalar int {0}\n", tmp.int_val);
        tmp.new_archetype.add_field(tmp.field_name, tmp.type_of_field, tmp.int_val);
        //Form1.stdout.print("    ARCH add_field(name={0},type={1},val={2})\n", tmp.field_name, tmp.type_of_field, tmp.int_val);
        tmp.clear_field_data();
        return nn;
    }

    public int parse_ARCH_string_contents() {
        // <ARCH_string_contents>  := <string_type>  [',' <string_value> ]
        int nn = parse_string_type();
        if (nn == 0) { return 0; }
        int comma_nn = accept(TokenType.COMMA);
        if (comma_nn > 0) {
            nn += comma_nn;
            nn += parse_string_value();
        }
        else {
            tmp.string_val = "";  // TODO: If per-Semantic-type default values exist, here is where to set that default.
        }
        //Form1.stdout.print("p: ARCH scalar string '{0}'\n", tmp.string_val);
        tmp.new_archetype.add_field(tmp.field_name, tmp.type_of_field, tmp.string_val);
        //Form1.stdout.print("    ARCH add_field(name={0},type={1},val='{2}')\n", tmp.field_name, tmp.type_of_field, tmp.string_val);
        tmp.clear_field_data();
        return nn;
    }

    public int parse_ARCH_decimal_contents() {
        // <ARCH_decimal_contents> := <decimal_type> [',' <decimal_value>]
        int nn = parse_decimal_type();
        if (nn == 0) { return 0; }
        int comma_nn = accept(TokenType.COMMA);
        if (comma_nn > 0) {
            nn += comma_nn;
            nn += parse_decimal_value();
        }
        else {
            tmp.decimal_val = 0M;  // TODO: If per-Semantic-type default values exist, here is where to set that default.
        }
        //Form1.stdout.print("p: ARCH scalar decimal {0}\n", tmp.decimal_val);
        tmp.new_archetype.add_field(tmp.field_name, tmp.type_of_field, tmp.decimal_val);
        //Form1.stdout.print("    ARCH add_field(name={0},type={1},val={2})\n", tmp.field_name, tmp.type_of_field, tmp.decimal_val);
        tmp.clear_field_data();
        return nn;
    }

    public int parse_ARCH_ID_contents() {
        // <ARCH_ID_contents> := <ID_type> [',' <ID_value> ]
        int nn = parse_ID_type();
        if (nn == 0) { return 0; }
        int comma_nn = accept(TokenType.COMMA);
        if (comma_nn > 0) {
            nn += comma_nn;
            nn += parse_ID_value();
        }
        else {
            tmp.int_val = 0;  // TODO: If per-Semantic-type default values exist, here is where to set that default.
        }
        //Form1.stdout.print("p: ARCH scalar ID {0}\n", tmp.int_val);
        tmp.new_archetype.add_field(tmp.field_name, tmp.type_of_field, tmp.int_val);
        //Form1.stdout.print("    ARCH add_field(name={0},type={1},val={2})\n", tmp.field_name, tmp.type_of_field, tmp.int_val);
        tmp.clear_field_data();
        return nn;
    }

    public int parse_ARCH_list_int_contents() {
        // <ARCH_list_int_contents> := <int_list_type> [',' <int_list_value> ]
        int nn = parse_int_list_type();
        if (nn == 0) { return 0; }
        tmp.list_int = new List<int>();
        int comma_nn = accept(TokenType.COMMA);
        if (comma_nn > 0) {
            nn += comma_nn;
            nn += parse_int_list_value();
        }
        // The Archetype was created earlier in parse_ARCH_decl().
        // The field name was parsed and stowed earlier in (the archetype preamble code, currently parse_ARCH_tag_name() ).
        // The List<int> to pass to the constructor was set earlier in this method.
        // The contents of that list were parsed and stowed within parse_zero_or_more_comma_separated_int_values().

        // Thus, we can now call add_field():
        tmp.new_archetype.add_field(tmp.field_name, tmp.type_of_field, tmp.list_int);

        //Form1.stdout.print("    add_field(name={0},type={1},[", tmp.field_name, tmp.type_of_field);
        //foreach (int ii in tmp.list_int)
        //{
        //    Form1.stdout.print("{0},", ii);
        //}
        //Form1.stdout.print("]\n");
        tmp.clear_field_data();

        return nn;
    }

    public int parse_ARCH_list_string_contents() {
        // <ARCH_list_string_contents> := <string_list_type> [',' <string_list_value> ]
        int nn = parse_string_list_type();
        if (nn == 0) { return 0; }
        tmp.list_string = new List<string>();
        int comma_nn = accept(TokenType.COMMA);
        if (comma_nn > 0) {
            nn += comma_nn;
            nn += parse_string_list_value();
        }

        // Thus, now we can call add_field():
        tmp.new_archetype.add_field(tmp.field_name, tmp.type_of_field, tmp.list_string);

        //Form1.stdout.print("    add_field(name={0},type={1},[", tmp.field_name, tmp.type_of_field);
        //foreach (string ss in tmp.list_string)
        //{
        //    Form1.stdout.print("{0},", ss);
        //}
        //Form1.stdout.print("]\n");
        tmp.clear_field_data();

        return nn;
    }

    public int parse_ARCH_list_decimal_contents() {
        // <ARCH_list_decimal_contents> := <decimal_list_type> [',' <decimal_list_value>]
        int nn = parse_decimal_list_type();
        if (nn == 0) { return 0; }
        tmp.list_decimal = new List<decimal>();
        int comma_nn = accept(TokenType.COMMA);
        if (comma_nn > 0) {
            nn += comma_nn;
            nn += parse_decimal_list_value();
        }

        // Thus, now we can call add_field():
        tmp.new_archetype.add_field(tmp.field_name, tmp.type_of_field, tmp.list_decimal);

        //Form1.stdout.print("    add_field(name={0},type={1},[", tmp.field_name, tmp.type_of_field);
        //foreach (decimal dd in tmp.list_decimal)
        //{
        //    Form1.stdout.print("{0},", dd);
        //}
        //Form1.stdout.print("]\n");
        tmp.clear_field_data();

        return nn;
    }

    public int parse_ARCH_list_ID_contents() {
        // <ARCH_list_ID_contents> := <ID_list_type> [',' <ID_list_value> ]
        int nn = parse_ID_list_type();
        if (nn == 0) { return 0; }
        tmp.list_int = new List<int>();
        int comma_nn = accept(TokenType.COMMA);
        if (comma_nn > 0) {
            nn += comma_nn;
            nn += parse_ID_list_value();
        }

        // Thus, now we can call add_field():
        tmp.new_archetype.add_field(tmp.field_name, tmp.type_of_field, tmp.list_int);

        //Form1.stdout.print("    add_field(name={0},type={1},[", tmp.field_name, tmp.type_of_field);
        //foreach (int ii in tmp.list_int)
        //{
        //    Form1.stdout.print("{0},", ii);
        //}
        //Form1.stdout.print("]\n");
        tmp.clear_field_data();

        return nn;
    }

    /********************/



} // partial class Script_Parser
