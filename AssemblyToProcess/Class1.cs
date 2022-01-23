using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TrackChangePropertyLib;

namespace AssemblyToProcess
{
    //public class testclass
    //{
    //    public Dictionary<string, object> nnn = new Dictionary<string, object>();
    //    private string _xxx;
    //    public string xxx
    //    {
    //        get
    //        {
    //            return _xxx;
    //        }
    //        set
    //        {
    //            if (this._xxx !=value)
    //            {
    //                this._xxx = value;
    //                nnn["xxx"] = true;

    //                if ( (value != null))
    //                {
    //                    object[] args = new object[] { this, "lst2" };
    //                    value.GetType().InvokeMember("OnParentCallPropertySet", BindingFlags.InvokeMethod, null, value, args);
                       
    //                }
    //            }
               
    //        }
    //    }
    //}
    public class Item:TrackingBase
    {
        public string Name { get; set; }
    }
    public class Class2 : TrackingBase
    {

        public int? Prop2 { get; set; }
        public string Prop3 { get; set; }

        public Item Item { get; set; } = new Item();

        public ObservableList<Item> lst2 { get; set; } = new ObservableList<Item>() {new Item() };

     
    }


  
     

   
  
}
