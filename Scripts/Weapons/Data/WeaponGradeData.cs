using UnityEngine;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 등급별 틴트 색상 데이터(GDD 5.5) - 하드코딩 분기문이 아니라 배열로 제공한다.
    /// </summary>
    // 스탯에는 등급을 반영한 최종값을 WeaponData에 직접 저작한다.
    public static class WeaponGradeData
    {
        // 최종 아트 적용 전까지 등급을 구분하기 위한 임시 틴트다.
        // WeaponRarity enum 선언 순서(Common, Rare, Epic, Legendary)와 인덱스가 반드시 일치해야 한다.
        private static readonly Color[] TintByRarity =
        {
            Color.white,
            new Color(0.3f, 0.55f, 1f), // 희귀
            new Color(0.65f, 0.25f, 0.95f), // 영웅
            new Color(1f, 0.6f, 0.1f), // 전설
        };

        public static Color GetTintColor(WeaponRarity rarity) => TintByRarity[(int)rarity];
    }
}
