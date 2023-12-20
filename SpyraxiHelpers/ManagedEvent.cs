using System;
using CLSS;
using System.Collections.Generic;

namespace SpyraxiHelpers
{
    public sealed class ManagedEvent
    {
        /// <summary>
        /// Return true to skip the game's default functionality for the event,
        /// return false otherwise. Even if the game's default functionality is
        /// skipped, the event's listeners will still be called when the event
        /// is invoked.
        /// </summary>
        // public HashSet<Func<bool>> ConditionalSkips { get; } = new();

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
        // public bool ShouldSkip()
        // {
        //     if (ConditionalSkips.Count == 0)
        //     {
        //         return false;
        //     }
        //     foreach (var condition in ConditionalSkips)
        //     {
        //         if (condition())
        //         {
        //             return true;
        //         }
        //     }
        //     return false;
        // }
    }
    
    public sealed class ManagedEvent<T>
    {
        /// <summary>
        /// Return true to skip the game's default functionality for the event,
        /// return false otherwise. Even if the game's default functionality is
        /// skipped, the event's listeners will still be called when the event
        /// is invoked.
        /// </summary>
        // public HashSet<Func<T, bool>> ConditionalSkips { get; } = new();

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
        // public bool ShouldSkip(T @object)
        // {
        //     if (ConditionalSkips.Count == 0)
        //     {
        //         return false;
        //     }
        //     foreach (var condition in ConditionalSkips)
        //     {
        //         if (condition(@object))
        //         {
        //             return true;
        //         }
        //     }
        //     return false;
        // }
    }
}
