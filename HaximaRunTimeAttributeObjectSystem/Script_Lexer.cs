using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

public enum TokenType {
    UNKNOWN = 0,

    ARCHETYPE,
    OBJ,
    // Other "element" keywords to follow...
    TAG,
    // Other non-element keywords to follow...

    L_PAREN,
    R_PAREN,

    L_CURLY,
    R_CURLY,

    L_BRACKET,
    R_BRACKET,

    COMMA,
    ARROW_COMMA,
    EQUAL_SIGN,

    INT,
    STRING,
    DECIMAL,
    ID,
    // Other non-fundamental scalar types to follow...
    LIST_INT,
    LIST_STRING,
    LIST_DECIMAL,
    LIST_ID,
    // Other non-fundamental list types to follow...

    INT_VALUE,
    STRING_VALUE,
    DECIMAL_VALUE,

    BARE_MULTI_WORD,

    //HASH_COMMENT_TO_END_OF_LINE,
    //SLASH_COMMENT_TO_END_OF_LINE,

    // SYNTHETIC_BEGIN_STREAM,  // ...Proposed synthetic token to prepend to the beginning of the token stream...
    // SYNTHETIC_END_STREAM,    // ...Proposed synthetic token to append to the end of the token stream...
} // enum TokenType



public class Token {
    public TokenType type    { get; private set; }
    public string text       { get; set; }
    public int line_number   { get; set; }
    public int column_number { get; set; }

    public Token(string ss, int line_num, int col_num) {
        text          = ss;
        line_number   = line_num;
        column_number = col_num;
        type          = type_for_text(text);
    } // Token()


    // This regex works to split tokens for the current grammer.  Will need changes if certain parse_elements change / are added.
    // REMINDER: 
    //     Keep the (punctuation-token bit) at the front in sync 
    //     with the (not-these-punctuations-in-bare-multi-word bit) at the end:
    public static Regex lexer_regex = new Regex(@"(,|\(|\)|\[|\]|\{|\}|[\=\>]+|'[^\']*'|""[^""]*""|[^\s^,^\(^\)^\[^\]^\{^\}^\=^\>]+)");
    // Maybe TODO: Capture comments as tokens:  @"\#.*$|//.*$|"
    // 
    // TODO: Needs some bullet-proofing
    //       Might be sensible to rewrite in a regex-with-internal-comments form...yes indeed...
    // 
    // Known failure modes include:  <-- should be fixed now?
    // - ARCHETYPE a1 ID=1{ }    // no space before left curly
    // - ARCHETYPE a2 ID = 2 {}  // no space between curlys
    // 
    // Suspected failure modes:
    // - Handling of comments; has not been thoroughly tested
    // - Lack of whitespace around ARROW_COMMA such as 'f1=>INT,42'

    // TODO: Eventually support for meta-characters (for escaping quotes) in strings, mainly \' \" \\
    // TODO: How about support for hexadecimal for INT, and scientific notation for DECIMAL?

    public static Regex int_value       = new Regex(@"^[+-]?\d+$");
    public static Regex string_value    = new Regex(@"^([""\'])[^""]*\1$");  // A string inside either '' or "" (no meta-character support, so no escaping ' or ")
    public static Regex decimal_value   = new Regex(@"^[+-]?\d*\.?\d+M$");   // TODO: How about odd forms such as '1.M' and the like?
    public static Regex bare_multi_word = new Regex(@"^([a-zA-Z]\w*)([-](\w+))*$");
    public static Regex auto_tag        = new Regex(@"^([a-zA-Z]\w*)-(\d+)$");  // Examples: ARCH-123 OBJ-456 SPR-789

    public static bool is_bare_multi_word(string name) {
        return Token.bare_multi_word.IsMatch(name);
    } // is_bare_multi_word()

    public static bool is_valid_auto_tag(string name) {
        return Token.auto_tag.IsMatch(name);
    } // is_valid_auto_tag()


    public int extract_int() {
        int ii = Int32.Parse(text);
        return ii;
    }

    public string extract_bare_string() {
        // Extract and return the bare string within '' or ""
        int len = text.Length;
        if (len <= 2) { return String.Empty; }
        return text.Substring(1, len - 2);
    }

    public string extract_bare_multi_word() {
        return text;
    }

    public decimal extract_bare_decimal() {
        int len = text.Length;
        if (len <= 1) { return 0.0M; }  // Or throw an exception???
        string ss = text.Substring(0, len - 1);
        return Decimal.Parse(ss);
    }

    public TokenType type_for_text(string tt) {
        // Would a table-based approach be nicer?
        if (tt.ToUpper() == "ARCHETYPE") { return TokenType.ARCHETYPE; }
        if (tt.ToUpper() == "OBJ") { return TokenType.OBJ; }
        // ...more "element" keywords to come...

        if (tt.ToUpper() == "TAG") { return TokenType.TAG; }
        // ...more non-element keywords to come...

        if (tt == "(") { return TokenType.L_PAREN; }
        if (tt == ")") { return TokenType.R_PAREN; }
        if (tt == "{") { return TokenType.L_CURLY; }
        if (tt == "}") { return TokenType.R_CURLY; }
        if (tt == "[") { return TokenType.L_BRACKET; }
        if (tt == "]") { return TokenType.R_BRACKET; }

        if (tt == ",")  { return TokenType.COMMA; }
        if (tt == "=>") { return TokenType.ARROW_COMMA; }
        if (tt == "=")  { return TokenType.EQUAL_SIGN; }

        if (tt.ToUpper() == "INT")     { return TokenType.INT; }
        if (tt.ToUpper() == "STRING")  { return TokenType.STRING; }
        if (tt.ToUpper() == "DECIMAL") { return TokenType.DECIMAL; }
        if (tt.ToUpper() == "ID")      { return TokenType.ID; }

        if (tt.ToUpper() == "LIST_INT")     { return TokenType.LIST_INT; }
        if (tt.ToUpper() == "LIST_STRING")  { return TokenType.LIST_STRING; }
        if (tt.ToUpper() == "LIST_DECIMAL") { return TokenType.LIST_DECIMAL; }
        if (tt.ToUpper() == "LIST_ID")      { return TokenType.LIST_ID; }

        if (int_value.Match(tt).Success)     { return TokenType.INT_VALUE; }
        if (string_value.Match(tt).Success)  { return TokenType.STRING_VALUE; }
        if (decimal_value.Match(tt).Success) { return TokenType.DECIMAL_VALUE; }

        if (bare_multi_word.Match(tt).Success) { return TokenType.BARE_MULTI_WORD; }

        // if (tt.Substring(0,1) == "#" ) { return TokenType.HASH_COMMENT_TO_END_OF_LINE;  }
        // if (tt.Substring(0,2) == "//") { return TokenType.SLASH_COMMENT_TO_END_OF_LINE; }

        return TokenType.UNKNOWN;
    } // TokenType()

} // class Token
