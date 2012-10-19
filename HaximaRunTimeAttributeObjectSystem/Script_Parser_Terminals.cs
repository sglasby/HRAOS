using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

partial class Script_Parser {
    // Parse method for terminal (and some near-terminal) parse nodes,
    // which can be called from various entity-specific nodes.
    // For example, parse_int_value() is called from both parse_ARCH_int_contents() and parse_OBJ_int_contents().

    public int parse_int_type() {
        // <int_type> := INT
        int nn = accept(TokenType.INT);
        if (nn > 0) {
            tmp.type_of_field = FieldType.INT;
        }
        return nn;
    }

    public int parse_string_type() {
        int nn = accept(TokenType.STRING);
        if (nn > 0) {
            tmp.type_of_field = FieldType.STRING;
        }
        return nn;
    }

    public int parse_decimal_type() {
        int nn = accept(TokenType.DECIMAL);
        if (nn > 0) {
            tmp.type_of_field = FieldType.DECIMAL;
        }
        return nn;
    }

    public int parse_ID_type() {
        // <ID_type> := ID
        int nn = accept(TokenType.ID);
        if (nn > 0) {
            tmp.type_of_field = FieldType.ID;
        }
        return nn;
    }

    /********************/

    public int parse_int_list_type() {
        // <int_list_type>     := LIST_INT
        int nn = accept(TokenType.LIST_INT);
        if (nn > 0) {
            tmp.type_of_field = FieldType.LIST_INT;
        }
        return nn;
    }

    public int parse_string_list_type() {
        // <string_list_type>  := LIST_STRING
        int nn = accept(TokenType.LIST_STRING);
        if (nn > 0) {
            tmp.type_of_field = FieldType.LIST_STRING;
        }
        return nn;
    }

    public int parse_decimal_list_type() {
        // <decimal_list_type> := LIST_DECIMAL
        int nn = accept(TokenType.LIST_DECIMAL);
        if (nn > 0) {
            tmp.type_of_field = FieldType.LIST_DECIMAL;
        }
        return nn;
    }

    public int parse_ID_list_type() {
        // <ID_list_type> := LIST_ID
        int nn = accept(TokenType.LIST_ID);
        if (nn > 0) {
            tmp.type_of_field = FieldType.LIST_ID;
        }
        return nn;
    }

    /********************/

    public int parse_int_value() {
        int nn = accept(TokenType.INT_VALUE);
        if (nn == 0) { return 0; }
        tmp.int_val = tokens[tmp.token_index].extract_int();
        return nn;
    }

    public int parse_string_value() {
        int nn = accept(TokenType.STRING_VALUE);
        if (nn == 0) { return 0; }
        tmp.string_val = tokens[tmp.token_index].extract_bare_string();
        return nn;
    }

    public int parse_decimal_value() {
        int nn = accept(TokenType.DECIMAL_VALUE);
        if (nn == 0) { return 0; }
        tmp.decimal_val = tokens[tmp.token_index].extract_bare_decimal();
        return nn;
    }

    public int parse_ID_value() {
        // TODO: Rewrite to use parse_int_value() and the like... ???

        // <ID_value> := <int_value> | <valid_tag>
        int nn = accept(TokenType.INT_VALUE);
        if (nn > 0) {
            int ID_value = tokens[tmp.token_index].extract_int();
            if (!ObjectRegistrar.All.is_valid_or_zero_ID(ID_value)) {
                // A zero ID will not resolve, otherwise we should have a live reference:
                // TODO: Replace exception with warning, parser backup ???
                error("Found non-zero ID '{0}' which does not resolve, possibly a forward reference?", ID_value);
            }
            tmp.int_val = ID_value;
            return nn;
        }
        // Have to accept() rather than expect(), because some callers can have zero elements:
        nn = accept(TokenType.BARE_MULTI_WORD);
        if (nn > 0) {
            string tag_value = tokens[tmp.token_index].extract_bare_multi_word();
            // Check if the tag corresponds to an already-registered object ID
            // Since we need to know the ID now, we don't support "forward references".

            // TODO: Should this also be replaced with a convenience method call defined in Object_Registrar ???
            int ID = ObjectRegistrar.All.ID_for_tag(tag_value);
            if (ID == 0) {
                // TODO: Replace exception with warning, parser backup ???
                error("Found tag '{0}' without ID, possibly a forward reference?", tag_value);
            }
            tmp.int_val = ID;
        }
        return nn;
    }

    /********************/

    public int parse_int_list_value() {
        // <int_list_value> := '[' <zero_or_more_comma_separated_int_values> ']'
        int total_tokens = 0;

        int nn = expect(TokenType.L_BRACKET);
        if (nn == 0) { return 0; }
        total_tokens += nn;

        nn = parse_zero_or_more_comma_separated_int_values();
        total_tokens += nn;

        nn = expect(TokenType.R_BRACKET);
        if (nn == 0) { back_up_parser(total_tokens); return 0; }
        total_tokens += nn;

        return total_tokens;
    }

