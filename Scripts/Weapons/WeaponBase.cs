using UnityEngine;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// MeleeWeapon/RangedWeapon이 공통으로 가지는 것 - 쿨다운 게이트, 내구도 초기화, 외부에서 받은 배율/컨디션이 반영된 쿨다운/데미지 계산 - 을 한 곳에 모은다.
    /// </summary>
    // 배율이 증강에서 온다는 사실은 모른다 - TryAttack 호출부(PlayerWeaponController)가 값을 실어서 넘겨줄 뿐이다.
    // 등급(Rarity)에 따른 최종 스탯은 여기서 계산하지 않는다 - definition.Damage/Cooldown/MaxDurability 자체가 이미 등급이 반영된 최종값이다. 컨디션(스폰마다 랜덤으로 갈리는 개체차)은 공격력에만 영향을 주며, 스포너가 아니라 이 클래스가 스스로 굴린다(WeaponSpawner는 Condition의 존재를 몰라도 된다).
    [RequireComponent(typeof(WeaponDurability))]
    public abstract class WeaponBase : MonoBehaviour, IWeapon
    {
        [SerializeField] protected WeaponDefinition definition;

        protected WeaponDurability Durability;
        private float _conditionDamageMultiplier = 1f;
        private float _nextAttackTime;

        private static readonly int WeaponConditionValueCount = System.Enum.GetValues(typeof(WeaponCondition)).Length;

        public WeaponDefinition Definition => definition;
        public WeaponCategory Category => definition.Category;
        public WeaponCondition Condition { get; private set; } = WeaponCondition.Normal;

        protected virtual void Awake()
        {
            Durability = GetComponent<WeaponDurability>();
            Durability.SetMaxDurability(definition.MaxDurability);

            if (definition.HasCondition)
            {
                Condition = (WeaponCondition)Random.Range(0, WeaponConditionValueCount);
                _conditionDamageMultiplier = WeaponConditionData.GetDamageMultiplier(Condition);
            }
        }

        public bool TryAttack(Vector3 originPosition, Vector3 aimDirection, float damageMultiplier, float attackSpeedMultiplier)
        {
            if (Time.time < _nextAttackTime) return false;
            _nextAttackTime = Time.time + EffectiveCooldown(attackSpeedMultiplier);

            PerformAttack(originPosition, aimDirection, EffectiveDamage(damageMultiplier));
            return true;
        }

        private float EffectiveCooldown(float attackSpeedMultiplier) => definition.Cooldown / attackSpeedMultiplier;
        private float EffectiveDamage(float damageMultiplier) => definition.Damage * damageMultiplier * _conditionDamageMultiplier;

        // 실제 타격 판정(멀티히트 vs 단일 투사체)은 카테고리마다 다르므로 PerformAttack으로 남긴다.
        protected abstract void PerformAttack(Vector3 originPosition, Vector3 aimDirection, float effectiveDamage);
    }
}
