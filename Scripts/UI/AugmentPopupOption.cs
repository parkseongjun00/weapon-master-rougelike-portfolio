namespace WeaponMaster.UI
{
    /// <summary>
    /// AugmentPopupView가 표시할 후보 하나의 화면 표시용 데이터 - 이름과 "선택하면 도달할 레벨"만 담는다.
    /// </summary>
    // 도메인 타입(AugmentDefinition 등)을 그대로 넘기지 않는 이유: 뷰가 Augments 네임스페이스를 아예 몰라도 되게 하기 위함.
    public readonly struct AugmentPopupOption
    {
        public readonly string DisplayName;
        public readonly int Level;

        public AugmentPopupOption(string displayName, int level)
        {
            DisplayName = displayName;
            Level = level;
        }
    }
}
