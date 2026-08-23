using System.Collections.Generic;
using UnityEngine;
using WeaponMaster.Core;
using WeaponMaster.Player;
using WeaponMaster.Weapons;

namespace WeaponMaster.Augments
{
    /// <summary>
    /// 플레이어가 실제로 선택한 증강만 보관하고, 레벨업 때마다 배율을 실제 소비처(PlayerWeaponController/PlayerMovement/HealthComponent)에 직접 밀어넣는다.
    /// </summary>
    // 딕셔너리 키는 AugmentDefinition 자체(WeaponDefinition/AchievementDefinition과 동일한 식별 방식) - StatType을 키로 쓰지 않으므로 "스탯 하나에 증강 하나" 제약이 없다(DevNotes.md §3단계 참고).
    public class PlayerAugmentManager : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponController playerWeaponController;
        [SerializeField] private HealthComponent healthComponent;
        [SerializeField] private PlayerMovement playerMovement;

        // 선택된 증강만 담는다 - 아직 한 번도 안 뽑힌 증강은 여기 없고, GetCurrentLevel이 0으로 대신 답한다.
        private readonly Dictionary<AugmentDefinition, AugmentInstance> _selected = new();

        public int GetCurrentLevel(AugmentDefinition definition)
        {
            return _selected.TryGetValue(definition, out AugmentInstance instance) ? instance.CurrentLevel : 0;
        }

        public void LevelUp(AugmentDefinition definition)
        {
            if (!_selected.TryGetValue(definition, out AugmentInstance instance))
            {
                instance = new AugmentInstance(definition);
                _selected[definition] = instance;
            }

            instance.LevelUp();
            ApplyEffect(definition);
        }

        // 카테고리별로 다른 곳에 적용한다 - 카테고리가 늘어나면 여기 case만 추가하면 된다.
        private void ApplyEffect(AugmentDefinition definition)
        {
            switch (definition.Category)
            {
                case AugmentCategory.StatBoost:
                    PushCombinedStatMultiplier(definition.StatType);
                    break;
            }
        }

        // 같은 StatType을 겨냥하는 StatBoost 증강이 여러 개 선택돼 있을 수 있어, 전부 곱해 합산한 뒤 푸시한다.
        // 지금은 스탯당 증강이 하나뿐이라 결과가 그 증강의 배율과 같지만, 나중에 늘어나도 이 로직은 그대로 맞는다.
        private void PushCombinedStatMultiplier(StatType statType)
        {
            float combined = 1f;
            foreach (AugmentInstance instance in _selected.Values)
            {
                if (instance.Definition.Category == AugmentCategory.StatBoost && instance.Definition.StatType == statType)
                {
                    combined *= instance.CurrentMultiplier;
                }
            }

            switch (statType)
            {
                case StatType.AttackDamage:
                    playerWeaponController.SetAttackDamageMultiplier(combined);
                    break;
                case StatType.AttackSpeed:
                    playerWeaponController.SetAttackSpeedMultiplier(combined);
                    break;
                case StatType.MoveSpeed:
                    playerMovement.SetSpeedMultiplier(combined);
                    break;
                case StatType.MaxHealth:
                    healthComponent.SetMaxHealthMultiplier(combined);
                    break;
            }
        }
    }
}
