namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 컨디션별 공격력 배율 데이터 - 하드코딩 분기문이 아니라 배열로 제공한다.
    /// </summary>
    // 컨디션은 공격력에만 영향을 주므로(WeaponCondition.cs 참고), 등급과 달리 카테고리별 비중 분배가 필요 없다 - 컨디션 값 자체가 곧 공격력 배율이다.
    public static class WeaponConditionData
    {
        // WeaponCondition enum 선언 순서(VeryBad, Bad, Normal, Good, VeryGood)와 인덱스가
        // 반드시 일치해야 한다. placeholder 수치 - 밸런스 테스트하며 불공평하다고 느껴지면
        // 폐지 검토 대상.
        private static readonly float[] DamageMultiplierByCondition = { 0.9f, 0.95f, 1f, 1.05f, 1.1f };

        public static float GetDamageMultiplier(WeaponCondition condition) => DamageMultiplierByCondition[(int)condition];
    }
}
