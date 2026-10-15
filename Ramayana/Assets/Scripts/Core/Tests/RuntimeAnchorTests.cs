// EditMode tests for the runtime anchor contract: provide, unset, and stale-provider protection.
using System.Text.RegularExpressions;
using JourneyToLanka.Core.Anchors;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JourneyToLanka.Core.Tests
{
    public sealed class RuntimeAnchorTests
    {
        private TransformAnchorSO _anchor;
        private GameObject _first;
        private GameObject _second;

        [SetUp]
        public void SetUp()
        {
            _anchor = ScriptableObject.CreateInstance<TransformAnchorSO>();
            _first = new GameObject("First");
            _second = new GameObject("Second");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_anchor);
            Object.DestroyImmediate(_first);
            Object.DestroyImmediate(_second);
        }

        [Test]
        public void Provide_ThenUnset_ClearsValue()
        {
            _anchor.Provide(_first.transform);
            Assert.AreSame(_first.transform, _anchor.Value);

            _anchor.Unset(_first.transform);
            Assert.IsFalse(_anchor.IsSet);
        }

        [Test]
        public void Unset_ByStaleProvider_KeepsCurrentValue()
        {
            _anchor.Provide(_first.transform);
            LogAssert.Expect(LogType.Warning, new Regex("replaced"));
            _anchor.Provide(_second.transform);

            _anchor.Unset(_first.transform);

            Assert.AreSame(_second.transform, _anchor.Value);
        }

        [Test]
        public void TryGetValue_WhenUnset_ReturnsFalse()
        {
            bool found = _anchor.TryGetValue(out Transform value);

            Assert.IsFalse(found);
            Assert.IsNull(value);
        }
    }
}
