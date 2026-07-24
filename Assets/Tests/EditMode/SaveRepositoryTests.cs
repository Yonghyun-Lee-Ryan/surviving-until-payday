using NUnit.Framework;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Events;
using SurviveUntilPayday.Save;
using UnityEngine;

namespace SurviveUntilPayday.Tests
{
    public sealed class SaveRepositoryTests
    {
        [Test]
        public void SaveAndLoad_RoundTripsRunAndMeta()
        {
            var memory = new InMemorySaveService();
            var repo = new SaveRepository(memory);

            var save = SaveRepository.CreateDefault();
            save.run.hasActiveRun = true;
            save.run.currentDay = 12;
            save.run.cash = 777_000L;
            save.run.health = 66;
            save.run.pendingEventId = "event_ot";
            save.meta.unlockedEndingIds.Add("ending_cash_king");
            save.meta.totalExperience = 250;

            repo.Save(save);
            var loaded = repo.LoadOrCreate();

            Assert.IsTrue(loaded.run.hasActiveRun);
            Assert.AreEqual(12, loaded.run.currentDay);
            Assert.AreEqual(777_000L, loaded.run.cash);
            Assert.AreEqual(66, loaded.run.health);
            Assert.AreEqual("event_ot", loaded.run.pendingEventId);
            Assert.AreEqual(250, loaded.meta.totalExperience);
            CollectionAssert.Contains(loaded.meta.unlockedEndingIds, "ending_cash_king");
        }

        [Test]
        public void CorruptJson_ReturnsDefaults_WithoutThrowing()
        {
            var memory = new InMemorySaveService();
            memory.WriteAllText("{ this is not json");
            var repo = new SaveRepository(memory);

            var loaded = repo.LoadOrCreate();

            Assert.IsNotNull(loaded);
            Assert.IsFalse(loaded.run.hasActiveRun);
            Assert.AreEqual(SaveVersion.Current, loaded.version);

            // 복구 시 정상 JSON으로 덮어쓰므로 재로드에서도 경고 없이 통과한다.
            var loadedAgain = repo.LoadOrCreate();
            Assert.AreEqual(SaveVersion.Current, loadedAgain.version);
            Assert.IsFalse(loadedAgain.run.hasActiveRun);
            Assert.IsFalse(string.IsNullOrWhiteSpace(memory.ReadAllText()));
            Assert.IsFalse(memory.ReadAllText().Contains("this is not json"));
        }

        [Test]
        public void ClearRunAndSave_KeepsMeta()
        {
            var memory = new InMemorySaveService();
            var repo = new SaveRepository(memory);
            var save = SaveRepository.CreateDefault();
            save.run.hasActiveRun = true;
            save.run.currentDay = 8;
            save.meta.unlockedEndingIds.Add("ending_a");
            save.meta.totalExperience = 40;
            repo.Save(save);

            repo.ClearRunAndSave(save);
            var loaded = repo.LoadOrCreate();

            Assert.IsFalse(loaded.run.hasActiveRun);
            Assert.AreEqual(1, loaded.run.currentDay);
            CollectionAssert.Contains(loaded.meta.unlockedEndingIds, "ending_a");
            Assert.AreEqual(40, loaded.meta.totalExperience);
        }

        [Test]
        public void OldVersion_IsMigratedSafely()
        {
            var memory = new InMemorySaveService();
            memory.WriteAllText("{\"version\":0,\"run\":{\"hasActiveRun\":true,\"currentDay\":3},\"meta\":{}}");
            var repo = new SaveRepository(memory);

            var loaded = repo.LoadOrCreate();
            Assert.AreEqual(SaveVersion.Current, loaded.version);
            Assert.IsTrue(loaded.run.hasActiveRun);
            Assert.AreEqual(3, loaded.run.currentDay);

            // 마이그레이션 결과가 파일에 반영된다.
            var reloaded = repo.LoadOrCreate();
            Assert.AreEqual(SaveVersion.Current, reloaded.version);
            Assert.IsTrue(
                memory.ReadAllText().Contains($"\"version\": {SaveVersion.Current}")
                || memory.ReadAllText().Contains($"\"version\":{SaveVersion.Current}"));
        }

        [Test]
        public void SaveMapper_CaptureAndRestore_GameState()
        {
            var state = new GameState
            {
                CurrentDay = 9,
                JobId = "job_a",
                TraitId = "trait_b",
                Salary = 2_800_000L,
                RandomSeed = 42
            };
            state.Stats.Cash = 123_456L;
            state.Stats.Health = 71;
            state.Stats.Stress = 33;
            state.Stats.Happiness = 44;
            state.Stats.CompanyScore = 55;

            var random = new SeededRandomService(42);
            random.Next(10);
            random.Next(10);

            var fallback = ScriptableObject.CreateInstance<EventData>();
            fallback.EditorSetCore(
                "fallback",
                "f",
                "d",
                EventCategory.Rest,
                1,
                30,
                1,
                new EventCondition(),
                new System.Collections.Generic.List<EventChoiceData>
                {
                    new EventChoiceData("a", "1"),
                    new EventChoiceData("b", "2"),
                    new EventChoiceData("c", "3")
                });

            var selector = new EventSelector(new[] { fallback }, fallback, random);
            selector.RestoreHistory(new[] { "e1", "e2" }, "e2");

            var run = SaveMapper.CaptureRun(state, random, selector, "pending_evt");
            var restored = SaveMapper.ToGameState(run);

            Assert.AreEqual(9, restored.CurrentDay);
            Assert.AreEqual(123_456L, restored.Stats.Cash);
            Assert.AreEqual(2, run.consumedRandomCalls);
            Assert.AreEqual("pending_evt", run.pendingEventId);
            CollectionAssert.AreEqual(new[] { "e1", "e2" }, run.recentEventIds);
        }

        [Test]
        public void SeededRandom_FastForward_RestoresSequence()
        {
            var a = new SeededRandomService(99);
            var first = a.Next(1000);
            var second = a.Next(1000);
            var consumed = a.ConsumedCount;

            var b = new SeededRandomService(99);
            b.FastForward(consumed);
            var nextA = a.Next(1000);
            var nextB = b.Next(1000);

            Assert.AreEqual(nextA, nextB);
            Assert.AreNotEqual(first, second);
        }
    }
}
