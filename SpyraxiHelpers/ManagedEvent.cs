using System;
using CLSS;
using System.Collections.Generic;

namespace SpyraxiHelpers
{
    public sealed class ManagedEvent
    {
        public HashSet<Action> Listeners { get; } = new();

        public void AddListener(Action action)
        {
            Listeners.Add(action);
        }
        public void RemoveListener(Action action)
        {
            Listeners.Remove(action);
        }
        public void ClearListeners()
        {
            Listeners.Clear();
        }
        public void Invoke()
        {
            Listeners.ForEach(l => l?.Invoke());
        }
    }
    
    public sealed class ManagedEvent<T>
    {
        public HashSet<Action<T>> Listeners { get; } = new();

        public void AddListener(Action<T> action)
        {
            Listeners.Add(action);
        }
        public void RemoveListener(Action<T> action)
        {
            Listeners.Remove(action);
        }
        public void ClearListeners()
        {
            Listeners.Clear();
        }
        public void Invoke(T @object)
        {
            Listeners.ForEach(l => l?.Invoke(@object));
        }
    }
}
