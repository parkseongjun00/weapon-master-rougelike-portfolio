using UnityEngine;
using UnityEngine.UI;
using WeaponMaster.Core;

namespace WeaponMaster.UI
{
    /// <summary>
    /// HealthComponent의 현재 값을 Slider에 반영한다.
    /// </summary>
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private HealthComponent target;
        [SerializeField] private Slider slider;

        private void OnEnable()
        {
            target.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(target.CurrentHealth, target.MaxHealth);
        }

        private void OnDisable()
        {
            target.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(float current, float max)
        {
            slider.maxValue = max;
            slider.value = current;
        }
    }
}
