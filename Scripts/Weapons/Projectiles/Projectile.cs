using UnityEngine;
using WeaponMaster.Core;
using WeaponMaster.Effects;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 처음 명중한 대상에게만 피해를 주고 그 즉시 파괴된다(관통 없음, GDD 6.4).
    /// </summary>
    // Destroy 대신 ProjectilePool로 반납한다 - 반납할 풀은 ProjectilePool.Spawn()이 생성 직후 SetOwningPool()로 주입해 준다.
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float maxLifetime = 3f;
        [SerializeField] private WeaponTrailEffect trailEffect;

        private Vector3 _direction;
        private float _damage;
        private float _speed;
        private Transform _shooterRoot;
        private Component _attacker;
        private LayerMask _targetLayers;
        private SimpleObjectPool<Projectile> _pool;

        public void SetOwningPool(SimpleObjectPool<Projectile> owningPool)
        {
            _pool = owningPool;
        }

        public void Launch(Vector3 travelDirection, float attackDamage, float travelSpeed, Transform shooter, Component attacker, LayerMask targetLayers, int attackLayer)
        {
            _direction = travelDirection;
            _damage = attackDamage;
            _speed = travelSpeed;
            _shooterRoot = shooter;
            _attacker = attacker;
            _targetLayers = targetLayers;
            if (attackLayer >= 0) gameObject.layer = attackLayer;
            if (trailEffect) trailEffect.Begin();
            Invoke(nameof(ReturnToPool), maxLifetime);
        }

        private void Update()
        {
            transform.position += _direction * (_speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_shooterRoot && other.transform.root == _shooterRoot) return;

            if (AttackTargetRules.TryGetTarget(other, _attacker, _targetLayers, out IDamageable damageable, out Component targetOwner))
            {
                Pose sparkPose = default;
                HitSparkPool sparkPool = HitSparkPool.Instance;
                HitElasticReaction reaction = null;
                bool showSpark = false;

                PrepareHitEffects();
                damageable.TakeDamage(_damage);
                PlayHitEffects();
                ReturnToPool();
                return;

                void PrepareHitEffects()
                {
                    bool validDamage = _damage > 0f
                        && damageable is HealthComponent health && health.IsAlive;
                    if (validDamage) targetOwner.TryGetComponent(out reaction);
                    showSpark = sparkPool && validDamage
                        && targetOwner.TryGetComponent(out HitEffectAnchorSet anchors)
                        && anchors.TryGetPose(transform.position, _direction, out sparkPose);
                }

                void PlayHitEffects()
                {
                    if (showSpark && sparkPool) sparkPool.Spawn(sparkPose);
                    if (reaction) reaction.Play(_direction);
                }
            }

            if (!other.isTrigger)
            {
                // 피해를 줄 수 없는 고체 장애물(예: 아레나 벽)에 부딪혔다.
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            CancelInvoke(nameof(ReturnToPool));
            _pool.Release(this);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(ReturnToPool));
            _shooterRoot = null;
            _attacker = null;
            _targetLayers = 0;
            if (trailEffect) trailEffect.Clear();
        }
    }
}
