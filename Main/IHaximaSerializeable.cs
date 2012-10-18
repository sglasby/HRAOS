public interface IHaximaSerializeable {
    int    ID      { get; set; }  // A positive integer, 0 is special non-valid value
    string autotag { get;      }  // Of the form "prefix-ID", such as "SPR-12345"
    string tag     { get; set; }  // Either null, or of a form which cannot collide with any autotag
} // interface IHaximaSerializeable