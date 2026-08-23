namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 스폰 인스턴스마다 무작위로 갈리는 개체차 - 등급과 달리 공격력에만 영향을 준다(WeaponConditionData 참고).
    /// </summary>
    // 내구도까지 흔들면 형평성이 너무 크게 벌어지고, 공격속도는 애초에 "개체 편차"라는 컨디션의 성격과 어울리지 않는다고 판단해 제외했다. placeholder 수치 - 밸런스 테스트 중 불공평하게 느껴지면 폐지 검토 대상.
    public enum WeaponCondition
    {
        VeryBad,
        Bad,
        Normal,
        Good,
        VeryGood,
    }
}
