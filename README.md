
# TrackChangeProperty


## This is an add-in for [Fody]
 Fast tracking changes on an object. Include Tracking collection changes.

TrackChangeProperty will Process any POCO class inherit from TrackingBase.

All trackable POCOs will be inject to implement `ITrackable` iterface , 
# Installation

```powershell
PM> Install-Package TrackChangeProperty.Fody
```

# How to use
```
public class ModelClass2:TrackingBase
    {
       public string Text { get; set; }

        public DateTime? Date { get; set; }
    }


    public class ModelClass1 : TrackingBase
    {
        public int? Prop1 { get; set; }
        public string Prop2 { get; set; }

        public ObservableList<ModelClass2> CollectionTracked { get; set; }

        public ObservableList<string> CollectionTracked_2 { get; set; }

       

    }


var obj = new ModelClass1();
           

            obj.PropertyChange += (s, e) =>
            {
                Console.WriteLine(e.PropertyName + " changed");

                //Prop1 changed
                //CollectionTracked changed
                //CollectionTracked changed
                //CollectionTracked changed
            };

            obj.Prop1 = 9;
            obj.CollectionTracked = new ObservableList<ModelClass2>();
            obj.CollectionTracked.Add(new ModelClass2());
            obj.CollectionTracked[0].Text = "abc";

            //reset dirty
            obj.ClearDirty();
           
           if (obj.IsDirty)
            {
                //has any changes
            }

           if (obj.CheckAnyChange("CollectionTracked"))
            {
                //Property CollectionTracked changed
            }


	

```
#What gets compiled

```
public class ModelClass1 : TrackingBase, ITrackable
{
    // Properties
    public int? Prop1 { get; set; }

    public string Prop2 { get; set; }

    public ObservableList<ModelClass2> CollectionTracked
    {
        get
        {
            if (this.<CollectionTracked>k__BackingField > null)
            {
                object[] args = new object[] { this, "CollectionTracked" };
                this.<CollectionTracked>k__BackingField.GetType().InvokeMember("OnParentCallPropertyGet", BindingFlags.InvokeMethod, null, this.<CollectionTracked>k__BackingField, args);
            }
            return this.<CollectionTracked>k__BackingField;
        }
        set
        {
            if (this.<CollectionTracked>k__BackingField != value)
            {
                base.ModifiedProperties["CollectionTracked"] = true;
                if (value > null)
                {
                    object[] args = new object[] { this, "CollectionTracked" };
                    value.GetType().InvokeMember("OnParentCallPropertySet", BindingFlags.InvokeMethod, null, value, args);
                }
            }
            this.<CollectionTracked>k__BackingField = value;
        }
    }

    public ObservableList<string> CollectionTracked_2
    {
        get
        {
            if (this.<CollectionTracked_2>k__BackingField > null)
            {
                object[] args = new object[] { this, "CollectionTracked_2" };
                this.<CollectionTracked_2>k__BackingField.GetType().InvokeMember("OnParentCallPropertyGet", BindingFlags.InvokeMethod, null, this.<CollectionTracked_2>k__BackingField, args);
            }
            return this.<CollectionTracked_2>k__BackingField;
        }
        set
        {
            if (this.<CollectionTracked_2>k__BackingField != value)
            {
                base.ModifiedProperties["CollectionTracked_2"] = true;
                if (value > null)
                {
                    object[] args = new object[] { this, "CollectionTracked_2" };
                    value.GetType().InvokeMember("OnParentCallPropertySet", BindingFlags.InvokeMethod, null, value, args);
                }
            }
            this.<CollectionTracked_2>k__BackingField = value;
        }
    }

   
}

 

```