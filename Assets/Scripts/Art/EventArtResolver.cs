using UnityEngine;

namespace SurviveUntilPayday.Art
{
    /// <summary>
    /// 사건 ID별 일러스트. Resources/Art/Events/{id} 가 있으면 카테고리 배경보다 우선한다.
    /// </summary>
    public static class EventArtResolver
    {
        public const string ResourcesFolder = "Art/Events";

        public static Sprite TryLoadEventIllustration(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return null;
            }

            return Resources.Load<Sprite>($"{ResourcesFolder}/{eventId.Trim()}");
        }

        public static Sprite ResolveBackgroundSprite(string eventId, Sprite categoryFallback)
        {
            var specific = TryLoadEventIllustration(eventId);
            return specific != null ? specific : categoryFallback;
        }
    }
}
