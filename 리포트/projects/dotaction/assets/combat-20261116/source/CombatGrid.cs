using System.Collections.Generic;
using UnityEngine;

namespace DotAction
{
    /// <summary>
    /// XZ 격자의 장애물과 점유를 관리하고 대각선 모서리 통과를 막는다.
    /// </summary>
    public sealed class CombatGrid : MonoBehaviour
    {
        public int radius = 12;
        public Vector2Int[] blockedCells;
        private readonly HashSet<Vector2Int> _blocked = new();
        private readonly Dictionary<Vector2Int, GridMotor> _occupants = new();

        private void Awake()
        {
            _blocked.Clear();
            foreach (Vector2Int cell in blockedCells)
            {
                _blocked.Add(cell);
            }
        }

        public bool IsWalkable(Vector2Int cell)
        {
            return Mathf.Abs(cell.x) <= radius && Mathf.Abs(cell.y) <= radius && !_blocked.Contains(cell);
        }

        public static Vector2Int Cell(Vector3 point)
        {
            return new Vector2Int(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.z));
        }

        public static Vector3 World(Vector2Int cell)
        {
            return new Vector3(cell.x, 0, cell.y);
        }

        public bool TryOccupy(Vector2Int cell, GridMotor owner)
        {
            if (!IsWalkable(cell) || (_occupants.TryGetValue(cell, out GridMotor other) && other != null && other != owner))
            {
                return false;
            }
            _occupants[cell] = owner;
            return true;
        }

        public bool CanOccupy(Vector2Int cell, GridMotor owner)
        {
            return IsWalkable(cell) && (!_occupants.TryGetValue(cell, out GridMotor other) || other == null || other == owner);
        }

        public void Release(Vector2Int cell, GridMotor owner)
        {
            if (_occupants.TryGetValue(cell, out GridMotor other) && other == owner)
            {
                _occupants.Remove(cell);
            }
        }

        /// <summary>
        /// A*로 8방향 최단 경로를 찾으며 실패하면 빈 경로를 반환한다.
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            var result = new List<Vector2Int>();
            if (!IsWalkable(goal) || start == goal)
            {
                return result;
            }
            var open = new List<Vector2Int> { start };
            var cost = new Dictionary<Vector2Int, float> { [start] = 0 };
            var parents = new Dictionary<Vector2Int, Vector2Int>();
            var closed = new HashSet<Vector2Int>();
            while (open.Count > 0)
            {
                Vector2Int current = open[0];
                foreach (Vector2Int candidate in open)
                {
                    if (cost[candidate] + Vector2Int.Distance(candidate, goal) < cost[current] + Vector2Int.Distance(current, goal))
                    {
                        current = candidate;
                    }
                }
                open.Remove(current);
                if (current == goal)
                {
                    while (current != start)
                    {
                        result.Add(current);
                        current = parents[current];
                    }
                    result.Reverse();
                    return result;
                }
                closed.Add(current);
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        Vector2Int next = current + new Vector2Int(x, y);
                        if ((x == 0 && y == 0) || !IsWalkable(next) || closed.Contains(next))
                        {
                            continue;
                        }
                        if (x != 0 && y != 0 && (!IsWalkable(current + new Vector2Int(x, 0)) || !IsWalkable(current + new Vector2Int(0, y))))
                        {
                            continue;
                        }
                        float score = cost[current] + (x != 0 && y != 0 ? 1.41421356f : 1f);
                        if (!cost.TryGetValue(next, out float previous) || score < previous)
                        {
                            cost[next] = score;
                            parents[next] = current;
                            if (!open.Contains(next))
                            {
                                open.Add(next);
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}
