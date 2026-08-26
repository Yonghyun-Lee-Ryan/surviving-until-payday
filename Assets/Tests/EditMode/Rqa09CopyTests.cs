using NUnit.Framework;
using SurviveUntilPayday.Ads;
using SurviveUntilPayday.Core;

namespace SurviveUntilPayday.Tests
{
    public sealed class Rqa09CopyTests
    {
        [Test]
        public void EmptyStateCopy_IsKoreanAndComplete()
        {
            AssertHangul(EmptyStateCopy.ContinueUnavailable);
            AssertHangul(EmptyStateCopy.NoDailyMissions);
            AssertHangul(EmptyStateCopy.NoDailyBest);
            AssertHangul(EmptyStateCopy.NoTraitsHint);
            AssertHangul(EmptyStateCopy.NoStatChanges);
            AssertHangul(EmptyStateCopy.NoResultData);
            AssertHangul(EmptyStateCopy.NoEndingData);
            AssertHangul(EmptyStateCopy.NoResultBody);
            AssertHangul(EmptyStateCopy.CodexEmptyList);
            AssertHangul(EmptyStateCopy.NoDescription);
            Assert.IsFalse(EmptyStateCopy.NoResultBody.ToLowerInvariant().Contains("game scene"));
        }

        [Test]
        public void CreditsCopy_ListsFontAndAudioLicenses()
        {
            Assert.IsTrue(CreditsCopy.Title.Contains("크레딧"), CreditsCopy.Title);
            Assert.IsTrue(CreditsCopy.Body.Contains("Noto"), CreditsCopy.Body);
            Assert.IsTrue(CreditsCopy.Body.Contains("Kenney"), CreditsCopy.Body);
            Assert.IsTrue(CreditsCopy.Body.Contains("SIL") || CreditsCopy.Body.Contains("CC0"), CreditsCopy.Body);
            Assert.IsFalse(CreditsCopy.Body.ToLowerInvariant().Contains("example.com"));
        }

        [Test]
        public void AccessibilityCopy_DocumentsOfflineCorePlay()
        {
            Assert.IsTrue(AccessibilityCopy.OfflineNote.Contains("오프라인"));
            Assert.IsTrue(AccessibilityCopy.OfflineNote.Contains("본편"));
            Assert.AreEqual("배경음", AccessibilityCopy.BgmLabel);
            Assert.AreEqual("효과음", AccessibilityCopy.SfxLabel);
            Assert.GreaterOrEqual(AccessibilityCopy.MinBodyFontSize, 20);
            Assert.GreaterOrEqual(AccessibilityCopy.MinTapHeight, 48f);
        }

        [Test]
        public void AdBlockReasonCopy_MapsOfflineAndServiceUnavailable()
        {
            var offline = AdBlockReasonCopy.FromGatewayReason("offline", RewardedAdPlacement.ChoiceReroll);
            Assert.IsTrue(offline.Contains("오프라인"), offline);
            Assert.IsTrue(AdBlockReasonCopy.ServiceUnavailable.Contains("광고"));
            var network = AdBlockReasonCopy.FromGatewayReason("network error", RewardedAdPlacement.RetryOutcome);
            Assert.IsTrue(network.Contains("오프라인"), network);
        }

        private static void AssertHangul(string value)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(value), "empty copy");
            var hasHangul = false;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] >= 0xAC00 && value[i] <= 0xD7A3)
                {
                    hasHangul = true;
                    break;
                }
            }

            Assert.IsTrue(hasHangul, value);
        }
    }
}
