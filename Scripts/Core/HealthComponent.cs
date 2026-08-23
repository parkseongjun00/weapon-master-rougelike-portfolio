using System;
using UnityEngine;

namespace WeaponMaster.Core
{
    /// <summary>
    /// 플레이어와 적이 공유하는 체력/피해/사망 처리 컴포넌트.
    /// </summary>
    // 액터마다 중복 구현하지 않도록 공유한다.
    // maxHealthMultiplier는 외부(증강 등)에서 SetMaxHealthMultiplier로 밀어넣는 값이고, 이 클래스는 그 값이 어디서 오는지 모른다.
    // Enemy_Basic은 아무도 이 메서드를 안 불러 기본값 1로 남으므로, 플레이어의 MaxHealth 증강이 적에게 새는 걸 자연히 막아준다.
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

            // 오버킬 시 실제로 깎인 양은 amount보다 작을 수 있어 델타를 따로 계산해 발행한다
            // - 데미지 숫자 팝업이 실제 적용된 피해량을 표시해야 해서다.
            OnDamaged?.Invoke(previousHealth - CurrentHealth);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (CurrentHealth <= 0f)
            {
                OnDeath?.Invoke();
            }
        }

        /// <summary>
        /// 레벨업 시 사용된다(GDD 7.1: 어떤 증강을 골랐든 레벨업할 때마다 완전 회복).
        /// MaxHealth도 다시 읽어오므로 방금 고른 MaxHealth 증강이 즉시 반영된다.
        /// </summary>
        public void FullHeal()
        {
            CurrentHealth = MaxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        /// <summary>
        /// 오브젝트 풀에서 재사용할 때 호출한다(Awake는 재활성화 시 다시 불리지 않으므로 낡은 체력값이 남는 문제를 여기서 해결한다).
        /// </summary>
        // 실제 Instantiate 경로를 타는 플레이어는 이 메서드를 호출하지 않으므로 기존 Awake 동작과 호환된다.
        public void ResetForReuse()
        {
            CurrentHealth = MaxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }
    }
}
