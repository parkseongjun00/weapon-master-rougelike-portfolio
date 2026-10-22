using UnityEngine;
using WeaponMaster.Core;
using WeaponMaster.Player;

namespace WeaponMaster.Enemies
{
    /// <summary>
    /// 적이 죽으면 월드 획득물 없이 XP를 플레이어에게 직접 지급한다.
    /// </summary>
    // 플레이어도 사용하는 HealthComponent에 적 전용 보상을 섞지 않는다.
    [RequireComponent(typeof(HealthComponent))]
    public class EnemyXPReward : MonoBehaviour
    {
        [SerializeField] private int xpValue = 10;

        private HealthComponent _health;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
        }

        private void OnEnable()
        {
            _health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            _health.OnDeath -= HandleDeath;
        }

        private void HandleDeath()
        {
            if (PlayerXP.Instance)
            {
                PlayerXP.Instance.AddXP(xpValue);
            }
        }
    }
}
