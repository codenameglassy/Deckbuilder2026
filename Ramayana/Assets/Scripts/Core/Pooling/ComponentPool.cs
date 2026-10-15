// Pool of prefab instances built on Unity's ObjectPool<T>, for anything that spawns repeatedly: card views,
// damage numbers, VFX, audio sources, list rows. Taking an instance activates it and calls IPoolable.OnTakenFromPool;
// returning it calls OnReturnedToPool, deactivates it and moves it back under the pool root if it was re-parented.
// Returns beyond maxSize destroy the extra instance. Prewarm creates instances up front without raising the
// IPoolable callbacks, so a fight never instantiates mid-play. Owners create one pool per prefab and Dispose it
// when they are destroyed. The double-return check runs only when collectionCheck is on (keep it on in development).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace JourneyToLanka.Core.Pooling
{
    public sealed class ComponentPool<T> : IDisposable where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _root;
        private readonly ObjectPool<T> _pool;
        private bool _prewarming;

        public ComponentPool(T prefab, Transform root, int defaultCapacity = 8, int maxSize = 256, bool collectionCheck = true)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            _prefab = prefab;
            _root = root;
            _pool = new ObjectPool<T>(Create, HandleGet, HandleRelease, HandleDestroy, collectionCheck, defaultCapacity, maxSize);
        }

        public int CountActive => _pool.CountActive;

        public int CountInactive => _pool.CountInactive;

        public T Get()
        {
            return _pool.Get();
        }

        public void Release(T item)
        {
            _pool.Release(item);
        }

        public void Prewarm(int count)
        {
            int missing = count - _pool.CountInactive;
            if (missing <= 0)
            {
                return;
            }

            List<T> created = ListPool<T>.Get();
            _prewarming = true;
            try
            {
                for (int i = 0; i < missing; i++)
                {
                    created.Add(_pool.Get());
                }

                for (int i = 0; i < created.Count; i++)
                {
                    _pool.Release(created[i]);
                }
            }
            finally
            {
                _prewarming = false;
                ListPool<T>.Release(created);
            }
        }

        public void Clear()
        {
            _pool.Clear();
        }

        public void Dispose()
        {
            _pool.Dispose();
        }

        private T Create()
        {
            T instance = Object.Instantiate(_prefab, _root);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void HandleGet(T item)
        {
            item.gameObject.SetActive(true);
            if (!_prewarming && item is IPoolable poolable)
            {
                poolable.OnTakenFromPool();
            }
        }

        private void HandleRelease(T item)
        {
            if (!_prewarming && item is IPoolable poolable)
            {
                poolable.OnReturnedToPool();
            }

            item.gameObject.SetActive(false);

            Transform itemTransform = item.transform;
            if (itemTransform.parent != _root)
            {
                itemTransform.SetParent(_root, false);
            }
        }

        private static void HandleDestroy(T item)
        {
            if (item == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(item.gameObject);
            }
            else
            {
                Object.DestroyImmediate(item.gameObject);
            }
        }
    }
}
