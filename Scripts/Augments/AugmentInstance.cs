namespace WeaponMaster.Augments
{
    /// <summary>
    /// 하나의 증강에 대한 플레이어의 현재 레벨.
    /// </summary>
    // MonoBehaviour가 아닌 순수 C# 클래스 - GameObject가 아니라 PlayerAugmentManager가 소유/보관한다. 레벨 규칙(최대치, 현재 보너스)이 여기 있어 PlayerAugmentManager는 몰라도 된다.
    public class AugmentInstance
    {
        public AugmentDefinition Definition { get; }
        public int CurrentLevel { get; private set; }

        public AugmentInstance(AugmentDefinition definition)
        {
            Definition = definition;
        }

        public bool IsMaxed => CurrentLevel >= Definition.MaxLevel;
        public float CurrentMultiplier => Definition.GetMultiplierAtLevel(CurrentLevel);

        public void LevelUp()
        {
            if (IsMaxed) return;
            CurrentLevel++;
        }
    }
}
