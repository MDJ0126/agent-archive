using UnityEngine;

namespace TurnRoom.PixelCamera.Samples
{
    /// <summary>
    /// 저장된 네온 옥상 예제에서 다섯 렌더 효과와 연속 이동을 비교한다.
    /// </summary>
    public sealed class FiveSwitchShowcase : MonoBehaviour
    {
        [System.Serializable]
        public sealed class MaterialSet
        {
            public Renderer renderer;
            public Material[] smooth, toon;
        }
        public MaterialSet[] materialSets;
        public PixelCameraEffect effect;
        public PixelCameraPreset preset;
        public PixelCameraPalette palette;
        public Renderer[] surfaces;
        public Material[] smoothMaterials;
        public Material[] toonMaterials;
        public Transform runner;
        public PixelCameraSteppedAnimator steppedAnimator;
        public bool[] switches = { true, true, true, true, true };
        public GameObject[] enabledIndicators;
        public PixelCameraObject[] pixelObjects;
        public GameObject[] languageGuides;
        public bool externalMotion;
        public bool IsPaused => _paused;
        private PixelCameraPreset _working;
        private Vector3 _runnerHome;
        private Vector3 _cameraHome;
        private Quaternion _cameraRotation;
        private float _time;
        private bool _paused;

        private void Awake()
        {
            _runnerHome = runner.localPosition;
            _cameraHome = effect.transform.position;
            _cameraRotation = effect.transform.rotation;
            _working = Instantiate(preset);
            effect.preset = _working;
            ApplySwitches();
        }

        private void Update()
        {
            for (int i = 0; i < 5; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                {
                    ToggleSwitch(i);
                }
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                bool enabled = !switches[0];
                for (int i = 0; i < 5; i++)
                {
                    switches[i] = enabled;
                }
                ApplySwitches();
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _paused = !_paused;
                steppedAnimator.GetComponent<Animator>().speed = _paused ? 0 : 1;
            }
            if (!_paused && !externalMotion)
            {
                _time += Time.deltaTime;
                runner.localPosition = _runnerHome + Vector3.right * Mathf.Sin(_time * .35f) * 3;
            }
            if (Input.GetMouseButton(1) && (!externalMotion || _paused))
            {
                effect.transform.RotateAround(externalMotion ? runner.position + Vector3.up * 1.3f : new Vector3(0, 3, 3), Vector3.up, Input.GetAxis("Mouse X") * 2);
            }
            if (Input.GetKeyDown(KeyCode.R) && !externalMotion)
            {
                effect.transform.SetPositionAndRotation(_cameraHome, _cameraRotation);
            }
        }

        private void OnDestroy()
        {
            if (_working != null)
            {
                Destroy(_working);
            }
        }

        /// <summary>
        /// 키보드와 저장된 버튼 이벤트에서 지정한 비교 기능을 전환한다.
        /// </summary>
        public void ToggleSwitch(int index)
        {
            if (index < 0 || index >= switches.Length)
            {
                return;
            }
            switches[index] = !switches[index];
            ApplySwitches();
        }

        /// <summary>
        /// 장면 자산을 보존하며 개인 프리셋과 렌더러의 비교 상태를 적용한다.
        /// </summary>
        public void ApplySwitches()
        {
            PixelCameraPreset settings = _working != null ? _working : preset;
            effect.enabled = switches[0] || switches[2] || switches[3];
            for (int i = 0; i < pixelObjects.Length; i++)
            {
                pixelObjects[i].pixelSize = switches[0] ? 4 : 1;
            }
            settings.outline = switches[2];
            settings.palette = switches[3] ? palette : null;
            settings.orderedDithering = switches[3];
            steppedAnimator.enabled = switches[4];
            for (int i = 0; i < surfaces.Length; i++)
            {
                surfaces[i].sharedMaterial = switches[1] ? toonMaterials[i] : smoothMaterials[i];
            }
            if (materialSets != null)
            {
                foreach (var set in materialSets)
                {
                    if (set.renderer != null)
                    {
                        set.renderer.sharedMaterials = switches[1] ? set.toon : set.smooth;
                    }
                }
            }
            for (int i = 0; i < enabledIndicators.Length; i++)
            {
                enabledIndicators[i].SetActive(switches[i % 5]);
            }
        }
    }
}
