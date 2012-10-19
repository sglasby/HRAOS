using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

public partial class Script_Parser {
    // TODO: 
    // Perhaps split this up into 2 + N seperate files,
    // (Script_Parser.top.cs, Script_Parser.terminals.cs, and Script_Parser.<ELEMENT_NAME_HERE>.cs)
    // the better to avoid clutter / disorganization...

    public string filename    { get; private set; }
    private Stream fstream    { get; set;         }
    public string contents    { get; private set; }
    public List<Token> tokens { get; private set; }
    public int pos            { get; private set; }  // Current position in the tokens stream; advanced by accept() and expect()
    public int num_lines      { get; private set; }
    public int num_tokens     { get; private set; }

    public enum Entity_Type {
        // TODO: Will this be suitable, or is there some "better" approach ?
        UNKNOWN = 0,
        ARCHETYPE,
        OBJ,
        // More to come...
    }

    private struct parse_info_holder {
        // TODO:
        // Perhaps split this into multiple structs, each holding exactly + only 
        // the info for a particular <ELEMENT> type, such as ARCHETYPE or OBJ...
        // 
        // A holder of recently-parsed information, to facilitate passing that information 
        // between disparate levels of the method call stack so that object constructors 
        // (and such) can be called with the parsed information.

        public int         token_index;  // The index in the token stream of the last successfully-matched token seen by accept() or expect()

        // Fields for Archetype construction:
        public Archetype new_archetype;
        public string    archetype_tag;

        // Fields for Obj construction:
        public Obj       new_obj;
        public Archetype parent_archetype;
        public string    obj_tag;
        public int       ID;

        // Fields for Archetype/Obj field construction:
        public string    field_name;
        public FieldType type_of_field;
        public int       int_val;
        public string    string_val;
        public decimal   decimal_val;

        public List<int>     list_int;
        public List<string>  list_string;
        public List<decimal> list_decimal;

        public void clear_field_data() {
            // Currently unused, might still be useful to add for more bullet-proofing.
            // Clear certain members, so the next parsed element starts with a blank slate.
            // 
            // Strictly speaking, it is not _needful_ to clear this, but examining this struct 
            // when debugging will benefit from having (just-used AND no-longer-needed) fields cleared.
            // 
            // Some members are not cleared here, as an entity (such as Archetype or Obj) 
            // may have other fields remaining to parse.
            // Also, the token_index is kept as the value may be useful when debugging.
            // 
            // ...Another clear method to clear the higher-level information 
            // (excepting token_index, which is never cleared until a new parse) might also be useful...

            //new_archetype    = null;  // Keep this
            //archetype_tag    = null;  // Keep this
            // 
            //new_obj          = null;  // Keep this
            //parent_archetype = null;  // Keep this
            //obj_tag          = null;  // Keep this
            //ID               = 0;     // Keep this
            // 
            field_name    = null;
            type_of_field = null;
            int_val       = 0;
            string_val    = null;
            decimal_val   = 0;
            list_int      = null;
            list_string   = null;
            list_decimal  = null;
        }
    } // struct parse_info_holder

    parse_info_holder tmp;

    public Script_Parser(string fn) {
        if (fn == null) { Error.Throw("Got null filename"); }
        if (!System.IO.File.Exists(fn)) { Error.Throw("Filename '{0}' does not exist", fn); }
        // Strangely, testing if a file is readable seems awkward.  
        // Is the idiomatic practice actually to just try to open the file and complain when it fails ???

        filename = fn;
        tokens   = new List<Token>();
        tmp      = new parse_info_holder();
    } // Script_Parser()

    /*********************************************/

    public void parse() {
        read_and_lex_to_tokens();
        parse_hax_script();  // This is the "start symbol" of the grammar

    } // parse()

    public List<Token> AsTokens(string line, int line_num) {
        List<Token> result = new List<Token>();
        Match mm = Token.lexer_regex.Match(line);

        int line_element_num = 1;
        while (mm.Success) {
            Capture cap = mm.Groups[0].Captures[0];
            int pos = cap.Index;
            int length = cap.Length;

            if (cap.ToString().Substring(0, 1) == "#") {
                // A comment to end-of-line of the "#" style, skip the rest of the line
                break;
            }
            if ((cap.Length >= 2) && cap.ToString().Substring(0, 2) == "//") {
                // A comment to end-of-line of the "//" style, skip the rest of the line
                // TODO: 
                // Will want to store certain specially-prefixed comments, 
                // to emit later when serializing, thus preserving said comments across load+save ...
                //
                // An attempt at this is in Script_Lexer.cs, commented out.
                // A strategy for skipping past zero-or-more comment tokens at arbitrary places
                // in the grammar would need to be implemented, to use this approach...
                break;
            }
            //Form1.stdout.print("(Line {0,2}, Col {1,2}, Length {2,2}) parse_element {3} token='{4}'\n", line_num, pos, length, line_element_num, cap);
            result.Add(new Token(cap.ToString(), line_num, pos));

            line_element_num++;
            mm = mm.NextMatch();
        }
        return result;
    }

