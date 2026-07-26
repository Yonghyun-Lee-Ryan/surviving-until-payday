using NUnit.Framework;
using SurviveUntilPayday.Core;
using SurviveUntilPayday.Data;
using SurviveUntilPayday.Save;

namespace SurviveUntilPayday.Tests
{
    public sealed class Unit26UxCopyTests
    {
        [Test]
        public void StatCopy_HasDescriptionsForAllGaugesAndCash()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(StatCopy.GetDescription(StatType.Cash)));
            Assert.IsFalse(string.IsNullOrWhiteSpace(StatCopy.GetDescription(StatType.Health)));
            Assert.IsFalse(string.IsNullOrWhiteSpace(StatCopy.GetDescription(StatType.Stress)));
            Assert.IsFalse(string.IsNullOrWhiteSpace(StatCopy.GetDescription(StatType.Happiness)));
            Assert.IsFalse(string.IsNullOrWhiteSpace(StatCopy.GetDescription(StatType.CompanyScore)));
        }

        [Test]
        public void FailureTipCatalog_DiffersByFailureReason()
        {
            var bankruptcy = FailureTipCatalog.GetTip(FailureReason.Bankruptcy, false);
            var burnout = FailureTipCatalog.GetTip(FailureReason.Burnout, false);
            var success = FailureTipCatalog.GetTip(FailureReason.None, true);
            Assert.IsFalse(string.IsNullOrWhiteSpace(bankruptcy));
            Assert.IsFalse(string.IsNullOrWhiteSpace(burnout));
            Assert.AreNotEqual(bankruptcy, burnout);
            Assert.IsTrue(success.Contains("다음엔 이렇게"));
        }

        [Test]
        public void CrisisWarningCopy_HasCashThresholds()
        {
            Assert.Greater(CrisisWarningCopy.LowCashThreshold, CrisisWarningCopy.CriticalCashThreshold);
            Assert.IsFalse(string.IsNullOrWhiteSpace(CrisisWarningCopy.CriticalCash));
        }

        [Test]
        public void Meta_FirstRunTutorialFlag_RoundTripsThroughSaveMapper()
        {
            var meta = new MetaProgressionManager();
            meta.Load(0, null, null, null, null);
            Assert.IsFalse(meta.FirstRunTutorialCompleted);
            meta.MarkFirstRunTutorialCompleted();

            var captured = SaveMapper.CaptureMeta(meta);
            Assert.IsTrue(captured.firstRunTutorialCompleted);

            var loaded = new MetaProgressionManager();
            SaveMapper.ApplyMeta(captured, loaded);
            Assert.IsTrue(loaded.FirstRunTutorialCompleted);
        }

        [Test]
        public void SaveVersion_IsAtLeast7()
        {
            Assert.GreaterOrEqual(SaveVersion.Current, 7);
        }
    }
}
