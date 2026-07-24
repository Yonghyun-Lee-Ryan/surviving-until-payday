using UnityEngine;
using UnityEngine.UI;

namespace SurviveUntilPayday.UI
{
    /// <summary>
    /// 씬의 모든 uGUI Text에 Noto Sans KR을 적용한다.
    /// </summary>
    public sealed class UiFontBootstrap : MonoBehaviour
    {
        [SerializeField] private bool includeInactive = true;
        [SerializeField] private bool applyOnEnable = true;

        private void OnEnable()
        {
            if (applyOnEnable)
            {
                ApplyAll();
            }
        }

        [ContextMenu("Apply Korean Font Now")]
        public void ApplyAll()
        {
            var texts = includeInactive
                ? GetComponentsInChildren<Text>(true)
                : GetComponentsInChildren<Text>(false);
            for (var i = 0; i < texts.Length; i++)
            {
                UiFont.Apply(texts[i]);
            }
        }
    }
}
