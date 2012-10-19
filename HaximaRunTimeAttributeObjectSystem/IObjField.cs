using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public interface IObjField {
    FieldType type { get; }

    // For each class which implements IObjFieldValue:
    // - One of the methods below implements an ordinary get + set
    // And all the others:
    // - throw an exception for get + set
    // 
    // If something with Perl-like semantics were wanted, could instead:
    // - implement Perl-like type conversions (we're not doing this, it's not what we want)
    int     iv { get; set; }
    string  sv { get; set; }
    decimal dv { get; set; }

    IList<int>     ilist { get; set; }
    IList<string>  slist { get; set; }
    IList<decimal> dlist { get; set; }

} // interface IObjField


    // The scheme for the various "storage types" and "semantic types" used is that:
    // - For each storage type, there are one or more semantic types which use such storage.
    //   Currently, the types are (int, string, decimal) and lists of those types.
    // 
    // - For each semantic type, there is a class implementing IObjField.
    //   These shall be named like Field<semantic_type_name>, such as FieldInt 
    //   or FieldList<semantic_type_name>, such as FieldListInt.
    // 
    // - Some of the semantic types (those which do not represent "bare" storage types)
    //   are implemented as subclasses of the bare/basic types.
    //   For example, the semantic type ID is stored with storage type INT, 
    //   and FieldID is a subclass of FieldInt.
    // 
    // - The sub-classed types may implement additional validity or range-checking logic.
    //   For example, the ID and LIST_ID types check for validity of their value(s) as "object IDs".
    //   In future, new INT-based semantic types might check against a minimum and maximum value, 
    //   or have a non-zero default field value.
