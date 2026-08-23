namespace WeaponMaster.Achievements
{
    /// <summary>
    /// 칭호(Achievement)가 "무엇을 측정해서" 달성 여부를 판단하는지 나열한다.
    /// </summary>
    // 칭호가 늘어도 이 목록은 그대로 - 새 칭호는 기존 metric을 재사용하고 threshold만 다르게 추가된다.
    public enum AchievementMetric
    {
        WeaponEquipCount,     // 평생 누적: 무기 장착 횟수
        EnemyKillCount,       // 평생 누적: 처치 수
        WeaponDestroyedCount, // 평생 누적: 무기 파괴(내구도 소진) 횟수
        RunPlayedCount,       // 평생 누적: 플레이한 런 횟수
        SurvivalSeconds,      // 단일 런: 생존 시간(초), 사망 시점에 최종값 확인
        UnarmedSeconds,       // 단일 런: 맨손 연속 유지 시간(초), 구간이 끝나는 시점에 확인
        WeaponConditionTier,  // 순간값: 장착한 무기의 컨디션 등급(서수)
    }
}
