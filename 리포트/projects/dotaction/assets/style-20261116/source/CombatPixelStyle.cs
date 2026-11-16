using TurnRoom.PixelCamera;
using UnityEngine;

namespace DotAction
{
    /// <summary>
    /// 화면 전체의 공통 픽셀 격자와 제한 팔레트를 지정한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatPixelStyle : MonoBehaviour
    {
        [Min(16)] public int referenceHeight = 360;
        public bool pixelize = true;
        public bool limitPalette = true;
        public PixelCameraPalette palette;
    }
}
