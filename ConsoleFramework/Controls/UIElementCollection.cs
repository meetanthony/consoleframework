using System;
using System.Collections;
using System.Collections.Generic;
using ConsoleFramework.Binding.Observables;

namespace ConsoleFramework.Controls;

public partial class Control
{
    public delegate void ControlAddedEventHandler(Control control);

    public delegate void ControlRemovedEventHandler(Control control);

    public class UiElementCollection : IList
    {
        private readonly IList _list;
        private readonly Control _parent;

        public event ControlAddedEventHandler? ControlAdded;
        public event ControlAddedEventHandler? ControlRemoved;

        private void OnControlAdded(Control control)
        {
            if (ControlAdded == null)
                return;
            ControlAdded(control);
        }
        
        private void OnControlRemoved(Control control)
        {
            if (ControlRemoved == null)
                return;
            ControlRemoved(control);
        }

        public UiElementCollection(Control parent)
        {
            _parent = parent;
            ObservableList<Control> observableList = new ObservableList<Control>(new List<Control>());
            _list = observableList;
            observableList.ListChanged += OnListChanged;
        }

        private void OnListChanged(object sender, ListChangedEventArgs args)
        {
            switch (args.Type)
            {
                case ListChangedEventType.ItemsInserted:
                {
                    for (int i = 0; i < args.Count; i++)
                    {
                        if (_list[args.Index + i] is Control control)
                        {
                            _parent.InsertChildAt(args.Index + i, control);
                            OnControlAdded(control);
                        }
                    }

                    break;
                }
                case ListChangedEventType.ItemsRemoved:
                    for (int i = 0; i < args.Count; i++)
                    {
                        Control control = _parent.Children[args.Index];
                        _parent.RemoveChild(control);
                        OnControlRemoved(control);
                    }

                    break;
                case ListChangedEventType.ItemReplaced:
                {
                    var removedControl = _parent.Children[args.Index];
                    _parent.RemoveChild(removedControl);
                    OnControlRemoved(removedControl);

                    if (_list[args.Index] is Control addedControl)
                    {
                        _parent.InsertChildAt(args.Index, addedControl);
                        OnControlAdded(addedControl);
                    }
                    break;
                }
            }
        }

        public IEnumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            _list.CopyTo(array, index);
        }

        public int Count => _list.Count;

        public object SyncRoot => _list.SyncRoot;

        public bool IsSynchronized => _list.IsSynchronized;

        public int Add(object? value)
        {
            return _list.Add(value);
        }

        public bool Contains(object? value)
        {
            return _list.Contains(value);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public int IndexOf(object? value)
        {
            return _list.IndexOf(value);
        }

        public void Insert(int index, object? value)
        {
            _list.Insert(index, value);
        }

        public void Remove(object? value)
        {
            _list.Remove(value);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        public object? this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        public bool IsReadOnly => _list.IsReadOnly;

        public bool IsFixedSize => _list.IsFixedSize;
    }
}