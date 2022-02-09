using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace TranscriptionCore
{
    public class UndoableCollection<T> : IList<T>
    {
        public bool Revert(Undo act)
        {
            if (act is UCChanged c)
            {
                switch (c)
                {
                    case Replaced r:
                        this[r.Index] = r.Old;
                        return true;
                    case Inserted a:
                        RemoveAt(a.Index);
                        return true;
                    case Removed rm:
                        Insert(rm.Index, rm.Old);
                        return true;
                }
            }

            return false;
        }

        public record UCChanged : Undo;

        public record Replaced(int Index, T Old) : UCChanged;

        public record Inserted(int Index) : UCChanged;

        public record Removed(int Index, T Old) : UCChanged;

        readonly List<T> children = new List<T>();
        public UndoableCollection()
        {
        }

        public T this[int index]
        {
            get
            {
                return children[index];
            }

            set
            {
                if (children.Count > index)
                {
                    var old = children[index];
                    children[index] = value;
                    OnRemoved?.Invoke(old);
                    OnAdd?.Invoke(value, index);
                    Update.OnContentChanged(new Replaced(index, old));
                }
                else if (children.Count == index)
                {
                    Add(value);
                }
            }

        }

        public void Add(T data)
        {
            children.Add(data);
            OnAdd?.Invoke(data, children.Count - 1);
            Update.OnContentChanged(new Inserted(children.Count));
        }

        public void Insert(int index, T data)
        {
            if (index < 0 || index > children.Count)
                throw new IndexOutOfRangeException();

            children.Insert(index, data);
            OnAdd?.Invoke(data, index);
            Update.OnContentChanged(new Inserted(index));
        }


        public void RemoveAt(int index)
        {
            if (index < 0 || index >= children.Count)
                throw new IndexOutOfRangeException();

            var old = children[index];
            children.RemoveAt(index);
            OnRemoved?.Invoke(old);
            Update.OnContentChanged(new Removed(index, old));
        }


        public bool Remove(T value)
        {
            var indx = children.IndexOf(value);
            if (indx >= 0)
            {
                RemoveAt(indx);
                return true;
            }
            return false;
        }

        public UpdateTracker Update { get; } = new UpdateTracker();

        public int Count => children.Count;

        public bool IsReadOnly => false;

        public int IndexOf(T item)
            => children.IndexOf(item);


        public void Clear()
        {
            Update.BeginUpdate();
            while (Count > 0)
                RemoveAt(0);

            children.Clear();
            Update.EndUpdate();
        }

        public bool Contains(T item)
            => children.Contains(item);

        public void CopyTo(T[] array, int arrayIndex)
            => children.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator()
            => children.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public Action<T, int>? OnAdd { get; set; }
        public Action<T>? OnRemoved { get; set; }
    }

}
