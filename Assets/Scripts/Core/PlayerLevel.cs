using System;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// 인생 경험치 → 레벨 변환.
    /// 레벨 n에서 다음 레벨까지 n*100 XP가 필요하다.
    /// </summary>
    public static class PlayerLevel
    {
        public const int MaxLevel = 50;

        public static int GetLevel(int totalExperience)
        {
            if (totalExperience < 0)
            {
                totalExperience = 0;
            }

            var level = 1;
            var remaining = totalExperience;
            while (level < MaxLevel)
            {
                var need = GetXpToNextLevel(level);
                if (remaining < need)
                {
                    break;
                }

                remaining -= need;
                level++;
            }

            return level;
        }

        public static int GetXpToNextLevel(int currentLevel)
        {
            var level = Math.Max(1, currentLevel);
            return level * 100;
        }

        public static int GetXpIntoCurrentLevel(int totalExperience, out int level, out int xpToNext)
        {
            level = GetLevel(totalExperience);
            var spent = 0;
            for (var i = 1; i < level; i++)
            {
                spent += GetXpToNextLevel(i);
            }

            var intoLevel = Math.Max(0, totalExperience - spent);
            xpToNext = level >= MaxLevel ? 0 : GetXpToNextLevel(level);
            return intoLevel;
        }
    }
}
