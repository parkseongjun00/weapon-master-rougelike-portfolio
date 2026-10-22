using UnityEngine;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 컨디션별 피해 배율과 무작위 선택을 제공한다.
    /// </summary>
    public static class WeaponConditionData
    {
        // WeaponCondition 선언 순서와 인덱스가 반드시 일치한다. 수치는 밸런스 조정 대상이다.
        private static readonly float[] DamageMultiplierByCondition = { 0.9f, 0.95f, 1f, 1.05f, 1.1f };

        public static float GetDamageMultiplier(WeaponCondition condition) => DamageMultiplierByCondition[(int)condition];

        // 배열 길이를 사용해 enum 값 개수와 별도 캐시가 어긋나지 않게 한다.
        public static WeaponCondition RollRandom() => (WeaponCondition)Random.Range(0, DamageMultiplierByCondition.Length);
    }
}
