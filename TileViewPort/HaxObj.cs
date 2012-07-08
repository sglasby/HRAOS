using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WinForms_display_bitmap
{
    public abstract class HaxObj : ObjectRegistrar.IHaximaSerializeable
    {
        public int ID { get; private set; }
        public string tag
        {
            get
            {
                return String.Format("{0}-{1}", ObjectRegistrar.HaxObjs.tag_prefix, ID);
            }
        } // tag()

    } // class HaxObj


    public class Terrain : HaxObj
    {
        public int sprite_ID { get; set; }
        public string name   { get; set; }
    } // class Terrain

} // namespace WinForms_display_bitmap
