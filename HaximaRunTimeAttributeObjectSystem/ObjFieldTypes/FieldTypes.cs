using System;
using System.Text;
using System.Collections.Generic;

public enum StorageTypes {
    INT = 0,
    STRING,
    DECIMAL,

    LIST_INT,
    LIST_STRING,
    LIST_DECIMAL,
}

public enum SemanticTypes {
    // TODO: Will desire in future to have per-semantic-type validation, parse/serialize code, min/max values, etc...
    //       The code for such will live in some combination between (here, Archetype, or ArchetypeNamedField)...

    EMPTY = 0,  // Special type to handle setting a new field without manually calling obj.add_field() first
    INT,
    STRING,
    DECIMAL,
    ID,  // Holds an Object ID

    LIST_INT,
    LIST_STRING,
    LIST_DECIMAL,
    LIST_ID,

} // enum SemanticTypes

public class FieldType {
    public string        keyword       { get; set; }
    public string        sigil         { get; set; }
    public SemanticTypes semantic_type { get; set; }
    public Type          storage_type  { get; set; }

    public FieldType(string _keyword_, string _sigil_, SemanticTypes _type_, Type _storage_) {
        keyword       = _keyword_;
        sigil         = _sigil_;    // TODO: Is this going to be used?  One possibility is to refactor so that the parser auto-identifies field type by sigil-as-fieldname-prefix...
        semantic_type = _type_;
        storage_type  = _storage_;
    } // FieldType()

    public static readonly FieldType EMPTY        = new FieldType("NULL",    "", SemanticTypes.EMPTY, typeof(void));

    public static readonly FieldType INT          = new FieldType("INT",     "#", SemanticTypes.INT,     typeof(int));
    public static readonly FieldType STRING       = new FieldType("STRING",  "$", SemanticTypes.STRING,  typeof(string));
    public static readonly FieldType DECIMAL      = new FieldType("DECIMAL", "%", SemanticTypes.DECIMAL, typeof(decimal));
    public static readonly FieldType ID           = new FieldType("ID",      "&", SemanticTypes.ID,      typeof(int));

    public static readonly FieldType LIST_INT     = new FieldType("LIST_INT",     "@#", SemanticTypes.LIST_INT,     typeof(List<int>)     );
    public static readonly FieldType LIST_STRING  = new FieldType("LIST_STRING",  "@$", SemanticTypes.LIST_STRING,  typeof(List<string>)  );
    public static readonly FieldType LIST_DECIMAL = new FieldType("LIST_DECIMAL", "@%", SemanticTypes.LIST_DECIMAL, typeof(List<decimal>) );
    public static readonly FieldType LIST_ID      = new FieldType("LIST_ID",      "@&", SemanticTypes.LIST_ID,      typeof(List<int>)     );

    public override string ToString() {
        return String.Format("{0}", keyword);
    }

} // class FieldType
