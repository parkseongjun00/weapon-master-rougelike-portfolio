using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.Enemies
{
    /// <summary>
    /// 접촉 피해는 최초 접촉 시 한 번만 발생하는 게 아니라, 접촉이 유지되는 동안 쿨다운 주기로 반복된다(GDD 6.1).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EnemyContactDamage : MonoBehaviour
    {
        [SerializeField] private float damage = 8f;
        [SerializeField] private float damageCooldown = 1f;

        private float _nextDamageTime;

        private void OnTriggerStay(Collider other)
        {
            if (Time.time < _nextDamageTime) return;

            if (other.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damage);
                _nextDamageTime = Time.time + damageCooldown;
            }
        }
    }
}
