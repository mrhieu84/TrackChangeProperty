using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TrackChangePropertyLib;


namespace AssemblyToProcess
{
   
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

    public class Class3 : TrackingBase
    {

        public int? Prop5 { get; set; }

        public Item Item { get; set; }


    }


   



}
