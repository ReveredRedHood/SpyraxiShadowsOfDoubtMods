using System;
using CLSS;
using System.Collections.Generic;

namespace DeTESTive
{
    public sealed class ManagedEvent
    {
        private readonly HashSet<Action> _listeners = new();
        public void AddListener(Action action)
        {
            _listeners.Add(action);
        }
        public void RemoveListener(Action action)
        {
            _listeners.Remove(action);
        }
        public void ClearListeners()
        {
            _listeners.Clear();
        }
        public void Invoke()
        {
            _listeners.ForEach(l => l?.Invoke());
        }
    }
    
    public sealed class ManagedEvent<T>
    {
        private readonly HashSet<Action<T>> _listeners = new();
        public void AddListener(Action<T> action)
        {
            _listeners.Add(action);
        }
        public void RemoveListener(Action<T> action)
        {
            _listeners.Remove(action);
        }
        public void ClearListeners()
        {
            _listeners.Clear();
        }
        public void Invoke(T @object)
        {
            _listeners.ForEach(l => l?.Invoke(@object));
        }
    }
}
