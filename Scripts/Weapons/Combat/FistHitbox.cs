using UnityEngine;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 손 Trigger Collider의 접촉을 맨손 근접 무기에 전달한다.
    /// </summary>
    public class FistHitbox : MonoBehaviour
    {
        [SerializeField] private Collider hitbox;

        private MeleeWeapon _meleeWeapon;

        /// <summary>
        /// 손 타격 판정에 사용하는 Trigger Collider다.
        /// </summary>
        public Collider Collider => hitbox ? hitbox : GetComponent<Collider>();

        /// <summary>
        /// 접촉을 전달할 맨손 근접 무기를 연결한다.
        /// </summary>
        public void Configure(MeleeWeapon meleeWeapon)
        {
            _meleeWeapon = meleeWeapon;
        }

        /// <summary>
        /// 손 타격 판정을 즉시 비활성화한다.
        /// </summary>
        public void Deactivate()
        {
            if (Collider) Collider.enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            _meleeWeapon?.ReportHit(other);
        }

        private void OnTriggerStay(Collider other)
        {
            _meleeWeapon?.ReportHit(other);
        }
    }
}
