namespace WeaponMaster.Core
{
    /// <summary>
    /// GDD 7.1: 확정된 1차 증강 대상 스탯 목록.
    /// </summary>
    // 내구도 증가는 의도적으로 제외했다(GDD 11-8, 검토 중) - 함부로 추가하지 말 것.
    public enum StatType
    {
        AttackDamage,
        AttackSpeed,
        MoveSpeed,
        MaxHealth,
    }
}
