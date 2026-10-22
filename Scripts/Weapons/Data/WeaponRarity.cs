namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 무기 정의에 고정되는 4단계 등급이다.
    /// </summary>
    // 스폰마다 바뀌는 값이 아니며, 스탯에는 등급을 반영한 최종값을 직접 저작한다.
    public enum WeaponRarity
    {
        Common,
        Rare,
        Epic,
        Legendary,
    }
}
