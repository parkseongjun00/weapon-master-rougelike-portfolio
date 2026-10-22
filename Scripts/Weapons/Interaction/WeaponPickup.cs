using UnityEngine;
using WeaponMaster.Player;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 감지 범위 진입/이탈을 알리고 월드·장착 상태에 따라 획득 콜라이더를 전환한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WeaponPickup : MonoBehaviour
    {
        [SerializeField] private Collider pickupCollider;
        [SerializeField] private float repickupGuardTime = 0.3f;

        private float _colliderEnableTime;
        private bool _isEquipped;
        private WeaponController _owner;
        public bool IsAvailable => !_isEquipped && !_owner && pickupCollider && pickupCollider.enabled && Time.time >= _colliderEnableTime;

        public bool TryClaim(WeaponController owner)
        {
            if (!owner || !IsAvailable) return false;
            _owner = owner;
            SetEquipped(true);
            return true;
        }

        /// <summary>
        /// 바닥에서 무기를 감지하는 전용 콜라이더다.
        /// </summary>
        public Collider PickupCollider => pickupCollider;

        // 장착 시 원래 스폰 슬롯을 비우기 위해 WeaponSpawner가 생성 직후 설정한다.
        public WeaponSpawner OwningSpawner { get; set; }
        public int SpawnPointIndex { get; set; } = -1;

        private void OnTriggerEnter(Collider other)
        {
            if (_isEquipped || !pickupCollider || !pickupCollider.enabled) return;
            if (Time.time < _colliderEnableTime) return;

            if (other.TryGetComponent(out PlayerCombatInput controller))
            {
                controller.NotifyPickupInRange(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_isEquipped || !pickupCollider || !pickupCollider.enabled) return;

            if (other.TryGetComponent(out PlayerCombatInput controller))
            {
                controller.NotifyPickupOutOfRange(this);
            }
        }

        public void SetEquipped(bool equipped)
        {
            _isEquipped = equipped;
            if (!equipped) _owner = null;
            pickupCollider.enabled = !equipped;

            if (equipped)
            {
                if (OwningSpawner && SpawnPointIndex >= 0)
                {
                    OwningSpawner.NotifySpawnPointVacated(SpawnPointIndex);
                    SpawnPointIndex = -1;
                }
            }
            else
            {
                // 플레이어가 방금 무기를 버린 위치에서 즉시 다시 주워지는 것을 방지한다.
                _colliderEnableTime = Time.time + repickupGuardTime;
            }
        }
    }
}
