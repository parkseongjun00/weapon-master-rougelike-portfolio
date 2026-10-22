using UnityEngine;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 무기 Model의 공격 콜라이더 접촉만 근접 무기에 전달한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WeaponAttackHitbox : MonoBehaviour
    {
        [SerializeField] private MeleeWeapon meleeWeapon;

        /// <summary>
        /// 접촉을 전달받는 근접 무기다.
        /// </summary>
        public MeleeWeapon Weapon => meleeWeapon;

        /// <summary>
        /// 접촉을 전달할 근접 무기를 연결한다.
        /// </summary>
        public void Configure(MeleeWeapon weapon)
        {
            meleeWeapon = weapon;
        }

        private void OnTriggerEnter(Collider other)
        {
            meleeWeapon?.ReportHit(other);
        }

        private void OnTriggerStay(Collider other)
        {
            meleeWeapon?.ReportHit(other);
        }
    }
}
