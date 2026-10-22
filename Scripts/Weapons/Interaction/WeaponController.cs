using System;
using UnityEngine;
using WeaponMaster.Characters;
using WeaponMaster.Core;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 캐릭터의 무기 슬롯과 장착·공격·중단을 관리한다.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        public event Action<WeaponBase> WeaponEquipped;
        public event Action WeaponBecameUnarmed;
        public event Action WeaponDestroyed;

        [SerializeField] private Transform weaponSocket;
        [SerializeField] private MonoBehaviour unarmedWeaponBehaviour;
        [SerializeField] private FistHitbox leftFistHitbox;
        [SerializeField] private FistHitbox rightFistHitbox;
        [SerializeField] private CharacterAnimationController animationController;
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private string attackLayerName = "PlayerAttack";
        [SerializeField] private bool alternateFists = true;
        [SerializeField] private MeleeHitboxSource firstFist = MeleeHitboxSource.LeftFist;
        [SerializeField] private float dropHeight = 0.9f;

        private IWeapon _unarmedWeapon;
        private IWeapon _currentWeapon;
        private IWeapon _activeWeapon;
        private GameObject _currentWeaponObject;
        private WeaponDurability _currentDurability;
        private Component _owner;
        private int _attackLayer;
        private MeleeHitboxSource _activeSource;
        private MeleeHitboxSource _nextFist;
        private Vector3 _attackDirection;
        private float _damageMultiplier = 1f;
        private float _speedMultiplier = 1f;
        private int _cancelFrame = -1;

        public bool IsUnarmed => _currentWeapon == _unarmedWeapon;
        public bool IsAttacking => _currentWeapon?.IsAttacking ?? false;
        public bool CanAttack => isActiveAndEnabled && Time.frameCount > _cancelFrame
            && (_currentWeapon?.CanAttack ?? false) && animationController && animationController.CanAttack;
        public float CurrentAttackInterval => _currentWeapon != null ? _currentWeapon.Cooldown / _speedMultiplier : 0f;

        private void Awake()
        {
            _unarmedWeapon = unarmedWeaponBehaviour as IWeapon;
            _currentWeapon = _unarmedWeapon;
            _owner = GetComponentInParent<IDamageable>(true) as Component;
            _attackLayer = LayerMask.NameToLayer(attackLayerName);
            _nextFist = firstFist;
            leftFistHitbox?.Configure(unarmedWeaponBehaviour as MeleeWeapon);
            rightFistHitbox?.Configure(unarmedWeaponBehaviour as MeleeWeapon);
            DisableFists();
        }

        private void OnEnable()
        {
            if (!animationController) return;
            animationController.HitWindowStarted += OpenHitWindow;
            animationController.HitWindowEnded += CloseHitWindow;
            animationController.AttackEnded += EndAttack;
        }

        private void Start() => SyncEquipment();

        private void OnDisable()
        {
            CancelAttack();
            if (!animationController) return;
            animationController.HitWindowStarted -= OpenHitWindow;
            animationController.HitWindowEnded -= CloseHitWindow;
            animationController.AttackEnded -= EndAttack;
        }

        public void SetAttackDamageMultiplier(float multiplier) => _damageMultiplier = Mathf.Max(0f, multiplier);
        public void SetAttackSpeedMultiplier(float multiplier) => _speedMultiplier = Mathf.Max(0.01f, multiplier);

        /// <summary>
        /// 입력이나 AI가 정한 방향으로 현재 무기의 공격을 요청한다.
        /// </summary>
        public bool TryAttack(Vector3 direction)
        {
            if (!isActiveAndEnabled || Time.frameCount <= _cancelFrame || _currentWeapon == null) return false;
            if (!animationController || !animationController.CanAttack || !weaponSocket) return false;
            var weapon = _currentWeapon;
            var source = IsUnarmed ? _nextFist : MeleeHitboxSource.EquippedWeapon;
            weapon.SetAttackContext(_owner, targetLayers, _attackLayer);
            if (!weapon.TryStartAttack(weaponSocket.position, direction, _damageMultiplier, _speedMultiplier)) return false;
            // 마지막 발사·명중으로 장비가 파괴되면 새 맨손에 이전 공격을 전달하지 않는다.
            if (_currentWeapon != weapon) return true;
            _activeWeapon = weapon;
            _activeSource = source;
            _attackDirection = direction;
            animationController.PlayAttack(weapon.Category, CurrentAttackInterval, source);
            if (IsUnarmed && alternateFists)
                _nextFist = source == MeleeHitboxSource.LeftFist ? MeleeHitboxSource.RightFist : MeleeHitboxSource.LeftFist;
            return true;
        }

        public void SetAttackHeld(bool held)
        {
            if (animationController)
                animationController.SetFiring(held && !IsUnarmed && _currentWeapon?.Category == WeaponCategory.Ranged, CurrentAttackInterval);
        }

        /// <summary>
        /// 획득 가능한 바닥 무기를 단일 슬롯에 장착한다.
        /// </summary>
        public bool TryEquip(WeaponPickup pickup)
        {
            if (!isActiveAndEnabled || !weaponSocket || !pickup || !pickup.TryGetComponent<IWeapon>(out var weapon)) return false;
            if (!pickup.TryClaim(this)) return false;
            Drop();
            CancelAttack();
            AttachWeaponToSocket(pickup.transform, weaponSocket);
            _currentWeaponObject = pickup.gameObject;
            _currentWeapon = weapon;
            _currentDurability = pickup.GetComponent<WeaponDurability>();
            if (_currentDurability) _currentDurability.OnDepleted += HandleWeaponDepleted;
            weapon.SetAttackContext(_owner, targetLayers, _attackLayer);
            SyncEquipment();
            if (pickup.TryGetComponent(out WeaponBase weaponBase)) WeaponEquipped?.Invoke(weaponBase);
            return true;
        }

        public void Drop()
        {
            if (!_currentWeaponObject) return;
            GameObject dropped = _currentWeaponObject;
            CancelAttack();
            // 취소 중 마지막 유효 타격의 내구도가 소진될 수 있다.
            if (_currentWeaponObject != dropped) return;
            ClearEquipment();
            dropped.transform.SetParent(null);
            dropped.transform.position = transform.position + Vector3.up * dropHeight;
            dropped.GetComponent<WeaponPickup>().SetEquipped(false);
        }

        public void CancelAttack()
        {
            _cancelFrame = Time.frameCount;
            if (animationController) animationController.CancelAttack();
            _currentWeapon?.CancelAttack();
            _activeWeapon = null;
            DisableFists();
        }

        public void ResetForReuse()
        {
            CancelAttack();
            _nextFist = firstFist;
            _attackDirection = Vector3.zero;
            _currentWeapon?.ResetForReuse();
            if (animationController) animationController.ResetForReuse();
            SyncEquipment();
        }

        private void OpenHitWindow()
        {
            if (_activeWeapon == null || _activeWeapon != _currentWeapon) return;
            if (_activeSource == MeleeHitboxSource.EquippedWeapon)
                _activeWeapon.EnableMeleeHitbox(weaponSocket.position, _attackDirection, _damageMultiplier);
            else if (_activeWeapon is MeleeWeapon melee)
            {
                FistHitbox fist = _activeSource == MeleeHitboxSource.RightFist ? rightFistHitbox : leftFistHitbox;
                if (fist) melee.EnableFistHitbox(fist.Collider, _damageMultiplier);
            }
        }

        private void CloseHitWindow() => _activeWeapon?.DisableMeleeHitbox();

        private void EndAttack()
        {
            _activeWeapon?.EndMeleeAttack();
            _activeWeapon = null;
            DisableFists();
        }

        private void HandleWeaponDepleted()
        {
            GameObject depleted = _currentWeaponObject;
            WeaponDestroyed?.Invoke();
            CancelAttack();
            ClearEquipment();
            Destroy(depleted);
        }

        private void ClearEquipment()
        {
            if (_currentDurability) _currentDurability.OnDepleted -= HandleWeaponDepleted;
            _currentDurability = null;
            _currentWeaponObject = null;
            _currentWeapon = _unarmedWeapon;
            SyncEquipment();
            WeaponBecameUnarmed?.Invoke();
        }

        private void SyncEquipment()
        {
            if (animationController) animationController.SetEquipment(IsUnarmed);
        }

        private void DisableFists()
        {
            leftFistHitbox?.Deactivate();
            rightFistHitbox?.Deactivate();
        }

        /// <summary>
        /// 그립 포인트를 소켓에 정렬한다.
        /// </summary>
        public static void AttachWeaponToSocket(Transform weapon, Transform socket)
        {
            weapon.SetParent(socket, false);
            Transform grip = weapon.Find("GripPoint");
            if (!grip)
            {
                weapon.localPosition = Vector3.zero;
                weapon.localRotation = Quaternion.identity;
                return;
            }
            weapon.rotation = socket.rotation * Quaternion.Inverse(grip.localRotation);
            weapon.position += socket.position - grip.position;
        }
    }
}
