using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TrackChangePropertyLib;

namespace AssemblyToProcess
{

    
  
 public class ModelClass3:TrackingBase
    {
       public string Text { get; set; }

        public DateTime? date { get; set; }
    }


   
    public class Class2 : TrackingBase
    {
       
        public int? Prop2 { get; set; }
        public string Prop3 { get; set; }



        public ObservableList<ModelClass3> lst2 {get; set; } = new ObservableList<ModelClass3>() { new ModelClass3() };

        public ObservableList<string> lst3 { get; set; } = new ObservableList<string>();

    }
     

   
  
}
