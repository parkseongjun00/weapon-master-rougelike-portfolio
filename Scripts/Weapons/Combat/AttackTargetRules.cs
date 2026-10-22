using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 근접·투사체가 공유하는 공격 대상 제한이다.
    /// </summary>
    public static class AttackTargetRules
    {
        public static bool TryGetTarget(Collider other, Component attacker, LayerMask targetLayers, out IDamageable damageable, out Component owner)
        {
            damageable = null;
            owner = null;
            if ((targetLayers.value & (1 << other.gameObject.layer)) == 0) return false;
            damageable = other.GetComponentInParent<IDamageable>(true);
            owner = damageable as Component;
            return owner && owner != attacker && (!(damageable is HealthComponent health) || health.IsAlive);
        }
    }
}
