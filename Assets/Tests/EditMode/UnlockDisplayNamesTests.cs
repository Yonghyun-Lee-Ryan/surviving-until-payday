using System.Collections.Generic;
using NUnit.Framework;
using SurviveUntilPayday.UI;

namespace SurviveUntilPayday.Tests
{
    public sealed class UnlockDisplayNamesTests
    {
        [Test]
        public void MapHelpers_HandleNullAndEmpty()
        {
            Assert.AreEqual(0, UnlockDisplayNames.MapEventTitles(null).Count);
            Assert.AreEqual(0, UnlockDisplayNames.MapTraitNames(new List<string>()).Count);
            Assert.AreEqual(0, UnlockDisplayNames.MapJobNames(null).Count);
        }

        [Test]
        public void UnknownId_FallsBackToReadableText()
        {
            Assert.AreEqual("unknown thing", UnlockDisplayNames.TraitName("trait_unknown_thing"));
            Assert.AreEqual("sample 99", UnlockDisplayNames.EventTitle("event_sample_99"));
            Assert.AreEqual("temp gig", UnlockDisplayNames.JobName("job_temp_gig"));
        }
    }
}