    public int parse_string_list_value() {
        // <string_list_value> := '[' <zero_or_more_comma_separated_string_values> ']'
        int total_tokens = 0;

        int nn = expect(TokenType.L_BRACKET);
        if (nn == 0) { return 0; }
        total_tokens += nn;

        nn = parse_zero_or_more_comma_separated_string_values();
        total_tokens += nn;

        nn = expect(TokenType.R_BRACKET);
        if (nn == 0) { back_up_parser(total_tokens); return 0; }
        total_tokens += nn;

        return total_tokens;
    }

    public int parse_decimal_list_value() {
        // <decimal_list_value> := '[' <zero_or_more_comma_separated_decimal_values> ']'
        int total_tokens = 0;

        int nn = expect(TokenType.L_BRACKET);
        if (nn == 0) { return 0; }
        total_tokens += nn;

        nn = parse_zero_or_more_comma_separated_decimal_values();
        total_tokens += nn;

        nn = expect(TokenType.R_BRACKET);
        if (nn == 0) { back_up_parser(total_tokens); return 0; }
        total_tokens += nn;

        return total_tokens;
    }

    public int parse_ID_list_value() {
        // <ID_list_value> := '[' <zero_or_more_comma_separated_ID_values> ']'
        int total_tokens = 0;

        int nn = expect(TokenType.L_BRACKET);
        if (nn == 0) { return 0; }
        total_tokens += nn;

        nn = parse_zero_or_more_comma_separated_ID_values();
        total_tokens += nn;

        nn = expect(TokenType.R_BRACKET);
        if (nn == 0) { back_up_parser(total_tokens); return 0; }
        total_tokens += nn;

        return total_tokens;
    }

    /********************/

    public int parse_zero_or_more_comma_separated_int_values() {
        // <zero_or_more_comma_separated_int_values> := <int_value> ',' <zero_or_more_comma_separated_int_values> | <int_value> ',' | <int_value> |
        int total_tokens = 0;
        while (true) {
            int nn = accept(TokenType.INT_VALUE);  // List might have zero elements
            if (nn == 0) { break; }

            int ii = tokens[tmp.token_index].extract_int();
            //Form1.stdout.print("p: list element int {0}\n", ii);
            tmp.list_int.Add(ii);
            total_tokens += nn;

            nn = accept(TokenType.COMMA);  // Comma is optional after last element of list
            if (nn == 0) { break; }
            total_tokens += nn;
        }
        return total_tokens;
    }

    public int parse_zero_or_more_comma_separated_string_values() {
        // <zero_or_more_comma_separated_string_values>  := <string_value> ',' <zero_or_more_comma_separated_string_values> | <string_value> ',' | <string_value> |
        int total_tokens = 0;
        while (true) {
            int nn = accept(TokenType.STRING_VALUE);  // List might have zero elements
            if (nn == 0) { break; }

            string ss = tokens[tmp.token_index].extract_bare_string();
            //Form1.stdout.print("p: list element string '{0}'\n", ss);
            tmp.list_string.Add(ss);
            total_tokens += nn;

            nn = accept(TokenType.COMMA);  // Comma is optional after last element of list
            if (nn == 0) { break; }
            total_tokens += nn;
        }
        return total_tokens;
    }

    public int parse_zero_or_more_comma_separated_decimal_values() {
        // <zero_or_more_comma_separated_decimal_values> := <decimal_value> ',' <zero_or_more_comma_separated_decimal_values> | <decimal_value> ',' | <decimal_value> |
        int total_tokens = 0;
        while (true) {
            int nn = accept(TokenType.DECIMAL_VALUE);  // List might have zero elements
            if (nn == 0) { break; }

            decimal dd = tokens[tmp.token_index].extract_bare_decimal();
            //Form1.stdout.print("p: list element decimal {0}\n", dd);
            tmp.list_decimal.Add(dd);
            total_tokens += nn;

            nn = accept(TokenType.COMMA);  // Comma is optional after last element of list
            if (nn == 0) { break; }
            total_tokens += nn;
        }
        return total_tokens;
    }

    public int parse_zero_or_more_comma_separated_ID_values() {
        // <zero_or_more_comma_separated_ID_values> := <ID_value> ',' <zero_or_more_comma_separated_ID_values> | <ID_value> ',' | <ID_value> |
        int total_tokens = 0;
        while (true) {
            int nn = parse_ID_value();  // List might have zero elements
            if (nn == 0) { break; }

            int ii = tmp.int_val;  // Was set by parse_ID_value()
            //Form1.stdout.print("p: list element ID {0}\n", ii);
            tmp.list_int.Add(ii);
            total_tokens += nn;

            nn = accept(TokenType.COMMA);  // Comma is optional after last element of list
            if (nn == 0) { break; }
            total_tokens += nn;
        }
        return total_tokens;
    }

    /********************/

    public int parse_field_name() {
        // <field_name> := <bare_multi_word>
        int nn = accept(TokenType.BARE_MULTI_WORD);
        if (nn > 0) {
            tmp.field_name = tokens[tmp.token_index].text;
        }
        return nn;
    }

} // partial class Script_Parser
