using UnityEngine;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 정적인 무기 스탯 데이터. damage/cooldown/maxDurability는 이미 등급(rarity)이 반영된 최종값이다.
    /// </summary>
    // 두 카테고리를 SO 서브클래스 두 개로 나누지 않고 category enum 하나로 커버한다 - GDD 5.1은 무기 카테고리가 코드가 아니라 데이터여야 한다고 요구하며, 3번째 카테고리가 추가되더라도 새 enum 값/필드만 있으면 된다. moveSpeedPenalty는 post-MVP 스탯이라(GDD 5.2, 정식 채택·후순위) Stage 4에서 SO 형태가 다시 바뀌지 않도록 지금 정의해 둔다 - 그때까지는 사용되지 않는다.
    // 등급별 배율을 코드가 계산하지 않는다. 사람이 직접 밸런싱하며 입력하거나, 추후 외부 도구가 계산해 이 필드에 주입하는 방식을 쓴다.
    [CreateAssetMenu(menuName = "Weapon Master/Weapon Definition", fileName = "WeaponDefinition")]
    public class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private WeaponCategory category;
        [SerializeField] private float damage;
        [SerializeField] private float cooldown;
        [SerializeField] private int maxDurability;

        [Header("Grade (GDD 5.5)")]
        [SerializeField] private WeaponRarity rarity = WeaponRarity.Common;

        [Header("Condition - 스폰마다 무작위로 갈리는 개체차(WeaponBase가 자체적으로 굴림)")]
        [SerializeField] private bool hasCondition = true; // 맨손 등 컨디션 대상에서 제외할 무기는 false로 설정

        [Header("Collection (도감) - WeaponRoster.IsDiscovered가 참고")]
        [SerializeField] private bool startsDiscovered; // 맨손처럼 PlayerWeaponController.Equip()을 거치지 않아 발견 기록이 남을 수 없는 무기는 true로 설정

        [Header("Melee only")]
        [SerializeField] private float range;
        [SerializeField] private float hitRadius;

        [Header("Ranged only")]
        [SerializeField] private float projectileSpeed;
        [SerializeField] private float muzzleOffset;
        [SerializeField] private Projectile projectilePrefab;

        [Header("Post-MVP (GDD 5.2) - unused until Stage 4")]
        [SerializeField] private float moveSpeedPenalty;

        public string DisplayName => displayName;
        public WeaponCategory Category => category;
        public float Damage => damage;
        public float Cooldown => cooldown;
        public int MaxDurability => maxDurability;
        public WeaponRarity Rarity => rarity;
        public bool HasCondition => hasCondition;
        public bool StartsDiscovered => startsDiscovered;
        public float Range => range;
        public float HitRadius => hitRadius;
        public float ProjectileSpeed => projectileSpeed;
        public float MuzzleOffset => muzzleOffset;
        public Projectile ProjectilePrefab => projectilePrefab;
        public float MoveSpeedPenalty => moveSpeedPenalty;
    }
}
