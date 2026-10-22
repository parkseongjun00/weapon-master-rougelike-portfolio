using System;
using UnityEngine;

namespace WeaponMaster.Player
{
    /// <summary>
    /// 적 처치로 획득한 XP를 누적하고 레벨업 이벤트를 발생시킨다.
    /// </summary>
    // 증강 선택은 별도 시스템이 구독하며, 단일 플레이어 인스턴스는 씬 재시작 때 함께 초기화된다.
    public class PlayerXP : MonoBehaviour
    {
        // 임시 레벨링 곡선으로, 다음 임계값은 레벨마다 50% 증가한다.
        private const float InitialLevelThreshold = 50f;
        private const float LevelThresholdGrowth = 1.5f;

        private float _nextLevelThreshold = InitialLevelThreshold;
        private float _currentLevelStartXp;

        public static PlayerXP Instance { get; private set; }

        public int TotalXP { get; private set; }
        public int CurrentLevel { get; private set; } = 1;

        // HUD의 XP 게이지가 "이번 레벨 안에서의 진행률"을 계산하는 데 쓴다:
        // (TotalXP - CurrentLevelStartXP) / (NextLevelThreshold - CurrentLevelStartXP)
        public float NextLevelThreshold => _nextLevelThreshold;
        public float CurrentLevelStartXP => _currentLevelStartXp;

        public event Action<int> OnXPChanged;
        public event Action OnLevelUp;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void AddXP(int amount)
        {
            if (amount <= 0) return;

            TotalXP += amount;
            OnXPChanged?.Invoke(TotalXP);

            // 여러 임계값을 한 번에 넘어도 레벨마다 이벤트를 한 번씩 발생시킨다.
            while (TotalXP >= _nextLevelThreshold)
            {
                CurrentLevel++;
                _currentLevelStartXp = _nextLevelThreshold;
                _nextLevelThreshold += _nextLevelThreshold * (LevelThresholdGrowth - 1f);
                OnLevelUp?.Invoke();
            }
        }
    }
}
