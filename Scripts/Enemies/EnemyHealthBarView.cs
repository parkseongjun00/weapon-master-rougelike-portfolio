using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.Enemies
{
    /// <summary>
    /// 적 머리 위 체력바 표시. Canvas 대신 SpriteRenderer 2장(배경 + 채움)으로 구현한다.
    /// </summary>
    // URP SRP 배칭으로 다수 적 렌더링 비용을 낮추기 위함. 채움 스프라이트는 배경과 같은 정사각형 스프라이트를 공유하고, localScale.x로 너비를 줄이면서 localPosition.x를 왼쪽 기준으로 보정해 왼쪽 끝은 고정된 채 오른쪽부터 줄어드는 일반적인 체력바처럼 보이게 한다.
    public class EnemyHealthBarView : MonoBehaviour
    {
        [SerializeField] private HealthComponent target;
        [SerializeField] private Transform fillTransform;

        private float _fullWidth;

        private void Awake()
        {
            _fullWidth = fillTransform.localScale.x;
        }

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
            float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            float width = _fullWidth * ratio;

            Vector3 scale = fillTransform.localScale;
            fillTransform.localScale = new Vector3(width, scale.y, scale.z);

            Vector3 position = fillTransform.localPosition;
            fillTransform.localPosition = new Vector3(-(_fullWidth - width) * 0.5f, position.y, position.z);
        }
    }
}
