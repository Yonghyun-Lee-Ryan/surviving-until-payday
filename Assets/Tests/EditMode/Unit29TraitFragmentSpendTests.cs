using NUnit.Framework;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Save;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class Unit29TraitFragmentSpendTests
    {
        [Test]
        public void TryUnlockTraitWithFragments_SpendsAndUnlocks()
        {
            var trait = ScriptableObject.CreateInstance<TraitData>();
            trait.EditorSet("trait_healthy_test", "체력왕", "desc", 2);

            var meta = new MetaProgressionManager();
            meta.Load(0, null, null, null, null);
            meta.AddTraitFragments(5);

            Assert.IsFalse(meta.IsTraitUnlocked(trait));
            Assert.IsTrue(meta.TryUnlockTraitWithFragments(trait, out var reason));
            Assert.IsTrue(string.IsNullOrEmpty(reason));
            Assert.IsTrue(meta.IsTraitUnlocked(trait));
            Assert.AreEqual(5 - MetaProgressionManager.TraitUnlockFragmentCost, meta.TraitFragmentCount);
            Assert.IsTrue(meta.Traits.IsUnlocked(trait.Id));

            Object.DestroyImmediate(trait);
        }

        [Test]
        public void TryUnlockTraitWithFragments_FailsWhenNotEnough()
        {
            var trait = ScriptableObject.CreateInstance<TraitData>();
            trait.EditorSet("trait_positive_test", "긍정왕", "desc", 3);

            var meta = new MetaProgressionManager();
            meta.Load(0, null, null, null, null);
            meta.AddTraitFragments(2);

            Assert.IsFalse(meta.TryUnlockTraitWithFragments(trait, out var reason));
            Assert.IsTrue(reason.Contains("부족"));
            Assert.AreEqual(2, meta.TraitFragmentCount);
            Assert.IsFalse(meta.IsTraitUnlocked(trait));

            Object.DestroyImmediate(trait);
        }

        [Test]
        public void TryUnlockTraitWithFragments_FailsWhenAlreadyUnlockedByLevel()
        {
            var trait = ScriptableObject.CreateInstance<TraitData>();
            trait.EditorSet("trait_overtime_test", "야근전문가", "desc", 2);

            var meta = new MetaProgressionManager();
            // Level 2 = 200 XP
            meta.Load(200, null, null, null, null);
            meta.AddTraitFragments(10);

            Assert.IsTrue(meta.IsTraitUnlocked(trait));
            Assert.IsFalse(meta.TryUnlockTraitWithFragments(trait, out var reason));
            Assert.IsTrue(reason.Contains("이미"));
            Assert.AreEqual(10, meta.TraitFragmentCount);

            Object.DestroyImmediate(trait);
        }

        [Test]
        public void FragmentUnlock_PersistsInMetaSave()
        {
            var trait = ScriptableObject.CreateInstance<TraitData>();
            trait.EditorSet("trait_persist_test", "테스트", "desc", 4);

            var meta = new MetaProgressionManager();
            meta.Load(0, null, null, null, null);
            meta.AddTraitFragments(3);
            Assert.IsTrue(meta.TryUnlockTraitWithFragments(trait, out _));

            var captured = SaveMapper.CaptureMeta(meta);
            var loaded = new MetaProgressionManager();
            SaveMapper.ApplyMeta(captured, loaded);

            Assert.AreEqual(0, loaded.TraitFragmentCount);
            Assert.IsTrue(loaded.Traits.IsUnlocked(trait.Id));
            Assert.IsTrue(loaded.IsTraitUnlocked(trait));

            Object.DestroyImmediate(trait);
        }
    }
}
