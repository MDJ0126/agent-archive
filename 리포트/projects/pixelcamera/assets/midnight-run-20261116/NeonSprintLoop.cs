using UnityEngine;

namespace TurnRoom.PixelCamera.Samples
{
    /// <summary>
    /// 연속 질주와 추적 카메라·깊이별 도시 순환을 담당한다.
    /// </summary>
    public sealed class NeonSprintLoop : MonoBehaviour
    {
        [System.Serializable]
        public struct CityLayer
        {
            public Transform[] chunks;
            [Range(.05f, 1.2f)] public float parallax;
            public float length;
        }

        public FiveSwitchShowcase showcase;
        public Transform runner;
        public Camera followCamera;
        public float speed = 7.5f;
        public float lead = 2.8f;
        public CityLayer[] layers;
        private Vector3 _cameraHome;
        private Quaternion _cameraRotation;
        private float[][] _origins;
        private float _distance;

        public float Distance => _distance;

        private void Awake()
        {
            _cameraHome = followCamera.transform.position;
            _cameraRotation = followCamera.transform.rotation;
            _origins = new float[layers.Length][];
            for (int i = 0; i < layers.Length; i++)
            {
                _origins[i] = new float[layers[i].chunks.Length];
                for (int j = 0; j < layers[i].chunks.Length; j++)
                {
                    _origins[i][j] = layers[i].chunks[j].position.x;
                }
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetView();
            }
            if (showcase.IsPaused)
            {
                return;
            }
            float delta = Mathf.Max(0, Time.deltaTime);
            _distance += speed * delta;
            runner.position += Vector3.right * speed * delta;
            UpdateLayers();
        }

        private void LateUpdate()
        {
            if (showcase.IsPaused)
            {
                return;
            }
            ResetView();
        }

        /// <summary>
        /// 실제 캐릭터 이동과 독립된 거리로 각 깊이의 모듈을 화면 밖에서 재배치한다.
        /// </summary>
        private void UpdateLayers()
        {
            for (int i = 0; i < layers.Length; i++)
            {
                CityLayer layer = layers[i];
                if (layer.chunks == null || layer.chunks.Length < 2 || layer.length <= 0)
                {
                    continue;
                }
                float travel = _distance * layer.parallax;
                float span = layer.length * layer.chunks.Length;
                for (int j = 0; j < layer.chunks.Length; j++)
                {
                    while (travel - _origins[i][j] > layer.length * .5f + 22)
                    {
                        _origins[i][j] += span;
                    }
                    Vector3 position = layer.chunks[j].position;
                    position.x = _origins[i][j] + _distance * (1 - layer.parallax);
                    layer.chunks[j].position = position;
                }
            }
        }

        /// <summary>
        /// 전진 중인 캐릭터보다 앞을 보여 주는 횡스크롤 시점을 복원한다.
        /// </summary>
        public void ResetView()
        {
            followCamera.transform.SetPositionAndRotation(
                new Vector3(runner.position.x + lead, _cameraHome.y, _cameraHome.z),
                _cameraRotation);
        }
    }
}
