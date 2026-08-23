namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 등급/희귀도 4단계(GDD 5.5) - 무기 종류(정의) 자체에 고정되는 값이다. 등급 딱지 + 시각적 틴트 + 도감/칭호 분류용으로만 쓰인다.
    /// </summary>
    // 같은 무기를 여러 번 스폰해도 등급은 바뀌지 않는다. 등급에 따른 실제 스탯 수치는 코드가 계산하지 않는다 - `WeaponDefinition`의 damage/cooldown/maxDurability에 이미 등급이 반영된 최종값을 직접 입력한다.
    public enum WeaponRarity
    {
        Common,
        Rare,
        Epic,
        Legendary,
    }
}
