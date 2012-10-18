using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO: This placeholder class will go away, in favor of the Obj class from HRAOS_Parser ...
    public abstract class HaxObj : IHaximaSerializeable
    {
        public int    ID      { get; set; }
        public string autotag { get; set; }
        public string tag     { get; set; }

        //public void register() {
        //    this.ID = ObjectRegistrar.All.register_obj(this);
        //}
        //public void unregister() {
        //    ObjectRegistrar.All.unregister_obj(this);
        //}

    } // class HaxObj


//// The 'Terrain' type will be implemented via Archetype + Obj, accessible to the script.
//// Thus, there will be no 'Terrain' class
//    public class Terrain : HaxObj
//    {
//        public int sprite_ID { get; set; }
//        public string name   { get; set; }
//    } // class Terrain
