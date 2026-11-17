using System.Collections.Generic;
using UnityEngine;

namespace DotAction
{
    /// <summary>
    /// 셀 사이를 부드럽게 이동하며 출발·도착 셀을 예약해 겹침을 막는다.
    /// </summary>
    public sealed class GridMotor : MonoBehaviour
    {
        public CombatGrid grid;
        public float speed = 4;
        public Transform visual;
        public Animator animator;
        public bool IsMoving { get; private set; }
        public int CompletedSteps { get; private set; }
        private List<Vector2Int> _path = new();
        private Vector2Int _cell;
        private Vector2Int _next;
        private bool _isInSegment;

        private void Start()
        {
            if (grid == null)
            {
                grid = FindFirstObjectByType<CombatGrid>();
            }
            _cell = CombatGrid.Cell(transform.position);
            transform.position = CombatGrid.World(_cell);
            grid.TryOccupy(_cell, this);
        }

        private void Update()
        {
            if (!_isInSegment && _path.Count > 0 && grid.TryOccupy(_path[0], this))
            {
                _next = _path[0];
                _path.RemoveAt(0);
                _isInSegment = true;
            }
            IsMoving = _isInSegment;
            if (animator != null)
            {
                animator.SetBool("Moving", IsMoving);
            }
            if (!_isInSegment)
            {
                return;
            }
            Vector3 target = CombatGrid.World(_next);
            Vector3 direction = target - transform.position;
            if (direction.sqrMagnitude > 0.0001f && visual != null)
            {
                visual.rotation = Quaternion.RotateTowards(visual.rotation, Quaternion.LookRotation(direction), 720 * Time.deltaTime);
            }
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            if ((transform.position - target).sqrMagnitude < 0.00001f)
            {
                grid.Release(_cell, this);
                _cell = _next;
                _isInSegment = false;
                CompletedSteps++;
            }
        }

        private void OnDisable()
        {
            if (grid != null)
            {
                grid.Release(_cell, this);
                if (_isInSegment)
                {
                    grid.Release(_next, this);
                }
            }
            if (animator != null)
            {
                animator.SetBool("Moving", false);
            }
        }

        public bool MoveTo(Vector2Int destination)
        {
            Vector2Int start = _isInSegment ? _next : _cell;
            if (!grid.IsWalkable(destination))
            {
                return false;
            }
            List<Vector2Int> path = grid.FindPath(start, destination);
            if (path.Count == 0 && destination != start)
            {
                return false;
            }
            _path = path;
            return true;
        }
    }
}
