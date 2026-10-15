// EditMode tests for ComponentPool: activation, instance reuse, prewarm, re-parenting on return, destroying
// returns beyond the size limit, double-return detection and disposal. The pooled type is Transform because
// this test assembly is Editor-only, and Unity cannot attach a MonoBehaviour defined in an Editor assembly.
// The IPoolable callbacks are covered in Play mode by the pooled card views.
using System;
using System.Collections.Generic;
using JourneyToLanka.Core.Pooling;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JourneyToLanka.Core.Tests
{
    public sealed class ComponentPoolTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();
        private Transform _prefab;
        private Transform _root;

        [SetUp]
        public void SetUp()
        {
            _prefab = Track(new GameObject("PoolPrefab")).transform;
            _root = Track(new GameObject("PoolRoot")).transform;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }

            _created.Clear();
        }

        [Test]
        public void Get_CreatesAnActiveInstanceUnderTheRoot()
        {
            var pool = new ComponentPool<Transform>(_prefab, _root);

            Transform item = pool.Get();

            Assert.AreNotSame(_prefab, item);
            Assert.IsTrue(item.gameObject.activeSelf);
            Assert.AreSame(_root, item.parent);
            Assert.AreEqual(1, pool.CountActive);
            Assert.AreEqual(0, pool.CountInactive);
        }

        [Test]
        public void Release_DeactivatesTheInstance_ThenItIsReused()
        {
            var pool = new ComponentPool<Transform>(_prefab, _root);
            Transform first = pool.Get();

            pool.Release(first);

            Assert.IsFalse(first.gameObject.activeSelf);
            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(1, pool.CountInactive);

            Transform second = pool.Get();
            Assert.AreSame(first, second);
            Assert.IsTrue(second.gameObject.activeSelf);
        }

        [Test]
        public void Prewarm_CreatesInactiveInstancesUpToTheCount()
        {
            var pool = new ComponentPool<Transform>(_prefab, _root);

            pool.Prewarm(3);
            pool.Prewarm(2);

            Assert.AreEqual(3, pool.CountInactive);
            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(3, _root.childCount);
            for (int i = 0; i < _root.childCount; i++)
            {
                Assert.IsFalse(_root.GetChild(i).gameObject.activeSelf);
            }
        }

        [Test]
        public void Release_MovesAReparentedInstanceBackUnderTheRoot()
        {
            var pool = new ComponentPool<Transform>(_prefab, _root);
            Transform elsewhere = Track(new GameObject("Elsewhere")).transform;
            Transform item = pool.Get();
            item.SetParent(elsewhere, false);

            pool.Release(item);

            Assert.AreSame(_root, item.parent);
        }

        [Test]
        public void Release_BeyondMaxSize_DestroysTheExtraInstance()
        {
            var pool = new ComponentPool<Transform>(_prefab, _root, 1, 1);
            Transform kept = pool.Get();
            Transform extra = pool.Get();

            pool.Release(kept);
            pool.Release(extra);

            Assert.AreEqual(1, pool.CountInactive);
            Assert.IsTrue(kept != null);
            Assert.IsTrue(extra == null);
        }

        [Test]
        public void Release_Twice_Throws()
        {
            var pool = new ComponentPool<Transform>(_prefab, _root);
            Transform item = pool.Get();
            pool.Release(item);

            Assert.Throws<InvalidOperationException>(() => pool.Release(item));
        }

        [Test]
        public void Dispose_DestroysPooledInstances()
        {
            var pool = new ComponentPool<Transform>(_prefab, _root);
            pool.Prewarm(2);

            pool.Dispose();

            Assert.AreEqual(0, _root.childCount);
            Assert.AreEqual(0, pool.CountInactive);
        }

        private GameObject Track(GameObject gameObject)
        {
            _created.Add(gameObject);
            return gameObject;
        }
    }
}
