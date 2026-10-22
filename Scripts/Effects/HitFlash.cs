using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.Effects
{
    /// <summary>
    /// 피격 시 모델을 짧게 흰색으로 표시하고 중단 시에도 원래 재질을 복원한다.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private Material flashMaterial;
        [SerializeField, Min(0f)] private float duration = 0.08f;

        private HealthComponent _health;
        private Material[][] _originalMaterials;
        private Material[][] _flashMaterials;
        private float _remaining;
        private bool _flashing;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
            if (targetRenderers == null) return;

            _originalMaterials = new Material[targetRenderers.Length][];
            _flashMaterials = new Material[targetRenderers.Length][];
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                if (!targetRenderers[i]) continue;
                _originalMaterials[i] = targetRenderers[i].sharedMaterials;
                _flashMaterials[i] = new Material[_originalMaterials[i].Length];
                for (int slot = 0; slot < _flashMaterials[i].Length; slot++)
                    _flashMaterials[i][slot] = flashMaterial;
            }
        }

        private void OnEnable()
        {
            _health.OnDamaged += HandleDamaged;
        }

        private void HandleDamaged(float amount)
        {
            // 피해 알림 도중 다른 구독자가 비활성화했을 때도 흰색을 다시 적용하지 않는다.
            if (!isActiveAndEnabled || !flashMaterial || _flashMaterials == null || duration <= 0f) return;

            _remaining = duration;
            if (_flashing) return;
            // 연속 피격 때 흰 재질을 원본으로 다시 저장하지 않는다.
            _flashing = true;
            for (int i = 0; i < targetRenderers.Length; i++)
                if (targetRenderers[i]) targetRenderers[i].sharedMaterials = _flashMaterials[i];
        }

        private void Update()
        {
            if (!_flashing) return;
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f) RestoreMaterials();
        }

        private void RestoreMaterials()
        {
            if (!_flashing) return;
            _flashing = false;
            _remaining = 0f;
            for (int i = 0; i < targetRenderers.Length; i++)
                if (targetRenderers[i]) targetRenderers[i].sharedMaterials = _originalMaterials[i];
        }

        private void OnDisable()
        {
            if (_health) _health.OnDamaged -= HandleDamaged;
            RestoreMaterials();
        }

        private void OnDestroy()
        {
            RestoreMaterials();
        }
    }
}
