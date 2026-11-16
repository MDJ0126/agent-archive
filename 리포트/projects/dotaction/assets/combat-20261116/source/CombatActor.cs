using System.Collections.Generic;
using DesignDataTable;
using UnityEngine;

namespace DotAction
{
    /// <summary>
    /// DDT 전투 수치를 적용하고 자동 공격·추적·피격·사망을 처리한다.
    /// </summary>
    public sealed class CombatActor : MonoBehaviour
    {
        public static readonly List<CombatActor> Actors = new();
        public int statsId = 1;
        public bool isPlayer;
        public GridMotor motor;
        public Animator animator;
        public Transform healthFill;
        public ArcBolt boltPrefab;
        public float Health { get; private set; }
        public CombatStats Stats { get; private set; }
        public bool IsAlive => Health > 0;
        public int ShotsFired { get; private set; }
        private float _nextAttack;
        private float _nextPath;
        private CombatActor _player;

        private void Awake()
        {
            Stats = CombatStatsTable.instance.GetData(statsId);
            if (Stats == null)
            {
                throw new System.InvalidOperationException("CombatStats 행을 찾지 못했습니다: " + statsId);
            }
            Health = Stats.Health;
            motor.speed = Stats.Speed;
        }

        private void OnEnable()
        {
            Actors.Add(this);
        }

        private void Update()
        {
            if (!IsAlive)
            {
                return;
            }
            if (!isPlayer)
            {
                if (_player == null)
                {
                    _player = Actors.Find(actor => actor.isPlayer && actor.IsAlive);
                }
                if (_player == null || !_player.IsAlive)
                {
                    motor.enabled = false;
                    return;
                }
                if (Time.time >= _nextPath)
                {
                    _nextPath = Time.time + 0.45f;
                    Vector2Int playerCell = CombatGrid.Cell(_player.transform.position);
                    // 적은 플레이어 셀을 예약하지 않고 인접한 공격 위치로 이동한다.
                    Vector2Int best = CombatGrid.Cell(transform.position);
                    float distance = float.MaxValue;
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            Vector2Int candidate = playerCell + new Vector2Int(x, y);
                            float score = Vector3.SqrMagnitude(CombatGrid.World(candidate) - transform.position);
                            if ((x != 0 || y != 0) && motor.grid.CanOccupy(candidate, motor) && score < distance)
                            {
                                best = candidate;
                                distance = score;
                            }
                        }
                    }
                    motor.MoveTo(best);
                }
            }
            if (Time.time < _nextAttack)
            {
                return;
            }
            CombatActor target = FindNearestTarget();
            if (target == null)
            {
                return;
            }
            _nextAttack = Time.time + Stats.Cooldown;
            ShotsFired++;
            if (!motor.IsMoving && motor.visual != null)
            {
                Vector3 direction = target.transform.position - transform.position;
                if (direction.sqrMagnitude > .001f)
                {
                    motor.visual.rotation = Quaternion.LookRotation(direction);
                }
            }
            animator.SetTrigger("Attack");
            if (isPlayer)
            {
                ArcBolt bolt = Instantiate(boltPrefab, transform.position + Vector3.up * 0.8f, Quaternion.identity);
                bolt.Launch(target, Stats.Damage);
            }
            else
            {
                target.TakeDamage(Stats.Damage);
            }
        }

        private void OnDisable()
        {
            Actors.Remove(this);
        }

        private CombatActor FindNearestTarget()
        {
            CombatActor closest = null;
            float distance = Stats.Range * Stats.Range;
            foreach (CombatActor candidate in Actors)
            {
                if (!candidate.IsAlive || candidate.isPlayer == isPlayer)
                {
                    continue;
                }
                float next = (candidate.transform.position - transform.position).sqrMagnitude;
                if (next <= distance)
                {
                    closest = candidate;
                    distance = next;
                }
            }
            return closest;
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive)
            {
                return;
            }
            Health = Mathf.Max(0, Health - damage);
            healthFill.localScale = new Vector3(Health / Stats.Health, 1, 1);
            if (Health > 0)
            {
                return;
            }
            animator.SetTrigger("Die");
            motor.enabled = false;
            CombatSession session = FindFirstObjectByType<CombatSession>();
            session.NotifyDeath(this);
            if (!isPlayer)
            {
                Destroy(gameObject, 2);
            }
        }
    }
}
