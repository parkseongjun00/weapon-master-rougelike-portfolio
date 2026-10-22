using System.Collections.Generic;
using UnityEngine;
using WeaponMaster.Core;
using WeaponMaster.Effects;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 활성 타격 구간에 닿은 적을 공격당 한 번씩 타격하는 근접 무기다.
    /// </summary>
    public class MeleeWeapon : WeaponBase
    {
        public override WeaponCategory Category => WeaponCategory.Melee;

        private enum MeleeAttackPhase
        {
            Idle,
            WaitingForHitbox,
            HitboxActive,
            Recovering,
        }

        [SerializeField, InspectorName("Attack Collider")] private Collider hitbox;
        [SerializeField] private WeaponTrailEffect trailEffect;

        /// <summary>
        /// 장착 무기의 근접 공격에 사용하는 전용 콜라이더다.
        /// </summary>
        public Collider AttackCollider => hitbox;

        private MeleeAttackPhase _attackPhase;
        private float _currentAttackDamage;
        private Collider _activeHitbox;
        private Component _attackerDamageableOwner;
        private readonly HashSet<Component> _hitTargets = new();
        public override bool IsAttacking => _attackPhase != MeleeAttackPhase.Idle;

        protected override void StartAttack(Vector3 originPosition, Vector3 aimDirection, float damageMultiplier)
        {
            _attackPhase = MeleeAttackPhase.WaitingForHitbox;
        }

        protected override bool CanStartAttack() => _attackPhase == MeleeAttackPhase.Idle;

        public override void EnableMeleeHitbox(Vector3 originPosition, Vector3 aimDirection, float damageMultiplier)
        {
            if (_attackPhase != MeleeAttackPhase.WaitingForHitbox || !hitbox) return;

            ActivateHitbox(hitbox, damageMultiplier);
            if (trailEffect) trailEffect.Begin();
        }

        /// <summary>
        /// 외부 Trigger Collider로 근접 공격의 활성 타격 구간을 시작한다.
        /// </summary>
        public void EnableFistHitbox(Collider externalHitbox, float damageMultiplier)
        {
            if (_attackPhase != MeleeAttackPhase.WaitingForHitbox || !externalHitbox) return;

            ActivateHitbox(externalHitbox, damageMultiplier);
        }

        /// <summary>
        /// 지정한 외부 Trigger Collider가 활성 상태일 때만 타격 구간을 끝낸다.
        /// </summary>
        public void DisableFistHitbox(Collider externalHitbox)
        {
            if (_activeHitbox != externalHitbox) return;

            DisableMeleeHitbox();
        }

        public override void DisableMeleeHitbox()
        {
            if (_attackPhase != MeleeAttackPhase.HitboxActive) return;

            _attackPhase = MeleeAttackPhase.Recovering;
            if (_activeHitbox) _activeHitbox.enabled = false;
            _activeHitbox = null;
            if (trailEffect) trailEffect.Stop();

            if (_hitTargets.Count > 0) Durability.Consume();
        }

        /// <summary>
        /// 진행 상태와 관계없이 근접 공격을 끝낸다.
        /// </summary>
        public override void EndMeleeAttack()
        {
            if (_attackPhase == MeleeAttackPhase.HitboxActive)
            {
                DisableMeleeHitbox();
            }

            _attackPhase = MeleeAttackPhase.Idle;
        }

        public override void CancelAttack()
        {
            EndMeleeAttack();
            _hitTargets.Clear();
            _attackerDamageableOwner = null;
            if (trailEffect) trailEffect.Clear();
        }

        private void OnDisable() => CancelAttack();

        /// <summary>
        /// 활성 근접 공격에서 접촉한 Collider를 피해 처리한다.
        /// </summary>
        public void ReportHit(Collider other)
        {
            if (_attackPhase != MeleeAttackPhase.HitboxActive) return;

            if (!AttackTargetRules.TryGetTarget(other, _attackerDamageableOwner, TargetLayers, out IDamageable damageable, out Component targetOwner)) return;
            if (!_hitTargets.Add(targetOwner)) return;

            Pose sparkPose = default;
            HitSparkPool sparkPool = HitSparkPool.Instance;
            Vector3 attackDirection = other.bounds.center - (_attackerDamageableOwner
                ? _attackerDamageableOwner.transform.position : transform.position);
            HitElasticReaction reaction = null;
            bool showSpark = false;

            PrepareHitEffects();
            damageable.TakeDamage(_currentAttackDamage);
            PlayHitEffects();

            void PrepareHitEffects()
            {
                bool validDamage = _currentAttackDamage > 0f
                    && damageable is HealthComponent health && health.IsAlive;
                if (validDamage) targetOwner.TryGetComponent(out reaction);
                showSpark = sparkPool && validDamage
                    && targetOwner.TryGetComponent(out HitEffectAnchorSet anchors)
                    && anchors.TryGetPose(_activeHitbox.bounds.center, attackDirection, out sparkPose);
            }

            void PlayHitEffects()
            {
                if (showSpark && sparkPool) sparkPool.Spawn(sparkPose);
                if (reaction) reaction.Play(attackDirection);
            }
        }

        private void ActivateHitbox(Collider attackHitbox, float damageMultiplier)
        {
            _attackerDamageableOwner = AttackerOwner;
            _currentAttackDamage = EffectiveDamage(damageMultiplier);
            _hitTargets.Clear();
            _activeHitbox = attackHitbox;
            if (AttackLayer >= 0) _activeHitbox.gameObject.layer = AttackLayer;
            _attackPhase = MeleeAttackPhase.HitboxActive;
            _activeHitbox.enabled = true;
        }

    }
}
