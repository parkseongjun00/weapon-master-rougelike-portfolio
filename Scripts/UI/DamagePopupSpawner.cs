using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.UI
{
    /// <summary>
    /// HealthComponent.OnDamaged를 데미지 숫자 팝업 표시로 연결한다.
    /// </summary>
    // Systems.md §5 "피격 시각 피드백"이 적/플레이어 공통으로 명시하므로 양쪽 프리팹에 부착한다(Stage1SceneBuilder). Core가 아니라 UI 네임스페이스에 두는 이유: 유일한 의존 대상이 UI 레이어의 DamageNumberPool이라 - Core가 UI를 참조하지 않는 기존 레이어링을 유지한다.
    [RequireComponent(typeof(HealthComponent))]
    public class DamagePopupSpawner : MonoBehaviour
    {
        [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.5f, 0f);

        private HealthComponent _health;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
        }

        private void OnEnable()
        {
            _health.OnDamaged += HandleDamaged;
        }

        private void OnDisable()
        {
            _health.OnDamaged -= HandleDamaged;
        }

        private void HandleDamaged(float amount)
        {
            DamageNumberPool.Instance?.Spawn(transform.position + spawnOffset, Mathf.RoundToInt(amount));
        }
    }
}
