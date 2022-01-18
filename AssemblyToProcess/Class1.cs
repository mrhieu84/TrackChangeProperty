using System;
using System.Collections.Generic;
using Framework;
using TestBaseClassLib;

namespace AssemblyToProcess
{
  
    public class Class2 : Class1
    {
        public string Prop2 { get; set; }
    }
    public class Class1 : BaseClass
    {
        public string Prop1 { get; set; }
    }

    [Tracking]
    public abstract class BaseClass
    {
        public event Action<string> PropertyChanged;
        public BaseClass()
        {
            ModifiedProperties.PropertyChanged += name => PropertyChanged?.Invoke(name);
        }
        public string PropBase { get; set; }
        public virtual TrackDictionary<string, bool> ModifiedProperties { get; set; } = new TrackDictionary<string, bool>();


    }


}
