using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TrackChangePropertyLib
{
    public class TrackingAttribute : Attribute
    {
    }

    public interface ITrackable
    {
        TrackDictionary<string, bool> ModifiedProperties { get; set; }
    }

    public interface IObservableBase
    {
         ITrackable Parent { get; set; }
        string PropertyName { get; set; }
    }
    public abstract class ObservableBase: IObservableBase
    {
        private object syncobject = new object();
        public ITrackable Parent { get; set; }

        public string PropertyName { get; set; }
       
        public virtual void OnParentCallPropertyGet(object parentObject, string propertyname)
        {
            if (Parent == null)
            {
                lock (syncobject)
                {
                   
                    PropertyName = propertyname;
                    Parent = (ITrackable)parentObject;
                }
            }


        }
        public virtual void OnParentCallPropertySet(object parentObject, object prevObject, string propertyname)
        {
            lock (syncobject)
            {
                if (prevObject != null)
                {
                    ((IObservableBase ) prevObject).Parent = null;
                    ((IObservableBase)prevObject).PropertyName = null;

                }
                PropertyName = propertyname;
                Parent = (ITrackable)parentObject;
            }
        }

    }
    public  class ObservableList<T> : ObservableBase, IList<T>
    {
        private List<T> internalList;
        public class ItemChangedEventArgs : EventArgs
        {
            //  public int index;
            public T item;
            public ItemChangedEventArgs(T item)
            {
                //  this.index = index;
                this.item = item;
            }
        }

        public delegate void ItemAddedEventHandler(object source, ItemChangedEventArgs e);
        public delegate void ItemChangedEventHandler(object source, ItemChangedEventArgs e);
        public delegate void ItemRemovedEventHandler(object source, ItemChangedEventArgs e);
        public delegate void ListChangedEventHandler(object source, EventArgs e);

        public event ListChangedEventHandler ListChanged;
        public event ItemRemovedEventHandler ItemRemoved;
        public event ItemAddedEventHandler ItemAdded;
        public event ItemChangedEventHandler ItemChanged;


        public ObservableList()
        {
            internalList = new List<T>();
            IsTrackingBaseType = typeof(T).IsSubclassOf(typeof(TrackingBase));
        }


        private bool IsTrackingBaseType;

        public  ObservableList(List<T> list)
        {

            internalList = list;
            IsTrackingBaseType = typeof(T).IsSubclassOf(typeof(TrackingBase));
            if (IsTrackingBaseType)
            {
                foreach (var w in internalList)
                {
                    (w as TrackingBase).PropertyChange -= ModelChange;
                    (w as TrackingBase).PropertyChange += ModelChange;
                }

            }

        }


        protected virtual void OnItemAdded(ItemChangedEventArgs e)
        {
            if (IsTrackingBaseType)
            {
                (e.item as TrackingBase).PropertyChange -= ModelChange;
                (e.item as TrackingBase).PropertyChange += ModelChange;
            }
            if (ItemAdded != null)
                ItemAdded(this, e);

        }
        protected virtual void OnItemChanged(ItemChangedEventArgs e)
        {
            if (ItemChanged != null)
                ItemChanged(this, e);
        }


        protected virtual void OnItemRemoved(ItemChangedEventArgs e)
        {
            if (IsTrackingBaseType)
            {
                (e.item as TrackingBase).PropertyChange -= ModelChange;
            }
            if (ItemRemoved != null)
                ItemRemoved(this, e);
        }


        protected virtual void OnListChanged(EventArgs e)
        {
            if (PropertyName!=null)
            this.Parent.ModifiedProperties[PropertyName] = true;

            if (ListChanged != null)
                ListChanged(this, e);
        }

        public int IndexOf(T item)
        {
            return internalList.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            internalList.Insert(index, item);
            OnItemAdded(new ItemChangedEventArgs(item));
            OnListChanged(EventArgs.Empty);

        }

        void ModelChange(object s, EventArgs e)
        {

            OnItemChanged(new ItemChangedEventArgs((T)s));
            OnListChanged(EventArgs.Empty);
        }

        public void RemoveAt(int index)
        {
            T item = internalList[index];
            internalList.Remove(item);

            OnItemRemoved(new ItemChangedEventArgs(item));
            OnListChanged(EventArgs.Empty);
        }

        public T this[int index]
        {
            get
            {
                return internalList[index];
            }
            set
            {
                var prev = internalList[index];
                internalList[index] = value;
                if (!prev.Equals(value))
                {

                    OnItemRemoved(new ItemChangedEventArgs(prev));
                    OnItemAdded(new ItemChangedEventArgs(value));

                    OnListChanged(EventArgs.Empty);
                }

            }
        }

        public void Add(T item)
        {
            internalList.Add(item);
            OnItemAdded(new ItemChangedEventArgs(item));
            OnListChanged(new PropertyChangedArgs { PropertyName = PropertyName });

        }

        public void AddRange(IEnumerable<T> list)
        {
            if (list == null) return;

            foreach (var w in list)
            {

                internalList.Add(w);
                OnItemAdded(new ItemChangedEventArgs(w));

            }

            OnListChanged(EventArgs.Empty);

        }
        public void Clear()
        {

            foreach (var w in internalList)
            {

                OnItemRemoved(new ItemChangedEventArgs(w));

            }

            internalList.Clear();
            OnListChanged(EventArgs.Empty);
        }

        public bool Contains(T item)
        {
            return internalList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return internalList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            lock (this)
            {
                int index = internalList.IndexOf(item);
                if (internalList.Remove(item))
                {

                    OnItemRemoved(new ItemChangedEventArgs(item));
                    OnListChanged(EventArgs.Empty);
                    return true;
                }
                else
                    return false;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)internalList).GetEnumerator();
        }
    }

  
    public class PropertyChangedArgs: EventArgs
    {
        public string PropertyName { get; set; }
    }
    public class TrackDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public event EventHandler<PropertyChangedArgs> PropertyChanged;

        public new TValue this[TKey key]
        {
            get => base[key];
            set
            {
               
                base[key] = value;
                PropertyChanged?.Invoke(this,  new PropertyChangedArgs { PropertyName=key.ToString() });
            }
        }

        //public new void Add(TKey key, TValue value)
        //{
        //    base.Add(key, value);
        //    PropertyChanged?.Invoke(this, null);
        //}

        public bool IsEventHandlerRegistered(Delegate prospectiveHandler)
        {
            if (this.PropertyChanged != null)
            {
                foreach (Delegate existingHandler in this.PropertyChanged.GetInvocationList())
                {
                    if (existingHandler == prospectiveHandler)
                    {
                        return true;
                    }
                }
            }
            return false;
        }



    }

    

    [Tracking]
    public abstract class TrackingBase: ObservableBase
    {
        public virtual TrackDictionary<string, bool> ModifiedProperties { get; set; } = new TrackDictionary<string, bool>();

        public event EventHandler<PropertyChangedArgs> PropertyChange;

        public TrackingBase()
        {
            this.ModifiedProperties.PropertyChanged +=  (s,e)  =>
            {
                if (this.PropertyName != null)
                    this.Parent.ModifiedProperties[PropertyName] = true;

                PropertyChange?.Invoke(this, e);
            };

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


}