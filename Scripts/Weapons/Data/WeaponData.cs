using System;
using UnityEngine;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 무기 프리팹에 내장되는 정적 데이터다.
    /// </summary>
    [Serializable]
    public class WeaponData
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [Header("Combat")]
        [SerializeField] private float damage = 1f;
        [SerializeField] private float cooldown = 1f;
        [SerializeField] private int maxDurability = 1;

        [Header("Grade (GDD 5.5)")]
        [SerializeField] private WeaponRarity rarity = WeaponRarity.Common;

        [Header("Condition")]
        [SerializeField] private bool hasCondition = true;

        [Header("Collection")]
        [SerializeField] private bool startsDiscovered;

        [Header("Post-MVP (GDD 5.2)")]
        [SerializeField] private float moveSpeedPenalty;

        [SerializeField] private MeleeWeaponData melee = new();
        [SerializeField] private RangedWeaponData ranged = new();

        public string Id => id;
        public string DisplayName => displayName;
        public float Damage => damage;
        public float Cooldown => cooldown;
        public int MaxDurability => maxDurability;
        public WeaponRarity Rarity => rarity;
        public bool HasCondition => hasCondition;
        public bool StartsDiscovered => startsDiscovered;
        public float MoveSpeedPenalty => moveSpeedPenalty;
        public MeleeWeaponData Melee => melee;
        public RangedWeaponData Ranged => ranged;
    }

    /// <summary>
    /// 근접 무기 전용 정적 데이터다.
    /// </summary>
    [Serializable]
    public class MeleeWeaponData
    {
        [SerializeField] private float range = 1f;
        [SerializeField] private float hitRadius = 0.5f;

        public float Range => range;
        public float HitRadius => hitRadius;
    }

    /// <summary>
    /// 원거리 무기 전용 정적 데이터다.
    /// </summary>
    [Serializable]
    public class RangedWeaponData
    {
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private Projectile projectilePrefab;

        public float ProjectileSpeed => projectileSpeed;
        public Projectile ProjectilePrefab => projectilePrefab;
    }
}