    public void read_and_lex_to_tokens() {
        // A) Strip comments of '#' or '//' type (not supporting /**/ type)
        // B) Keep track of line and column number of tokens
        // C) Return a list of tokens

        Form1.stdout.print("Lexing script file '{0}'...\n", filename);
        string[] lines = File.ReadAllLines(filename);

        // Scan for comment-to-end-of-line constructs '#' and '//', outside of strings:
        // Split the rest into tokens, store their line + column:
        int line_num = 1;
        foreach (string line in lines) {
            tokens.AddRange(AsTokens(line, line_num));
            line_num++;
        } // foreach(line)
        this.num_lines  = line_num;
        this.num_tokens = tokens.Count;

        Form1.stdout.print("Lexing complete, processed {0} lines with {1} tokens.\n", num_lines, num_tokens);

        //Form1.stdout.print("\nDebug output: Token stream:\n");
        //int ii = 1;
        //int prev_line = 0;
        //foreach (Token tt in tokens)
        //{
        //    Form1.stdout.print("{0,3}: (L={1,2},C={2,2}) {3,-18} '{4}'\n", ii, tt.line_number, tt.column_number, tt.type, tt.text);
        //    if (tt.line_number > prev_line)
        //    {
        //        prev_line = tt.line_number;
        //        Form1.stdout.print("\n");  // Blank line after the group of tokens comprising a given source line
        //    }
        //    ii++;
        //}
        //Form1.stdout.print("\n");

    } // read_and_lex_to_tokens()


    /*********************************************/


    void error(string format, params object[] args) {
        // TODO:
        // Perhaps a simpler means of updating the error handling would include
        // changing error() to log a warning, rather than throw an exception...
        // 
        // This would have to occur in combination with parser changes to match,
        // whether via back_up_parser() or otherwise...
        Error.Throw(format, args);
    }

    // TODO: 
    // In general, parse error/warnings should be much better than currently.
    // Some things which should be resolved:
    // - How to have warnings, rather than fatal errors (where does the output go -- presumably a file handle, and perhaps also notification in some GUI)
    // - What are best practices in error/warning messages?  Should report line and column number, for one thing...

    void warn_at() {
        // This is a start towards better diagnostics, but more can be done.  (Or less can be done; not-wordy is also valuable...)
        int line_number = tokens[pos].line_number;
        int col_number  = tokens[pos].column_number;
        string word     = tokens[pos].text;

        string obj_tag  = tmp.new_obj.tag;
        string autotag  = tmp.new_obj.autotag;
        string obj_name = (obj_tag != null) ? obj_tag : autotag;

        Form1.stdout.print("Warning: At line {0}, column {1}, on word '{2}'\n", line_number, col_number, word);
        Form1.stdout.print("         In OBJ declaration of '{0}'\n", obj_name);
    }

    void warn_add_OBJ_field_not_in_archetype(string field_name, string arch_tag) {
        // TODO: Policy control over this at some point; one place to change things.
        warn_at();
        Form1.stdout.print("         Archetype '{1}' does not define OBJ field '{0}'\n", field_name, arch_tag);
    }

    void warn_add_OBJ_field_defaulting_to_basic_field_type(string field_name, string arch_tag, FieldType type) {
        warn_at();
        Form1.stdout.print("         Archetype '{1}' does not define OBJ field '{0}', defaulting type to '{2}'\n",
                           field_name, arch_tag, type.ToString());
    }

    void warn_add_OBJ_field_mismatch_with_archetype_field(Obj obj, string field_name, FieldType obj_field_type) {
        warn_at();
        Form1.stdout.print("         Field type mismatch with Archetype '{1}', ignoring OBJ field '{0}',\n" +
                           "         obj_field_type '{2}' != arch_field_type '{3}'\n",
                            field_name, obj.archetype.tag, obj_field_type.ToString(), obj.archetype[field_name].type.ToString());
    }

    void print_debug_for_add_list_field(string field_name, FieldType type, List<int> list_int, List<string> list_string, List<decimal> list_decimal) {
        return;  // Comment out to re-enable debug output.  Goes well with the debug output in parse_OBJ_decl()

        Form1.stdout.print("    OBJ add_field(name={0}, type={1},[", field_name, type.ToString());
        if (list_int != null) { foreach (int     ii in list_int) { Form1.stdout.print("{0}, ", ii); } }
        if (list_string != null) { foreach (string  ss in list_string) { Form1.stdout.print("'{0}', ", ss); } }
        if (list_decimal != null) { foreach (decimal dd in list_decimal) { Form1.stdout.print("{0}M, ", dd); } }
        Form1.stdout.print("]\n");
    }



