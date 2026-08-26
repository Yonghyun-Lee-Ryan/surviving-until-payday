using System.Collections.Generic;
using UnityEngine;

namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 업적 표시 데이터 (R-QA-05). 트리거 id는 AchievementIds와 동일하다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Achievement_",
        menuName = "Survive Until Payday/Data/Achievement",
        order = 45)]
    public sealed class AchievementData : ScriptableObject
    {
        [SerializeField] private string id = "ach_";
        [SerializeField] private string title = "업적";
        [SerializeField] [TextArea(2, 4)] private string description;

        public string Id => id;
        public string Title => title;
        public string Description => description;

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add("id가 비어 있습니다.");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                errors.Add("title이 비어 있습니다.");
            }

            return errors;
        }

#if UNITY_EDITOR
        public void EditorSet(string newId, string newTitle, string newDescription)
        {
            id = newId;
            title = newTitle;
            description = newDescription ?? string.Empty;
        }
#endif
    }
}
