using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DotAction
{
    /// <summary>
    /// 목적지 입력과 3웨이브 전투 진행을 관리하고 저장된 HUD의 동적 값만 갱신한다.
    /// </summary>
    public sealed class CombatSession : MonoBehaviour
    {
        public CombatActor player;
        public CombatActor[] enemyPrefabs;
        public Transform destinationMarker;
        public LineRenderer route;
        public Text healthText;
        public Text progressText;
        public Text resultText;
        public Camera worldCamera;
        public bool IsFinished { get; private set; }
        public int Kills { get; private set; }
        public int Wave { get; private set; }
        private int _remaining;
        private float _nextWave = 1;
        private float _started;
        private float _nextKeyboardMove;

        private void Start()
        {
            _started = Time.time;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }
            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(player.Health)} / {player.Stats.Health:0}";
            }
            if (!IsFinished && progressText != null)
            {
                progressText.text = $"WAVE {Wave}/3   /   {Kills} SLAIN";
            }
            if (IsFinished)
            {
                return;
            }
            if (_remaining == 0 && Time.time >= _nextWave)
            {
                if (Wave >= 3)
                {
                    Finish("RUINS CLEARED\nR  /  RUN AGAIN");
                    return;
                }
                SpawnWave();
            }
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Ray ray = worldCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (plane.Raycast(ray, out float distance))
                {
                    SetDestination(CombatGrid.Cell(ray.GetPoint(distance)));
                }
            }
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || Time.time < _nextKeyboardMove)
            {
                return;
            }
            int x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            int y = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            if (x != 0 || y != 0)
            {
                _nextKeyboardMove = Time.time + 0.18f;
                // 카메라의 화면 오른쪽·위쪽 방향을 격자 축으로 변환한다.
                Vector3 right = worldCamera.transform.right;
                Vector3 up = Vector3.ProjectOnPlane(worldCamera.transform.up, Vector3.up).normalized;
                Vector3 direction = right * x + up * y;
                SetDestination(CombatGrid.Cell(player.transform.position) + new Vector2Int(Mathf.RoundToInt(direction.x), Mathf.RoundToInt(direction.z)));
            }
        }

        public bool SetDestination(Vector2Int cell)
        {
            if (IsFinished || !player.motor.MoveTo(cell))
            {
                return false;
            }
            destinationMarker.position = CombatGrid.World(cell) + Vector3.up * 0.03f;
            var path = player.motor.grid.FindPath(CombatGrid.Cell(player.transform.position), cell);
            route.positionCount = path.Count + 1;
            route.SetPosition(0, player.transform.position + Vector3.up * 0.04f);
            for (int i = 0; i < path.Count; i++)
            {
                route.SetPosition(i + 1, CombatGrid.World(path[i]) + Vector3.up * 0.04f);
            }
            return true;
        }

        private void SpawnWave()
        {
            Wave++;
            _remaining = 4 + Wave * 2;
            for (int i = 0; i < _remaining; i++)
            {
                float angle = (i * 360f / _remaining + Wave * 23) * Mathf.Deg2Rad;
                Vector2Int cell = new Vector2Int(Mathf.RoundToInt(Mathf.Cos(angle) * 9), Mathf.RoundToInt(Mathf.Sin(angle) * 9));
                CombatActor enemy = Instantiate(enemyPrefabs[i % enemyPrefabs.Length], CombatGrid.World(cell), Quaternion.identity);
                enemy.motor.grid = player.motor.grid;
            }
            Debug.Log($"DOTACTION_WAVE: {Wave}; enemies={_remaining}");
        }

        public void NotifyDeath(CombatActor actor)
        {
            if (actor.isPlayer)
            {
                Finish("RANGER FALLEN\nR  /  TRY AGAIN");
            }
            else
            {
                Kills++;
                _remaining--;
                if (_remaining == 0)
                {
                    _nextWave = Time.time + 2;
                }
            }
        }

        private void Finish(string message)
        {
            IsFinished = true;
            if (resultText != null)
            {
                resultText.text = message.Replace("\n", "   /   ");
            }
            player.motor.enabled = false;
            player.enabled = false;
            foreach (CombatActor actor in CombatActor.Actors.ToArray())
            {
                actor.motor.enabled = false;
                actor.enabled = false;
            }
        }
    }
}
