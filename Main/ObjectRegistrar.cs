using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ObjectRegistrar {
    // The global registrars:
    public  static ObjectRegistrar All;
    public  static ObjectRegistrar Sprites;
    public  static ObjectRegistrar TileSheets;
    // ...more such would be added, per each type we want to handily retrieve a list of instances of...

    private static int highest_ID { get; set; }

    Type registered_type;
    public  string tag_prefix { get; private set; }  // Used to compose autotag strings such as "SPR-12345", in the parser code
    public  int    num_objs   { get; private set; }  // TODO: change this to get (collection).Length for (All, TileSheets, Sprites, ...)

    Dictionary<int,    IHaximaSerializeable> objs_by_ID;
    Dictionary<string, int>                  IDs_by_tag;  // The key is the autotag, and also (if existing) the tag

    // TODO: 
    // It occurs to me that more tidying could be done.
    // This class could be changed to have 
    //     static (highest_id, objs_by_ID, IDs_by_tag)
    // and
    //     (some collection such as Dictionary<int,int> storing ID => ID, for each specific type)
    // 
    // With suitable changes to object_for_ID(), register_object_as() and so forth,
    // this scheme might be tidier.  In particular, it would simplify the case analysis in register_object_as().
    // 
    // On the other hand, the current form works (and has gotten rid of objs_by_ID as needless).

    static ObjectRegistrar() {
        // Static class constructor:
        highest_ID = 0;
        All        = new ObjectRegistrar(typeof(object),      "ALL");  // No need for prefix on this one
        TileSheets = new ObjectRegistrar(typeof(TileSheet),   "TileSheet");
        Sprites    = new ObjectRegistrar(typeof(ITileSprite), "Sprite");
        // ...more such would be added, per each type we want to handily retrieve a list of instances of...
    } // Initialize()

    public ObjectRegistrar(Type type_arg, string prefix_arg) {
        registered_type = type_arg;
        tag_prefix      = prefix_arg;
        num_objs        = 0;
        objs_by_ID      = new Dictionary<int, IHaximaSerializeable>();
        IDs_by_tag      = new Dictionary<string, int>();
    } // ObjectRegistrar()

    /*****************************************/

    public bool is_valid_or_zero_ID(int id) {
        if (id == 0)                   { return true; }
        if (object_for_ID(id) != null) { return true; }
        return false;
    }

    public bool is_valid_ID(int id) {
        if (object_for_ID(id) != null) { return true; }
        return false;
    }

    /*****************************************/

    public int ID_for_object(IHaximaSerializeable obj) {
        // This method is redundant, (normally one just says 'obj.ID')
        // It exists for the sake of symmetry, I suppose.
        if (obj == null) { return 0; }
        int ID = obj.ID;
        return ID;
    } // ID_for_obj()

    public object object_for_ID(int ID) {
        IHaximaSerializeable obj = null;
        if (ID == 0) { return obj; }  // The special ID 0 never refers to a valid object of any kind
        bool found = objs_by_ID.TryGetValue(ID, out obj);
        //if (!found) {
        //    Console.WriteLine("ObjectRegistrar.obj_for_ID() ID '{}' not found\n", ID);
        //}
        return obj;
    } // obj_for_ID()

    public int ID_for_tag(string tag) {
        // If the tag matches either the autotag or the manual tag of a registered object, we return the ID
        if (tag        == null) { return 0; }
        if (tag.Length == 0)    { return 0; }
        int ID = 0;
        bool found = IDs_by_tag.TryGetValue(tag, out ID);
        //if (!found) {
        //    Console.WriteLine("ObjectRegistrar.ID_for_tag() tag '{0}' not found\n", tag);
        //}
        return ID;
    } // ID_for_tag()

    /*****************************************/

    public int register_object_as(IHaximaSerializeable obj, Type tt, int new_ID) {
        // In the constructor of any IHaximaSerializeable,
        // a call must be made to this method, to register the new object
        // into 'All' and into the class-specific registrar.
        // 
        // This scheme may be extended at some point to allow adding 
        // more ObjRegistrar instances at runtime (when some script syntax specifies thus),
        // indicating that all Obj of some Archetype should be 
        // indexed in such an ObjRegistrar.
        // 
        // To do so would probably require re-arranging the current 
        // named static ObjRegistrar instances into a Dictionary<string, ObjRegistrar>
        // so that all such could be accessed in the same way.
        if (obj == null ||
            tt != registered_type) {
            throw new ArgumentException("Got incorrect object type");
        }
        int ID;
        if (new_ID == 0) {
            // Passing a zero ID means to auto-assign a new one.
            // 
            // This is the case for all new objects created at run-time, 
            // as well as some new objects hand-edited in the script.
            ID = ++highest_ID;
        }
        else {
            // Passing a non-zero ID means to use the given ID.
            // 
            // We must check for a collision, and perhaps update the highest_ID 
            // to prevent a future collision on auto-assign.
            // 
            // This is the case for previously-saved objects (which serialized their ID into the script),
            // as well as some new objects hand-edited in the script,
            // or in the case or hand-editing re-ordering of object constructors.

            if (ObjectRegistrar.All.objs_by_ID.ContainsKey(new_ID)) {
                // There is a collision with an already-assigned ID.
                // This can happen if a hand-edited script contains an object constructor call which specifies an already-used ID.
                // 
                // TODO: Less-lethal error handling, or a more precise diagnostic (cooperating with the parser)...
                Error.BadArg("register_obj() Got an already-in-use ID: {0}", new_ID);
            }
            ID = new_ID;
            if (ID >= highest_ID) {
                highest_ID = ID;
            }
        }
        // At this point, ID is definitely non-zero
        if (tt != typeof(object)) {
            // Registration in a type-specific registrar also causes registration in 'All' as object.
            ObjectRegistrar.All.register_object_as(obj, typeof(object), ID);
        }
        this.objs_by_ID.Add(ID, obj);
        this.num_objs++;
        //Console.WriteLine("registered 0x{0:X} as ID {1}, now {2} objects registered", obj.GetHashCode(), ID, num_objs);
        return ID;
    } // register_object_as()

    public void unregister_object(IHaximaSerializeable obj) {
        // NOTE: There are no callers ATM, as test/demo code has not exercised object cleanup yet.
        // 
        // Any object which we wish to dispose of needs to have unregister_object() called on it first,
        // otherwise the references in the ObjectRegistrar would keep the garbage collector from reclaiming it.
        // 
        // Among other callers would be code for
        // teardown and cleanup/reset of the "session" / "world state".
        if (obj == null) { Error.BadArg("Got null obj"); }  // Or perhaps just return?
        int ID = obj.ID;
        if (ID == 0) {
            Error.BadArg("Called for not-previously-registered object");
        }
        objs_by_ID.Remove(ID);
        num_objs--;
        //Console.WriteLine("un-registered 0x{0:X} from ID {1}, now {2} objects registered", obj.GetHashCode(), ID, num_objs);
    } // unregister_object()

    public void register_tag(string tag, int ID) {
        if (IDs_by_tag.ContainsKey(tag)) {
            Error.BadArg("register_tag() called for already-present tag '{0}'.", tag);
        }
        IDs_by_tag[tag] = ID;
    } // register_tag()

    public void unregister_tag(string tag, int ID) {
        // Called from some method in an IHaximaSerializeable when a tag is changed / unset.
        // Uses two parameters so that doing an unregister for a tag that does not belong to that object is hard to do accidentally.
        int registered_ID;
        if (!IDs_by_tag.TryGetValue(tag, out registered_ID)) {
            Error.BadArg("unregister_tag() called for not-registered tag '{0}'.", tag);
        }
        if (ID != registered_ID) {
            Error.BadArg("unregister_obj() called for wrong object?  tag='{0}' ID={1}, but found ID={2}", tag, ID, registered_ID);
        }
        IDs_by_tag.Remove(tag);
    } // unregister_tag()

    /*****************************************/

    // TODO:
    // There remains a block of methods in HRAOS_Parser ObjRegistrar.cs 
    // which will need to be brought over when the parser is merged.
    // 
    // Those methods pertain to the Archetype class,
    // and similar methods will likely be defined for other kernal primitive types (Obj, TileSheet, TileSprite, ...)
    // 
    // The methods in question are:
    // public int ID_for_Archetype(Archetype arch) {                 // This has no callers ATM
    // public Archetype Archetype_for_ID(int ID) {                   // One caller: in Script_Parser_OBJ.cs, parse_OBJ_parent_archetype_field()
    // public Archetype Archetype_for_tag(string tag)                // Commented out, under review as to whether it should exist
    // public void register_Archetype(Archetype arch, int new_ID) {  // This has no callers ATM
    // public void unregister_Archetype(Archetype arch) {            // This has no callers ATM

} // class ObjectRegistrar
