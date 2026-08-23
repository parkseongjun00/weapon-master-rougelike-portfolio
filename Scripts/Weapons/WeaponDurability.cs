using System;
using UnityEngine;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 무기 인스턴스의 내구도 상태를 들고 있으며, 소모(Consume)와 소진 알림(OnDepleted)까지 직접 담당한다.
    /// </summary>
    // 이름이 "Tracker"였을 때는 수동적으로 관찰만 하는 것처럼 오해하기 쉬워 WeaponDurability로 개명했다. MeleeWeapon/RangedWeapon과 분리해 둔 이유는, 내구도를 *언제* 소모하는지는 무기 종류마다 다르지만(GDD 5.3), *0이 됐을 때 벌어지는 일*은 동일하기 때문이다.
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
        /// WeaponDefinition이 maxDurability를 제공할 때 MeleeWeapon/RangedWeapon.Awake()에서 호출된다.
        /// </summary>
        // 컴포넌트 간 Awake 호출 순서가 보장되지 않는 Unity의 특성에 기대어 직렬화 필드가 먼저 설정됐을 거라 가정하지 않기 위함이다.
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
