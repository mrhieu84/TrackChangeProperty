
# TrackChangeProperty


## This is an add-in for [Fody]
 Fast tracking changes on an object. Include Tracking collection changes.

TrackChangeProperty will Process any class inherit from TrackingBase.

All trackable  will be inject to implement `ITrackable` iterface , 
# Installation

```powershell
PM> Install-Package TrackChangeProperty.Fody
```
# Add to FodyWeavers.xml
```
Add <TrackChangeProperty/> to FodyWeavers.xml

<Weavers>
  <TrackChangeProperty/>
</Weavers>
```
# How to use

**Note: `All classes must inherit from TrackingBase`. Using built-in ObservableList  to track changes collection.**
```csharp


public class ModelClass2:TrackingBase
    {
       public string Text { get; set; }

        public DateTime? Date { get; set; }
    }


    public class ModelClass1 : TrackingBase
    {
        public int? Prop1 { get; set; }
        public string Prop2 { get; set; }

        public ObservableList<ModelClass2> CollectionTracked { get; set; } //classes inherit from Trackingbase

        public ObservableList<string> CollectionTracked_2 { get; set; }  // valua types: string, int, float,...

       

    }
```
**Any changes of properties in ModelClass1  will trigger the PropertyChange event**

```csharp
var obj = new ModelClass1();
           

            obj.PropertyChange += (s, e) =>
            {
                Console.WriteLine(e.PropertyName + " changed");

                //Prop1 changed
                //CollectionTracked changed
                //CollectionTracked changed
                //CollectionTracked changed
		
		
		//CollectionTracked_2 changed
		//CollectionTracked_2 changed
		//CollectionTracked_2 changed
            };

            obj.Prop1 = 9;
            obj.CollectionTracked = new ObservableList<ModelClass2>();
            obj.CollectionTracked.Add(new ModelClass2());
            obj.CollectionTracked[0].Text = "abc";
		
	    obj.CollectionTracked_2 = new ObservableList<string>();
	    obj.CollectionTracked_2 .Add("abc");
	   obj.CollectionTracked_2.Removet(0);
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
# What gets compiled

```csharp
public class ModelClass1 : TrackingBase, ITrackable
{
    public int? Prop1
    {
        [CompilerGenerated]
        get
        {
            return this.<Prop1>k__BackingField;
        }
        [CompilerGenerated]
        set
        {
           if (this.<Prop1>k__BackingField != value)
	    {
		this.<Prop1>k__BackingField = value;
		base.ModifiedProperties["Prop1"] = true;
	    }

        }
    }

     public string Prop2
    {
        [CompilerGenerated]
        get
        {
            return this.<Prop2>k__BackingField;
        }
        [CompilerGenerated]
        set
        {
           if (this.<Prop2>k__BackingField != value)
	    {
		this.<Prop2>k__BackingField = value;
		base.ModifiedProperties["Prop2"] = true;
	    }

        }
    }

   
	
    
    public ObservableList<ModelClass2> CollectionTracked
    {
    	 [CompilerGenerated]
        get
        {
            if (this.<CollectionTracked>k__BackingField > null)
            {
                object[] args = new object[] { this, "CollectionTracked" };
                this.<CollectionTracked>k__BackingField.GetType().InvokeMember("OnParentCallPropertyGet", BindingFlags.InvokeMethod, null, this.<CollectionTracked>k__BackingField, args);
            }
            return this.<CollectionTracked>k__BackingField;
        }
	 [CompilerGenerated]
        set
        {
             ObservableList<Item> target = this.<CollectionTracked>k__BackingField;
	    if (this.<CollectionTracked>k__BackingField != value)
	    {
		this.<CollectionTracked>k__BackingField = value;
		base.ModifiedProperties["CollectionTracked"] = true;
		if (value != null)
		{
		    object[] args = new object[] { this, target, "CollectionTracked" };
		    value.GetType().InvokeMember("OnParentCallPropertySet", BindingFlags.InvokeMethod, null, value, args);
		}
		else
		{
		    object[] args = new object[] { this, "CollectionTracked" };
		    target.GetType().InvokeMember("OnParentSetNull", BindingFlags.InvokeMethod, null, target, args);
		}
	    }

        }
    }


    public ObservableList<string> CollectionTracked_2
    {
    	 [CompilerGenerated]
        get
        {
            if (this.<CollectionTracked_2>k__BackingField > null)
            {
                object[] args = new object[] { this, "CollectionTracked_2" };
                this.<CollectionTracked_2>k__BackingField.GetType().InvokeMember("OnParentCallPropertyGet", BindingFlags.InvokeMethod, null, this.<CollectionTracked_2>k__BackingField, args);
            }
            return this.<CollectionTracked_2>k__BackingField;
        }
	 [CompilerGenerated]
        set
        {
           ObservableList<Item> target = this.<CollectionTracked_2>k__BackingField;
	    if (this.<CollectionTracked_2>k__BackingField != value)
	    {
		this.<CollectionTracked_2>k__BackingField = value;
		base.ModifiedProperties["CollectionTracked_2"] = true;
		if (value != null)
		{
		    object[] args = new object[] { this, target, "CollectionTracked_2" };
		    value.GetType().InvokeMember("OnParentCallPropertySet", BindingFlags.InvokeMethod, null, value, args);
		}
		else
		{
		    object[] args = new object[] { this, "CollectionTracked_2" };
		    target.GetType().InvokeMember("OnParentSetNull", BindingFlags.InvokeMethod, null, target, args);
		}
	    }

        }



   
}

 

```
