using UnityEngine;
using WeaponMaster.Weapons;

namespace WeaponMaster.Player
{
    /// <summary>
    /// 게임플레이 상태(이동/무기 장착/공격)를 Animator 파라미터로 변환하는 순수 브릿지.
    /// </summary>
    // PlayerMovement/PlayerWeaponController는 이 클래스의 존재를 모른다 - 이 클래스가 반대로
    // 그쪽을 구독한다(증강 배율 개편, DevNotes.md §2.16과 동일한 방향: 핵심 시스템이 부가
    // 시스템을 모르게 한다).
    public class PlayerAnimationController : MonoBehaviour
    {
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int IsUnarmedParam = Animator.StringToHash("IsUnarmed");
        private static readonly int IsFiringParam = Animator.StringToHash("IsFiring");
        private static readonly int AttackTriggerParam = Animator.StringToHash("AttackTrigger");
        private static readonly int AttackVariantParam = Animator.StringToHash("AttackVariant");

        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerWeaponController weaponController;
        [SerializeField] private PlayerInputReader input;

        private bool _isUnarmed = true;
        private bool _isRangedEquipped;
        private int _attackVariant;

        private void OnEnable()
        {
            weaponController.WeaponEquipped += HandleWeaponEquipped;
            weaponController.WeaponBecameUnarmed += HandleWeaponBecameUnarmed;
            weaponController.AttackPerformed += HandleAttackPerformed;
            animator.SetBool(IsUnarmedParam, _isUnarmed); // 시작 상태(맨손) 반영
        }

        private void OnDisable()
        {
            weaponController.WeaponEquipped -= HandleWeaponEquipped;
            weaponController.WeaponBecameUnarmed -= HandleWeaponBecameUnarmed;
            weaponController.AttackPerformed -= HandleAttackPerformed;
        }

        private void Update()
        {
            animator.SetFloat(SpeedParam, movement.CurrentSpeed);
            // Run_and_Shoot 포즈는 원거리 장착 상태에서 공격을 홀드하는 동안만 켠다(이동/정지
            // 공용, DevNotes.md §3.2). 근접/맨손 상태에서 공격 버튼을 눌러도 안 켜진다.
            animator.SetBool(IsFiringParam, _isRangedEquipped && input.AttackHeld);
        }

        private void HandleWeaponEquipped(WeaponBase weapon)
        {
            _isUnarmed = false;
            _isRangedEquipped = weapon.Category == WeaponCategory.Ranged;
            animator.SetBool(IsUnarmedParam, false);
        }

        private void HandleWeaponBecameUnarmed()
        {
            _isUnarmed = true;
            _isRangedEquipped = false;
            animator.SetBool(IsUnarmedParam, true);
        }

        // 맨손 공격은 좌/우 훅을 번갈아 재생한다(Left_Short_Hook_from_Guard/Right_Upper_Hook_from_Guard).
        private void HandleAttackPerformed(WeaponCategory category)
        {
            if (category == WeaponCategory.Ranged) return; // 원거리는 위 IsFiring 블렌드로 처리, 트리거 불필요

            _attackVariant = 1 - _attackVariant;
            animator.SetInteger(AttackVariantParam, _attackVariant);
            animator.SetTrigger(AttackTriggerParam);
        }
    }
}