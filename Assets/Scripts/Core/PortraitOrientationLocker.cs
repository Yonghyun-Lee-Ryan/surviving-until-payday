using UnityEngine;

namespace SurviveUntilPayday.Core
{
    /// <summary>
    /// Android Portrait 전용으로 화면 방향을 고정한다.
    /// </summary>
    public sealed class PortraitOrientationLocker : MonoBehaviour
    {
        private void Awake()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
        }
    }
}
