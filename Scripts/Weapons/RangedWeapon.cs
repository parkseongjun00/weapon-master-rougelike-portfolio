using UnityEngine;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 원거리 무기 - 발사하는 즉시 내구도를 소모하며, 투사체의 명중 여부와 무관하다(GDD 5.3).
    /// </summary>
    public class RangedWeapon : WeaponBase
    {
        protected override void PerformAttack(Vector3 originPosition, Vector3 aimDirection, float effectiveDamage)
        {
            Durability.Consume();

            Vector3 direction = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : transform.forward;
            Vector3 spawnPosition = originPosition + direction * definition.MuzzleOffset;

            Projectile projectile = ProjectilePool.Instance.Get(definition.ProjectilePrefab, spawnPosition, Quaternion.LookRotation(direction));
            projectile.Init(direction, effectiveDamage, definition.ProjectileSpeed, transform.root);
        }
    }
}
