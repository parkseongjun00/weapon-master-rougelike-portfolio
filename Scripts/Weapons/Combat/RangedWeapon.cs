using UnityEngine;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 원거리 무기 - 발사하는 즉시 내구도를 소모하며, 투사체의 명중 여부와 무관하다(GDD 5.3).
    /// </summary>
    public class RangedWeapon : WeaponBase
    {
        private const string MuzzlePointName = "MuzzlePoint";

        private Transform _muzzlePoint;

        public override WeaponCategory Category => WeaponCategory.Ranged;

        protected override void Awake()
        {
            base.Awake();
            _muzzlePoint = transform.Find(MuzzlePointName);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_muzzlePoint == null)
            {
                Debug.LogWarning($"[RangedWeapon] '{name}'에 {MuzzlePointName}가 없어 공격 원점에서 투사체를 생성한다.", this);
            }
#endif
        }

        protected override void StartAttack(Vector3 originPosition, Vector3 aimDirection, float damageMultiplier)
        {
            Durability.Consume();

            Vector3 direction = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : transform.forward;
            Vector3 spawnPosition = _muzzlePoint != null ? _muzzlePoint.position : originPosition;

            Projectile projectile = ProjectilePool.Instance.Spawn(Data.Ranged.ProjectilePrefab, spawnPosition, Quaternion.LookRotation(direction));
            projectile.Launch(direction, EffectiveDamage(damageMultiplier), Data.Ranged.ProjectileSpeed, transform.root, AttackerOwner, TargetLayers, AttackLayer);
        }
    }
}
