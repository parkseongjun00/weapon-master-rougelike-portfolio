using UnityEngine;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 모든 무기 유형이 공유하는 쿨다운 게이트와 공격 수치 계산을 제공한다.
    /// </summary>
    [RequireComponent(typeof(WeaponDurability))]
    public abstract class WeaponBase : MonoBehaviour, IWeapon
    {
        [SerializeField] private WeaponData data = new();

        protected WeaponDurability Durability;
        private float _nextAttackAllowedTime;
        protected Component AttackerOwner;
        protected LayerMask TargetLayers = ~0;
        protected int AttackLayer = -1;

        public WeaponData Data => data;
        public abstract WeaponCategory Category { get; }
        public float Cooldown => data.Cooldown;
        public virtual bool IsAttacking => false;
        public bool CanAttack => Time.time >= _nextAttackAllowedTime && CanStartAttack();

        public void SetAttackContext(Component owner, LayerMask targetLayers, int attackLayer)
        {
            AttackerOwner = owner;
            TargetLayers = targetLayers;
            AttackLayer = attackLayer;
        }

        public void ResetForReuse()
        {
            CancelAttack();
            _nextAttackAllowedTime = 0f;
            AttackerOwner = null;
            TargetLayers = 0;
            AttackLayer = -1;
        }

        /// <summary>
        /// 이 무기 인스턴스에 부여된 컨디션이다.
        /// </summary>
        public WeaponCondition Condition { get; private set; } = WeaponCondition.Normal;

        protected virtual void Awake()
        {
            Durability = GetComponent<WeaponDurability>();
            Durability.SetMaxDurability(data.MaxDurability);

            // 컨디션을 사용하는 무기는 인스턴스 생성 시 한 번만 정한다.
            if (data.HasCondition)
            {
                Condition = WeaponConditionData.RollRandom();
            }
        }

        public bool TryStartAttack(Vector3 originPosition, Vector3 aimDirection, float damageMultiplier, float attackSpeedMultiplier)
        {
            if (Time.time < _nextAttackAllowedTime) return false;
            if (!CanStartAttack()) return false;

            _nextAttackAllowedTime = Time.time + EffectiveCooldown(attackSpeedMultiplier);

            StartAttack(originPosition, aimDirection, damageMultiplier);
            return true;
        }

        // 원거리 무기는 이 시점에 발사하고, 근접 무기는 Animation Event까지 대기한다.
        protected virtual void StartAttack(Vector3 originPosition, Vector3 aimDirection, float damageMultiplier)
        {
        }

        // 무기별 공격 진행 상태에 따라 새 공격 시작을 제한한다.
        protected virtual bool CanStartAttack() => true;

        // 원거리는 TryStartAttack에서 즉시 처리하므로 근접 타격 구간 제어는 기본 구현에서 하지 않는다.
        public virtual void EnableMeleeHitbox(Vector3 originPosition, Vector3 aimDirection, float damageMultiplier)
        {
        }

        public virtual void DisableMeleeHitbox()
        {
        }

        /// <summary>
        /// 근접 공격을 끝낸다.
        /// </summary>
        public virtual void EndMeleeAttack()
        {
        }

        public virtual void CancelAttack()
        {
        }

        private float EffectiveCooldown(float attackSpeedMultiplier) => data.Cooldown / attackSpeedMultiplier;

        // 컨디션 보정은 피해에만 적용한다.
        protected float EffectiveDamage(float damageMultiplier) => data.Damage * damageMultiplier * WeaponConditionData.GetDamageMultiplier(Condition);

    }
}
