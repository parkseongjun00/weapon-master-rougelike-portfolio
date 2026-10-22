using System;
using UnityEngine;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 무기 인스턴스의 내구도를 보관하고 소진을 알린다.
    /// </summary>
    // 소모 시점은 무기 유형이 결정하고, 소진 상태와 알림만 이 컴포넌트가 공통 처리한다.
    public class WeaponDurability : MonoBehaviour
    {
        [SerializeField] private int maxDurability = 10;
        [SerializeField] private bool infiniteDurability;

        public int CurrentDurability { get; private set; }

        public event Action OnDepleted;

        private void Awake()
        {
            CurrentDurability = maxDurability;
        }

        /// <summary>
        /// 무기 데이터의 최대 내구도로 현재 상태를 초기화한다.
        /// </summary>
        // WeaponBase가 직접 호출하므로 컴포넌트 간 Awake 실행 순서에 의존하지 않는다.
        public void SetMaxDurability(int value)
        {
            maxDurability = value;
            CurrentDurability = value;
        }

        public void Consume(int amount = 1)
        {
            if (infiniteDurability || CurrentDurability <= 0) return;

            CurrentDurability = Mathf.Max(0, CurrentDurability - amount);
            if (CurrentDurability <= 0)
            {
                OnDepleted?.Invoke();
            }
        }
    }
}
