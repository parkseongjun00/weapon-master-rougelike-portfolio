using System;
using UnityEngine;
using WeaponMaster.Player;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 플레이어의 단일 장착 무기 상태 머신 - 현재 들고 있는 무기가 무엇인지, 어떻게 교체되는지(기존 무기 자동 드롭 -> 새 무기 장착, GDD 5.4), 수동 드롭, 그리고 파괴되거나 버려졌을 때 맨손 무기로 되돌아가는 처리까지 담당한다.
    /// </summary>
    public class PlayerWeaponController : MonoBehaviour
    {
        // 칭호 시스템(Achievements)이 구독하는 이벤트 - PlayerWeaponController는 이 이벤트를
        // 누가 듣는지 전혀 모른다(WeaponMaster.Achievements를 참조하지 않음).
        public event Action<WeaponBase> WeaponEquipped;
        public event Action WeaponBecameUnarmed;
        public event Action WeaponDestroyed;

        /// <summary>
        /// 공격이 실제로 발동했을 때(쿨다운을 통과했을 때) 발행된다.
        /// </summary>
        public event Action<WeaponCategory> AttackPerformed;

        [SerializeField] private Transform weaponSocket;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private AimVectorProvider aim;
        [SerializeField] private MonoBehaviour unarmedWeaponBehaviour; // IWeapon을 구현해야 함
        [SerializeField] private float dropHeight = 0.9f; // 버려진 무기가 스폰될 때와 같은, 집을 수 있는 높이를 유지

        private IWeapon _unarmedWeapon;
        private IWeapon _currentWeapon;
        private GameObject _currentWeaponObject;
        private WeaponDurability _currentDurability;

        // 증강 등으로 정해지는 배율 - 장착된 무기 인스턴스가 아니라 여기 저장하는 이유:
        // PlayerWeaponController는 무기가 바뀌어도 안 바뀌는 안정적인 대상이라, 무기 교체마다
        // 다시 밀어넣어줄 필요가 없다.
        private float _attackDamageMultiplier = 1f;
        private float _attackSpeedMultiplier = 1f;

        private void Awake()
        {
            _unarmedWeapon = unarmedWeaponBehaviour as IWeapon;
            _currentWeapon = _unarmedWeapon;
        }

        private void OnEnable()
        {
            input.DropPerformed += HandleManualDrop;
        }

        private void OnDisable()
        {
            input.DropPerformed -= HandleManualDrop;
        }

        private void Update()
        {
            if (input.AttackHeld)
            {
                // 공격은 루트(발 위치)가 아니라 무기를 실제로 쥔 위치에서 시작한다.
                // 그래야 근접 사거리와 투사체 스폰 높이가 적의 발밑을 스치지 않고
                // 대략 몸통 높이에 맞게 된다.
                bool attacked = _currentWeapon?.TryAttack(weaponSocket.position, aim.AimDirection, _attackDamageMultiplier, _attackSpeedMultiplier) ?? false;
                if (attacked)
                {
                    AttackPerformed?.Invoke(_currentWeapon.Category);
                }
            }
        }

        public void SetAttackDamageMultiplier(float multiplier)
        {
            _attackDamageMultiplier = multiplier;
        }

        public void SetAttackSpeedMultiplier(float multiplier)
        {
            _attackSpeedMultiplier = multiplier;
        }

        public void Equip(WeaponPickup pickup)
        {
            DropCurrentWeapon();

            GameObject weaponObject = pickup.gameObject;
            weaponObject.transform.SetParent(weaponSocket, false);
            weaponObject.transform.localPosition = Vector3.zero;
            weaponObject.transform.localRotation = Quaternion.identity;
            pickup.SetEquipped(true);

            _currentWeaponObject = weaponObject;
            _currentWeapon = weaponObject.GetComponent<IWeapon>();
            _currentDurability = weaponObject.GetComponent<WeaponDurability>();

            if (weaponObject.TryGetComponent(out WeaponBase weaponBase))
            {
                WeaponCollectionHandler.RecordDiscovery(weaponBase.Definition);
                WeaponEquipped?.Invoke(weaponBase);
            }

            if (_currentDurability)
            {
                _currentDurability.OnDepleted += HandleWeaponDepleted;
            }
        }

        private void HandleManualDrop()
        {
            DropCurrentWeapon();
        }

        private void DropCurrentWeapon()
        {
            if (!_currentWeaponObject) return;

            GameObject droppedObject = _currentWeaponObject;
            ClearCurrentWeaponState();

            droppedObject.transform.SetParent(null);
            droppedObject.transform.position = transform.position + Vector3.up * dropHeight;
            droppedObject.GetComponent<WeaponPickup>().SetEquipped(false);
        }

        private void HandleWeaponDepleted()
        {
            GameObject depleted = _currentWeaponObject;
            WeaponDestroyed?.Invoke();
            ClearCurrentWeaponState();

            Destroy(depleted);
        }

        // 무기 슬롯을 맨손 상태로 되돌리는 공통 처리 - 수동 드롭과 내구도 소진 둘 다
        // 구독 해제 + 상태 초기화가 동일하므로 한 곳에 모은다. WeaponBecameUnarmed도
        // 여기서 발행해야 한다 - DropCurrentWeapon()에서만 발행하면, 무기가 파괴돼 맨손이
        // 되는 경로(HandleWeaponDepleted)에서는 이 이벤트가 안 뜨는 틈이 생긴다.
        private void ClearCurrentWeaponState()
        {
            if (_currentDurability)
            {
                _currentDurability.OnDepleted -= HandleWeaponDepleted;
            }

            _currentWeaponObject = null;
            _currentDurability = null;
            _currentWeapon = _unarmedWeapon;

            WeaponBecameUnarmed?.Invoke();
        }
    }
}
