using System;
using UnityEngine;
using WeaponMaster.Movement;
using WeaponMaster.Weapons;

namespace WeaponMaster.Characters
{
    /// <summary>
    /// 사람형 캐릭터의 이동·공격·퇴장 모션을 재생한다.
    /// </summary>
    public class CharacterAnimationController : MonoBehaviour
    {
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Unarmed = Animator.StringToHash("IsUnarmed");
        private static readonly int Firing = Animator.StringToHash("IsFiring");
        private static readonly int Attack = Animator.StringToHash("AttackTrigger");
        private static readonly int Variant = Animator.StringToHash("AttackVariant");
        private static readonly int AttackSpeed = Animator.StringToHash("AttackSpeedMultiplier");
        [SerializeField] private Animator animator;
        [SerializeField] private MonoBehaviour movement;
        [SerializeField] private AnimationClip meleeWeaponSwingClip;
        [SerializeField] private AnimationClip unarmedLeftPunchClip;
        [SerializeField] private AnimationClip unarmedRightPunchClip;
        [SerializeField] private AnimationClip rangedFireClip;
        [SerializeField] private AnimationClip idleClip;
        [SerializeField] private AnimationClip walkClip;
        [SerializeField] private AnimationClip runClip;
        [SerializeField] private AnimationClip exitClip;
        [SerializeField, Min(0.01f)] private float movementBlendTime = 0.08f;

        public event Action HitWindowStarted;
        public event Action HitWindowEnded;
        public event Action AttackEnded;

        private IMovementSpeed _movement;
        private AnimatorOverrideController _overrides;
        private int _actionLayer;
        private int _lowerLayer;
        private int _expectedState;
        private int _version;
        private bool _entered;
        private bool _exiting;
        private bool _moving;
        private bool _isUnarmed = true;
        private float _lowerWeight;

        public bool CanAttack => !_exiting && animator && animator.isActiveAndEnabled && animator.runtimeAnimatorController;
        public bool IsExitPlaying => _exiting && animator && animator.isActiveAndEnabled
            && animator.GetCurrentAnimatorStateInfo(0).fullPathHash == Animator.StringToHash("Base Layer.Exit");

        private void Awake()
        {
            _movement = movement as IMovementSpeed;
            _actionLayer = animator.GetLayerIndex("Action");
            _lowerLayer = animator.GetLayerIndex("Lower Body");
            animator.applyRootMotion = false;
            if (idleClip || walkClip || runClip || exitClip)
            {
                _overrides = new AnimatorOverrideController(animator.runtimeAnimatorController);
                if (idleClip) _overrides["Idle"] = idleClip;
                if (walkClip) _overrides["walking"] = walkClip;
                if (runClip) _overrides["running"] = runClip;
                if (exitClip) _overrides["HumanoidExitSlot"] = exitClip;
                animator.runtimeAnimatorController = _overrides;
            }
        }

        private void LateUpdate()
        {
            if (!CanAttack || _exiting) return;
            float speed = _movement?.CurrentSpeed ?? 0f;
            animator.SetFloat(Speed, speed);
            _moving = _moving ? speed > 0.07f : speed > 0.15f;
            _lowerWeight = Mathf.MoveTowards(_lowerWeight, _moving ? 1f : 0f, Time.deltaTime / movementBlendTime);
            if (_lowerLayer >= 0) animator.SetLayerWeight(_lowerLayer, _lowerWeight);
        }

        public void SetEquipment(bool unarmed)
        {
            _isUnarmed = unarmed;
            if (animator) animator.SetBool(Unarmed, unarmed);
        }

        public void SetFiring(bool firing, float interval)
        {
            if (!CanAttack) return;
            animator.SetBool(Firing, firing);
            if (firing && rangedFireClip) SetPlaybackSpeed(rangedFireClip, interval);
        }

        public void PlayAttack(WeaponCategory category, float interval, MeleeHitboxSource source)
        {
            if (!CanAttack || category == WeaponCategory.Ranged) return;
            _version++;
            _entered = false;
            string state = source == MeleeHitboxSource.EquippedWeapon ? "Attack_MeleeWeapon"
                : source == MeleeHitboxSource.RightFist ? "Attack_UnarmedRight" : "Attack_UnarmedLeft";
            _expectedState = Animator.StringToHash("Action." + state);
            AnimationClip clip = source == MeleeHitboxSource.EquippedWeapon ? meleeWeaponSwingClip
                : source == MeleeHitboxSource.RightFist ? unarmedRightPunchClip : unarmedLeftPunchClip;
            animator.SetInteger(Variant, source == MeleeHitboxSource.RightFist ? 1 : 0);
            SetPlaybackSpeed(clip, interval);
            animator.SetTrigger(Attack);
        }

        private void SetPlaybackSpeed(AnimationClip clip, float interval)
        {
            animator.SetFloat(AttackSpeed, clip && interval > 0f ? Mathf.Max(1f, clip.length / interval) : 1f);
        }

        // 이벤트 발신 상태와 현재 공격을 대조해 취소된 상태의 잔여 이벤트를 거른다.
        public void EnableMeleeHitbox(AnimationEvent signal)
        {
            if (Accepts(signal)) HitWindowStarted?.Invoke();
        }
        public void DisableMeleeHitbox(AnimationEvent signal)
        {
            if (Accepts(signal)) HitWindowEnded?.Invoke();
        }
        private bool Accepts(AnimationEvent signal) => _entered && !_exiting && _expectedState != 0
            && signal.animatorStateInfo.fullPathHash == _expectedState;

        public int EnterAttackState(int stateHash)
        {
            if (_expectedState != stateHash || _exiting) return -1;
            _entered = true;
            return _version;
        }
        public void ExitAttackState(int stateHash, int version)
        {
            if (!_entered || _expectedState != stateHash || _version != version) return;
            _entered = false;
            _expectedState = 0;
            AttackEnded?.Invoke();
        }

        public void CancelAttack()
        {
            _version++;
            _entered = false;
            _expectedState = 0;
            if (!animator || !animator.isActiveAndEnabled || !animator.runtimeAnimatorController) return;
            animator.ResetTrigger(Attack);
            animator.SetBool(Firing, false);
            if (_actionLayer >= 0) animator.Play("Locomotion", _actionLayer, 0f);
        }

        public void ResetForReuse()
        {
            CancelAttack();
            _exiting = false;
            _moving = false;
            _lowerWeight = 0f;
            if (!animator || !animator.isActiveAndEnabled) return;
            animator.Rebind();
            animator.SetBool(Unarmed, _isUnarmed);
            animator.SetFloat(AttackSpeed, 1f);
            animator.SetFloat(Speed, 0f);
            if (_actionLayer >= 0) animator.SetLayerWeight(_actionLayer, 1f);
            if (_lowerLayer >= 0) animator.SetLayerWeight(_lowerLayer, 0f);
            animator.Play("Locomotion", 0, 0f);
            animator.Update(0f);
        }

        public float PlayExit()
        {
            CancelAttack();
            _exiting = true;
            if (!exitClip || !animator || !animator.isActiveAndEnabled) return 0f;
            if (_actionLayer >= 0) animator.SetLayerWeight(_actionLayer, 0f);
            if (_lowerLayer >= 0) animator.SetLayerWeight(_lowerLayer, 0f);
            animator.Play("Exit", 0, 0f);
            animator.Update(0f);
            return exitClip.length;
        }

        private void OnDisable()
        {
            _expectedState = 0;
            _entered = false;
        }
        private void OnDestroy()
        {
            if (_overrides) Destroy(_overrides);
        }
    }
}
