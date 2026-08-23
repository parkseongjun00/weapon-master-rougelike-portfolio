using UnityEngine;
using WeaponMaster.Core;
using WeaponMaster.Player;

namespace WeaponMaster.Enemies
{
    /// <summary>
    /// 적이 죽는 즉시 XP가 플레이어에게 직접 지급된다(GDD 6.1/7.1) - XP 획득 오브젝트는 생성되지 않는다.
    /// </summary>
    // HealthComponent와 분리해 둔 이유는, HealthComponent는 플레이어에게도 쓰이는데 플레이어는 죽는다고 XP를 받으면 안 되기 때문이다.
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
