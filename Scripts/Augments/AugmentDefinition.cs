using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.Augments
{
    /// <summary>
    /// 증강별 정적 데이터. bonusPerLevel[i]는 레벨 i+1에서의 *최종* 배율이다(레벨당 증분값이 아니라).
    /// </summary>
    // 이 방식 덕분에 스택 공식 없이도 증강마다 다른 커브(1레벨에 크게/점진적으로 등)를 가질 수 있다.
    [CreateAssetMenu(menuName = "Weapon Master/Augment Definition", fileName = "AugmentDefinition")]
    public class AugmentDefinition : ScriptableObject
    {
        [SerializeField] private AugmentCategory category = AugmentCategory.StatBoost;
        /// <summary>
        /// category가 StatBoost일 때만 의미 있음.
        /// </summary>
        [SerializeField] private StatType statType;
        [SerializeField] private string displayName;
        [SerializeField] private float[] bonusPerLevel;

        public AugmentCategory Category => category;
        public StatType StatType => statType;
        public string DisplayName => displayName;
        public int MaxLevel => bonusPerLevel.Length;

        public float GetMultiplierAtLevel(int level)
        {
            if (level <= 0) return 1f;
            int index = Mathf.Min(level, bonusPerLevel.Length) - 1;
            return bonusPerLevel[index];
        }
    }
}
