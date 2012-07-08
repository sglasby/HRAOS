using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WinForms_display_bitmap
{
    public class ObjectRegistrar
    {
        // The global registrars:
        public static ObjectRegistrar HaxObjs;
        public static ObjectRegistrar Sprites;

        Type registered_type;
        public  string tag_prefix { get; private set; }
        public  int    num_objs   { get; private set; }
        private int    highest_ID { get; set; }
        Dictionary<int, object> objs_by_ID;
        Dictionary<object, int> IDs_by_obj;

        static ObjectRegistrar()
        {
            // Static class constructor:
            HaxObjs = new ObjectRegistrar(typeof(Object),     "Obj");
            Sprites = new ObjectRegistrar(typeof(TileSprite), "Spr");
        } // Initialize()

        public ObjectRegistrar(Type type_arg, string prefix_arg)
        {
            registered_type = type_arg;
            tag_prefix      = prefix_arg;
            num_objs        = 0;
            highest_ID      = 0;
            objs_by_ID      = new Dictionary<int, object>();
            IDs_by_obj      = new Dictionary<object, int>();
        } // ObjectRegistrar()

        public int ID_for_obj(object obj)
        {
            // This method is redundant, since registered objects will have an ID() method, 
            // but we define it for the sake of completeness.
            int ID = 0;
            if (obj == null ||
                obj.GetType() != registered_type)
            {
                throw new ArgumentException("Got incorrect object type");
            }
            bool found = IDs_by_obj.TryGetValue(obj, out ID);
            if (!found)
            {
                //Console.WriteLine("ObjectRegistrar.ID_for_obj() obj '{}' not found\n", obj);
            }
            return ID;
        } // ID_for_obj()

        public object obj_for_ID(int ID)
        {
            object obj = null;
            bool found = objs_by_ID.TryGetValue(ID, out obj);
            if (!found)
            {
                //Console.WriteLine("ObjectRegistrar.obj_for_ID() ID '{}' not found\n", ID);
            }
            return obj;
        } // obj_for_ID()

        public int register_obj(object obj)
        {
            if (obj == null ||
                obj.GetType() != registered_type)
            {
                throw new ArgumentException("Got incorrect object type");
            }
            int ID = ++highest_ID;
            objs_by_ID.Add(ID, obj);
            IDs_by_obj.Add(obj, ID);
            num_objs++;
            //Console.WriteLine("registered 0x{0:X} as ID {1}, now {2} objects registered", obj.GetHashCode(), ID, num_objs);
            return ID;
        } // register_obj()

        public void unregister_obj(object obj)
        {
            if (obj == null ||
                obj.GetType() != registered_type)
            {
                throw new ArgumentException("Got incorrect object type");
            }
            int ID = ID_for_obj(obj);
            if (ID == 0)
            {
                throw new ArgumentException("Called for not-previously-registered object");
            }
            objs_by_ID.Remove(ID);
            IDs_by_obj.Remove(obj);
            num_objs--;
            //Console.WriteLine("un-registered 0x{0:X} from ID {1}, now {2} objects registered", obj.GetHashCode(), ID, num_objs);
        } // unregister_obj()

        public interface IHaximaSerializeable
        {
            // Which style is more proper for an interface???
            //int    ID();   // A positive integer, 0 is special non-valid value
            //string tag();  // Of the form "prefix-ID", such as "SPR-12345"
            int    ID  { get; }
            string tag { get; }
        } // interface IHaximaSerializeable




    } // class ObjectRegistrar

} // namespace WinForms_display_bitmap
