using UnityEngine;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 등급별 틴트 색상 데이터(GDD 5.5) - 하드코딩 분기문이 아니라 배열로 제공한다.
    /// </summary>
    // 등급에 따른 실제 스탯 수치는 여기서 다루지 않는다 - WeaponDefinition의 damage/cooldown/maxDurability 필드에 이미 등급이 반영된 최종값이 직접 들어있다.
    public static class WeaponGradeData
    {
        // placeholder 색상 - 실제 아트 패스(Stage 5) 전까지 등급 구분용 임시 틴트.
        // WeaponRarity enum 선언 순서(Common, Rare, Epic, Legendary)와 인덱스가 반드시 일치해야 한다.
        private static readonly Color[] TintByRarity =
        {
            Color.white,
            new Color(0.3f, 0.55f, 1f), // Rare
            new Color(0.65f, 0.25f, 0.95f), // Epic
            new Color(1f, 0.6f, 0.1f), // Legendary
        };

        public static Color GetTintColor(WeaponRarity rarity) => TintByRarity[(int)rarity];
    }
}
