using System;
using System.Collections.Generic;

namespace Framework
{
    public class TrackingAttribute : Attribute
    {

    }

    public interface ITrackable
    {

        TrackDictionary<string, bool> ModifiedProperties { get; set; }
 
       
    }

    public class TrackDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public event Action<string> PropertyChanged;

        public new TValue this[TKey key]
        {
            get => base[key];
            set
            {
               
                base[key] = value;
                PropertyChanged?.Invoke(key.ToString());
            }
        }

        public new void Add(TKey key, TValue value)
        {
            base.Add(key, value);
            PropertyChanged?.Invoke(key.ToString());
        }

        
    }

  
}