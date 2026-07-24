using NUnit.Framework;
using SurviveUntilPayday.UI;

namespace SurviveUntilPayday.Tests
{
    public sealed class UiFormatTests
    {
        [Test]
        public void KoreanWonFormatter_UsesThousandsSeparator()
        {
            Assert.AreEqual("1,000,000원", KoreanWonFormatter.Format(1_000_000L));
            Assert.AreEqual("-15,000원", KoreanWonFormatter.Format(-15_000L));
            Assert.AreEqual("+25,000원", KoreanWonFormatter.FormatDelta(25_000L));
            Assert.AreEqual("-8,000원", KoreanWonFormatter.FormatDelta(-8_000L));
        }

        [Test]
        public void DayDisplayFormatter_UsesKoreanWeekday()
        {
            Assert.AreEqual("1일 (월)", DayDisplayFormatter.Format(1, System.DayOfWeek.Monday));
            Assert.AreEqual("7일 (일)", DayDisplayFormatter.Format(7, System.DayOfWeek.Sunday));
        }
    }
}
