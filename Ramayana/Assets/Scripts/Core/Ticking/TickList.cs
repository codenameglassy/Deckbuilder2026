// Ordered, allocation-free list of tick receivers. Invokes in registration order. Adds during a run start next run,
// removes during a run take effect immediately. One throwing receiver is logged and does not stop the others.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace JourneyToLanka.Core.Ticking
{
    public sealed class TickList<T> where T : class
    {
        private static readonly Predicate<T> s_isNull = IsNull;

        private readonly List<T> _items = new List<T>(64);
        private readonly List<T> _pendingAdds = new List<T>(16);
        private bool _isRunning;
        private bool _hasRemovals;

        public bool Add(T item)
        {
            if (item == null || _items.Contains(item) || _pendingAdds.Contains(item))
            {
                return false;
            }

            if (_isRunning)
            {
                _pendingAdds.Add(item);
            }
            else
            {
                _items.Add(item);
            }

            return true;
        }

        public bool Remove(T item)
        {
            if (item == null)
            {
                return false;
            }

            if (_pendingAdds.Remove(item))
            {
                return true;
            }

            int index = _items.IndexOf(item);
            if (index < 0)
            {
                return false;
            }

            if (_isRunning)
            {
                _items[index] = null;
                _hasRemovals = true;
            }
            else
            {
                _items.RemoveAt(index);
            }

            return true;
        }

        public void Run(Action<T, float> invoke, float deltaTime)
        {
            _isRunning = true;
            try
            {
                int count = _items.Count;
                for (int i = 0; i < count; i++)
                {
                    T item = _items[i];
                    if (item == null)
                    {
                        continue;
                    }

                    try
                    {
                        invoke(item, deltaTime);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
            }
            finally
            {
                _isRunning = false;
                ApplyPendingChanges();
            }
        }

        private void ApplyPendingChanges()
        {
            if (_hasRemovals)
            {
                _items.RemoveAll(s_isNull);
                _hasRemovals = false;
            }

            if (_pendingAdds.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _pendingAdds.Count; i++)
            {
                _items.Add(_pendingAdds[i]);
            }

            _pendingAdds.Clear();
        }

        private static bool IsNull(T item)
        {
            return item == null;
        }
    }
}
