using UnityEngine;
using WeaponMaster.Player;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 플레이어를 감지해 이 무기를 그들의 PlayerWeaponController에 넘기고, 월드/장착 상태에 따라 콜라이더를 토글한다. 공격 방법이나 내구도 추적은 알지 못한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WeaponPickup : MonoBehaviour
    {
        [SerializeField] private float repickupGuardTime = 0.3f;

        private Collider _pickupCollider;
        private float _colliderEnableTime;

        // 생성 시점에 WeaponSpawner가 설정하며, 이 슬롯이 회수됐을 때 스포너에 알리는 데 쓰인다.
        public WeaponSpawner SourceSpawner { get; set; }
        public int SpawnIndex { get; set; } = -1;

        private void Awake()
        {
            _pickupCollider = GetComponent<Collider>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time < _colliderEnableTime) return;

            if (other.TryGetComponent(out PlayerWeaponController controller))
            {
                controller.Equip(this);
            }
        }

        public void SetEquipped(bool equipped)
        {
            _pickupCollider.enabled = !equipped;

            if (equipped)
            {
                if (SourceSpawner && SpawnIndex >= 0)
                {
                    SourceSpawner.NotifyVacated(SpawnIndex);
                    SpawnIndex = -1;
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
