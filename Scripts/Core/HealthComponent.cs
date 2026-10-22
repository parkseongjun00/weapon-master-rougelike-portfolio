using System;
using UnityEngine;

namespace WeaponMaster.Core
{
    /// <summary>
    /// 플레이어와 적이 공유하는 체력/피해/사망 처리 컴포넌트.
    /// </summary>
    // 최대 체력 배율은 외부에서 주입하며, 호출되지 않은 액터는 기본값 1을 유지한다.
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField] private float baseMaxHealth = 100f;
        private float _maxHealthMultiplier = 1f;

        public float MaxHealth => baseMaxHealth * _maxHealthMultiplier;
        public float CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0f;

        public event Action<float, float> OnHealthChanged;
        public event Action<float> OnDamaged;
        public event Action OnDeath;

        private void Awake()
        {
            CurrentHealth = MaxHealth;
        }

        public void SetMaxHealthMultiplier(float multiplier)
        {
            _maxHealthMultiplier = multiplier;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return;

            float previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

            // 오버킬을 제외한 실제 피해량을 발행한다.
            OnDamaged?.Invoke(previousHealth - CurrentHealth);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (CurrentHealth <= 0f)
            {
                OnDeath?.Invoke();
            }
        }

        /// <summary>
        /// 현재 최대 체력까지 완전히 회복한다.
        /// </summary>
        public void FullHeal()
        {
            CurrentHealth = MaxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        /// <summary>
        /// 풀에서 재사용할 수 있도록 체력을 초기화한다.
        /// </summary>
        // 재활성화 시 Awake가 다시 호출되지 않으므로 풀의 호출부가 명시적으로 실행한다.
        public void ResetForReuse()
        {
            CurrentHealth = MaxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }
    }
}