    int expect(TokenType tt) {
        // DEBUG: Uncomment this in expect() and accept() if the token count at end-of-parse ever gets out of sync:
        //if (num_tokens != pos) { Form1.stdout.print("expect: pos={0} token='{1}'\n", pos, tokens[pos].text); }

        if (pos >= tokens.Count) { return 0; }  // Is there a "better" way to determine end-of-input?
        // For instance, if an extra token of type END_OF_INPUT was appended to the end of the token stream?

        //Form1.stdout.print("expect() tokens[{0}] type={1} text='{2}' / expect={3}\n", pos, tokens[pos].type, tokens[pos].text, tt);
        // Try to find mandatory parse_element tt, producing an error if not found.
        if (tokens[pos].type == tt) {
            tmp.token_index = pos;  // Our caller can get the needed info, given this index
            pos++;
            return 1;
        }
        // Note: 
        // While the following debug line produces output when things are wrong, 
        // it also produces a bit of output when the parse tree halts down one branch, in normal operation.
        // So don't freak out if a few such messages appear.  (Comment it out for production use.)
        //Form1.stdout.print("Warning: expect() tokens[{0}] type={1} text='{2}' / expect={3}\n", pos, tokens[pos].type, tokens[pos].text, tt);
        return 0;
    } // expect()

    int accept(TokenType tt) {
        // DEBUG: Uncomment this in expect() and accept() if the token count at end-of-parse ever gets out of sync:
        //if (num_tokens != pos) { Form1.stdout.print("accept: pos={0} token='{1}'\n", pos, tokens[pos].text); }

        if (pos >= tokens.Count) { return 0; }  // Is there a "better" way to determine end-of-input?
        //Form1.stdout.print("accept() tokens[{0}] type={1} text='{2}' / accept={3}\n", pos, tokens[pos].type, tokens[pos].text, tt);

        // Try to find optional parse_element tt.
        if (tokens[pos].type == tt) {
            tmp.token_index = pos;  // Our caller can get the needed info, given this index
            pos++;
            return 1;
        }
        return 0;
    } // accept()

    void back_up_parser(int nn) {
        // Used to backtrack nn tokens
        // Use with discretion, beware shooting yourself in the foot iteratively...
        if (nn < 0) { error("Called with negative value"); }
        pos -= nn;
    }

    /*********************************************/


    public int parse_hax_script() {
        // This is the "start symbol" of the grammar:
        // <hax_script> := <elements>
        Form1.stdout.print("Parsing script...\n");
        this.pos = 0;
        int total_tokens_consumed = parse_elements();
        Form1.stdout.print("Script parsing complete, consumed {0} tokens.\n", total_tokens_consumed);
        if (this.num_tokens != total_tokens_consumed) {
            Form1.stdout.print("    Expected {0} tokens, parsed {1}, how did this happen?\n", num_tokens, total_tokens_consumed);
        }
        Form1.stdout.print("\n");
        return total_tokens_consumed;
    }

    public int parse_elements() {
        // <elements> := <element> <elements> |
        int nn = 0;
        while (true) {
            int tokens_consumed = parse_element();
            if (tokens_consumed == 0) { break; }
            nn += tokens_consumed;
        }
        return nn;
    }

    public int parse_element() {
        // <element>  := <archetype_decl> | <obj_decl>
        // ...More to come in future iterations of the grammar...
        int nn;

        nn = parse_ARCH_decl();

        if (nn == 0) {
            nn = parse_OBJ_decl();
        }

        // if (nn == 0) {
        //     nn = some_other_decl();
        // }
        // if (nn == 0) {
        //     nn = yet_another_decl();
        // }
        // ...

        return nn;
    }

    // Note:
    // The question arose, "how to hook up the parser skeleton to the various constructor calls, so that it does real work?".
    // The high-level constructs (such as ARCHETYPE) are at near-"top" levels of the call stack,
    // while the actual data we want to feed as constructor parameters are accessible at the very bottom (within accept() and expect()).
    // Further, some calls must be made after others, for instance in the case of ARCHETYPE
    // must call 'Archetype some_arch = new Archetype(name);', and then later, 'some_arch.add_field(field_name, type, <various_other_args>)'.
    // 
    // The possible means of getting the data from one place to another are:
    // - Option A: Via the return type.
    //             It would be needful for the various parsing methods to return something like KeyValuePair<int, some_list_or_struct>
    //             and to propagate the info all the way up the call chain of parse methods.  Yech.
    // - Option B: Via 'out' parameter(s).
    //             Likewise, needful to propagate the info up multiple levels.  Yech.
    // - Option C: Stow it someplace visible to any method whether "high" or "low" in the parse call chain.
    //             In this case, we stow it in the 'struct parse_info_holder' called 'Script_Parser.tmp'.
    // 
    // Option C was chosen, as the others seemed like the wrong thing.

} // partial class Script_Parser
