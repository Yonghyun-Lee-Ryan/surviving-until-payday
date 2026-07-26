using System;
using System.Globalization;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 로컬 캘린더 날짜 기준 “오늘의 직장인” 시드.
    /// </summary>
    public static class DailyChallenge
    {
        public const string DateKeyFormat = "yyyy-MM-dd";

        public static string LocalDateKey(DateTime? localNow = null)
        {
            var date = (localNow ?? DateTime.Now).Date;
            return date.ToString(DateKeyFormat, CultureInfo.InvariantCulture);
        }

        public static bool TryParseDateKey(string dateKey, out DateTime date)
        {
            return DateTime.TryParseExact(
                dateKey,
                DateKeyFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);
        }

        /// <summary>
        /// 날짜 문자열을 안정적인 int 시드로 변환한다. 같은 키면 항상 같은 값.
        /// </summary>
        public static int SeedFromDateKey(string dateKey)
        {
            if (string.IsNullOrEmpty(dateKey))
            {
                return 1;
            }

            unchecked
            {
                var hash = 2166136261u;
                for (var i = 0; i < dateKey.Length; i++)
                {
                    hash ^= dateKey[i];
                    hash *= 16777619u;
                }

                var seed = (int)hash;
                return seed == 0 ? 1 : seed;
            }
        }

        public static int SeedForLocalToday(DateTime? localNow = null)
        {
            return SeedFromDateKey(LocalDateKey(localNow));
        }
    }
}
