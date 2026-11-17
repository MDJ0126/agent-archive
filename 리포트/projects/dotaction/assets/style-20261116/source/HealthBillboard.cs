using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace DotAction
{
    /// <summary>
    /// 체력바의 긴 축과 높이를 카메라 화면 축에 맞춘다.
    /// </summary>
    [ExecuteAlways]
    public sealed class HealthBillboard : MonoBehaviour
    {
        public Camera targetCamera;
        private readonly Dictionary<Camera, Quaternion> _rotations = new();

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += BeginCamera;
            RenderPipelineManager.endCameraRendering += EndCamera;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
            RenderPipelineManager.endCameraRendering -= EndCamera;
            foreach (Quaternion rotation in _rotations.Values)
            {
                transform.rotation = rotation;
                break;
            }
            _rotations.Clear();
        }

        private void BeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if ((camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView) || (targetCamera != null && camera != targetCamera))
            {
                return;
            }
            _rotations[camera] = transform.rotation;
            transform.rotation = camera.transform.rotation;
        }

        private void EndCamera(ScriptableRenderContext context, Camera camera)
        {
            if (_rotations.TryGetValue(camera, out Quaternion rotation))
            {
                transform.rotation = rotation;
                _rotations.Remove(camera);
            }
        }
    }
}
