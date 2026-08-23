using System;
using UnityEngine;

namespace WeaponMaster.Player
{
    /// <summary>
    /// 적 처치로 획득한 XP를 누적하고 레벨업 이벤트를 발생시킨다.
    /// </summary>
    // OnLevelUp에 반응해 증강을 선택하는 것은 별개 관심사(AugmentSelectionManager)이며, 이 클래스는 증강의 존재 자체를 알지 못한다. 여기서 static 접근은 안전하다: 게임에는 고정된 아레나 하나와 플레이어 하나만 존재하고(GDD), 사망 시 씬을 완전히 다시 로드하면 Awake를 통해 Instance가 자연스럽게 초기화된다.
    public class PlayerXP : MonoBehaviour
    {
        // 임시 레벨링 곡선 - 추후 조정 대상(GDD 11-1). 다음 레벨까지 필요한
        // 누적 XP는 레벨마다 50%씩 증가한다.
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

            // 한 번에 큰 XP를 얻으면 여러 임계값을 동시에 넘을 수 있다 - 대기 중인
            // 증강 팝업이 모두 표시되도록 레벨마다 OnLevelUp을 한 번씩 발생시킨다.
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
