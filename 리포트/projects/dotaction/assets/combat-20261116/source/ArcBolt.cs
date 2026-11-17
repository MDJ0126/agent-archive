using UnityEngine;

namespace DotAction
{
    /// <summary>
    /// 자동 공격의 발사 시점 피해량을 보존하고 살아 있는 목표에만 적용한다.
    /// </summary>
    public sealed class ArcBolt : MonoBehaviour
    {
        public float speed = 14;
        private CombatActor _target;
        private float _damage;
        private float _expires;

        private void Update()
        {
            if (_target == null || !_target.IsAlive || Time.time > _expires)
            {
                Destroy(gameObject);
                return;
            }
            Vector3 target = _target.transform.position + Vector3.up * 0.8f;
            Vector3 direction = target - transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            if ((transform.position - target).sqrMagnitude < 0.04f)
            {
                _target.TakeDamage(_damage);
                Destroy(gameObject);
            }
        }

        public void Launch(CombatActor target, float damage)
        {
            _target = target;
            _damage = damage;
            _expires = Time.time + 3;
        }
    }
}
