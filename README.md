
# TrackChangeProperty


## This is an add-in for [Fody]
A modified version of TrackChange (https://www.nuget.org/packages/TrackChange.Fody), tracking derived classes and raises the PropertyChanged event

TrackChangeProperty will Process any POCO class has  `TrackingAttribute`. 

All trackable POCOs will be inject to implement `ITrackable` iterface , you can copy follow `ITrackable` iterface file in your project
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
### event PropertyChanged
```csharp
[Tracking]
public class TrackingBase
{
    public DateTime? Prop1 { get; set; }
   public virtual TrackDictionary<string, bool> ModifiedProperties { get; set; }

     public event EventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(EventArgs e)
        {

            PropertyChanged?.Invoke(this, e);
        }

       public TrackingBase (){
              this.ModifiedProperties.PropertyChanged += new Action<string>(name =>
                {

                    OnPropertyChanged(EventArgs.Empty);
                });
       }

    
}

public class DerivedClass1: TrackingBase
{
   
    public string Test2 { get; set; }
}

var obj= new DerivedClass1();
  obj.PropertyChanged   += (s,e)  =>
    {
            
    });

```
