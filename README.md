
# TrackChangeProperty


## This is an add-in for [Fody]
A modified version of TrackChange (https://www.nuget.org/packages/TrackChange.Fody), allow tracking derived classes and raises the PropertyChanged event

TrackChangeProperty will Process any POCO class has  `TrackingAttribute`. 

All trackable POCOs will be inject to implement `ITrackable` iterface.
# Installation

```powershell
PM> Install-Package TrackChangeProperty.Fody
```

# copy the code into your project
```csharp
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

```

### Your code

```csharp
[Tracking] 
public class ClassBase
{
    public DateTime? Prop1 { get; set; }

}

public class DerivedClass1: ClassBase
{
   
    public string Test2 { get; set; }
}


```

### What gets compiled
```csharp
[Tracking]
public class DerivedClass1 : ITrackable
{
    public DateTime? Prop1
    {
        [CompilerGenerated]
        get
        {
            return this.<Prop1>k__BackingField;
        }
        [CompilerGenerated]
        set
        {
            bool flag = object.Equals(this.Prop1, value);
            bool flag2 = !flag;
            if (flag2)
            {
                this.ModifiedProperties["Prop1"] = true;
            }
            this.<Prop1>k__BackingField = value;
        }
    }

    public string Test2
    {
        [CompilerGenerated]
        get
        {
            return this.<Test2>k__BackingField;
        }
        [CompilerGenerated]
        set
        {
            bool flag = object.Equals(this.Test2, value);
            bool flag2 = !flag;
            if (flag2)
            {
                this.ModifiedProperties["Test2"] = true;
            }
            this.<Test2>k__BackingField = value;
        }
    }

    [NonSerialized]
    public virtual TrackDictionary<string, bool> ModifiedProperties { get; set; } = new TrackDictionary<string, bool>();
}

```
### A simple implementation of TrackingBase 
```csharp
[Tracking]
public class TrackingBase
{
    public DateTime? Prop1 { get; set; }
   public virtual TrackDictionary<string, bool> ModifiedProperties { get; set; }

     public event EventHandler PropertyChanged;

       

       public TrackingBase (){
              this.ModifiedProperties.PropertyChanged += new Action<string>(name =>
                {

                    OnPropertyChanged(EventArgs.Empty);
                });
       }
         protected virtual void OnPropertyChanged(EventArgs e)
        {

            PropertyChanged?.Invoke(this, e);
        }
        
                
        public bool IsDirty => ModifiedProperties.Count > 0;
        public void ClearDirty()
        {
            ModifiedProperties.Clear();
        }

        public void MakeDirty(string name)
        {
            ModifiedProperties[name] = true;

        }

        public bool CheckAnyChange(params string[] propertiesname)
        {
            if (propertiesname != null)
            {
                return propertiesname.Any(a => ModifiedProperties.ContainsKey(a));
            }
            return false;
        }
        
       

    
}

public class DerivedClass1: TrackingBase
{
   
    public string Test2 { get; set; }
}

var obj= new DerivedClass1();
//raise event on Properties Change
  obj.PropertyChanged   += (s,e)  =>
    {
            
    });
    
   // check any changes
   var HasChanges = obj.IsDirty;
   
   //reset changes
   obj.ClearDirty();
   //add custom change 
   obj.MakeDirty("mycustomname");
   
//check property change
bool isChange= obj.CheckAnyChange("mycustomname", "Test2");

```
