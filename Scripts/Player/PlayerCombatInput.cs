using UnityEngine;
using WeaponMaster.Weapons;

namespace WeaponMaster.Player
{
    /// <summary>
    /// 플레이어의 공격·상호작용 입력을 공통 무기 기능에 전달한다.
    /// </summary>
    public class PlayerCombatInput : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private AimVectorProvider aim;
        [SerializeField] private WeaponController weaponController;
        private WeaponPickup _nearbyPickup;

        private void OnEnable() => input.InteractPerformed += Interact;
        private void OnDisable()
        {
            input.InteractPerformed -= Interact;
            _nearbyPickup = null;
            if (weaponController) weaponController.SetAttackHeld(false);
        }
        private void Update()
        {
            weaponController.SetAttackHeld(input.AttackHeld);
            if (input.AttackHeld) weaponController.TryAttack(aim.AimDirection);
        }
        public void NotifyPickupInRange(WeaponPickup pickup) => _nearbyPickup = pickup;
        public void NotifyPickupOutOfRange(WeaponPickup pickup)
        {
            if (_nearbyPickup == pickup) _nearbyPickup = null;
        }
        private void Interact()
        {
            if (_nearbyPickup && _nearbyPickup.IsAvailable)
            {
                var pickup = _nearbyPickup;
                _nearbyPickup = null;
                if (weaponController.TryEquip(pickup) && pickup.TryGetComponent(out WeaponBase weapon))
                    WeaponCollectionHandler.RecordDiscovery(weapon);
            }
            else weaponController.Drop();
        }
    }
}
