using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 처음 명중한 대상에게만 피해를 주고 그 즉시 파괴된다(관통 없음, GDD 6.4).
    /// </summary>
    // Destroy 대신 ProjectilePool로 반납한다 - 반납할 풀은 ProjectilePool.Get()이 체크아웃 직후 SetPool()로 주입해 준다.
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float maxLifetime = 3f;

        private Vector3 _direction;
        private float _damage;
        private float _speed;
        private Transform _shooterRoot;
        private SimpleObjectPool<Projectile> _pool;

        public void SetPool(SimpleObjectPool<Projectile> owningPool)
        {
            _pool = owningPool;
        }

        public void Init(Vector3 travelDirection, float attackDamage, float travelSpeed, Transform shooter)
        {
            _direction = travelDirection;
            _damage = attackDamage;
            _speed = travelSpeed;
            _shooterRoot = shooter;
            Invoke(nameof(ReleaseSelf), maxLifetime);
        }

        private void Update()
        {
            transform.position += _direction * (_speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_shooterRoot && other.transform.root == _shooterRoot) return;

            if (other.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(_damage);
                ReleaseSelf();
                return;
            }

            if (!other.isTrigger)
            {
                // 피해를 줄 수 없는 고체 장애물(예: 아레나 벽)에 부딪혔다.
                ReleaseSelf();
            }
        }

        private void ReleaseSelf()
        {
            CancelInvoke(nameof(ReleaseSelf));
            _pool.Release(this);
        }
    }
}
