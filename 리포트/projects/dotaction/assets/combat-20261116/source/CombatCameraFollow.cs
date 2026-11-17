using UnityEngine;

namespace DotAction
{
    /// <summary>
    /// 가까운 사선 탑뷰를 유지하며 플레이어를 부드럽게 추적한다.
    /// </summary>
    public sealed class CombatCameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(10, 14, -10);
        public float smoothTime = .12f;
        private Vector3 _velocity;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }
            transform.position = Vector3.SmoothDamp(transform.position, target.position + offset, ref _velocity, smoothTime);
        }
    }
}
