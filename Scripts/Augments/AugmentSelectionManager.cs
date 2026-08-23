using System.Collections.Generic;
using UnityEngine;
using WeaponMaster.Player;
using WeaponMaster.UI;

namespace WeaponMaster.Augments
{
    /// <summary>
    /// 레벨업 흐름을 조율한다 - 일시정지, 3택 팝업 표시 요청, 선택 적용, 재개. 증강 카탈로그(roster)를 들고 후보를 뽑지만, 레벨 상태 소유·배율 계산·팝업 렌더링·완전 회복은 소유하지 않는다.
    /// </summary>
    public class AugmentSelectionManager : MonoBehaviour
    {
        private const int MaxShownOptions = 3;

        [SerializeField] private PlayerXP playerXP;
        [SerializeField] private PlayerAugmentManager playerAugments;
        [SerializeField] private AugmentRoster roster;
        [SerializeField] private AugmentPopupView popupView;

        // 아직 안 맥스된 전체 후보(최대 3개보다 많을 수 있음) - 실제 화면엔 이 중 앞의 일부만 보여준다.
        private readonly List<AugmentDefinition> _currentCandidates = new();
        // 한 번의 XP 획득으로 여러 레벨을 동시에 넘을 수 있어 OnLevelUp이 연달아 여러 번 발생할 수 있다.
        // _pendingLevelUps가 이를 큐에 담아 레벨마다 팝업을 순서대로 보여준다.
        private int _pendingLevelUps;

        private void OnEnable()
        {
            playerXP.OnLevelUp += HandleLevelUp;
            popupView.OptionChosen += HandleOptionChosen;
        }

        private void OnDisable()
        {
            playerXP.OnLevelUp -= HandleLevelUp;
            popupView.OptionChosen -= HandleOptionChosen;
        }

        private void HandleLevelUp()
        {
            _pendingLevelUps++;

            // 큐의 첫 레벨업일 때만 일시정지+표시 시작 - 이미 진행 중이면 AdvanceQueue()가 처리 후 다시 ShowPopup()을 부른다.
            if (_pendingLevelUps == 1)
            {
                Time.timeScale = 0f;
                ShowPopup();
            }
        }

        private void ShowPopup()
        {
            _currentCandidates.Clear();
            foreach (AugmentDefinition definition in roster.Augments)
            {
                if (playerAugments.GetCurrentLevel(definition) < definition.MaxLevel)
                {
                    _currentCandidates.Add(definition);
                }
            }

            Shuffle(_currentCandidates);

            if (_currentCandidates.Count == 0)
            {
                // 더 제안할 게 없는 경우(모든 증강 최대 레벨)에도 큐는 그대로 진행한다 - 완전 회복은 LevelUpFullHeal이 별도로 처리.
                AdvanceQueue();
                return;
            }

            int shown = Mathf.Min(MaxShownOptions, _currentCandidates.Count);
            var options = new List<AugmentPopupOption>(shown);
            for (int i = 0; i < shown; i++)
            {
                AugmentDefinition definition = _currentCandidates[i];
                options.Add(new AugmentPopupOption(definition.DisplayName, playerAugments.GetCurrentLevel(definition) + 1));
            }

            popupView.Show(options);
        }

        private void HandleOptionChosen(int index)
        {
            AugmentDefinition chosen = _currentCandidates[index];
            playerAugments.LevelUp(chosen);
            popupView.Hide();

            AdvanceQueue();
        }

        private void AdvanceQueue()
        {
            _pendingLevelUps--;

            if (_pendingLevelUps > 0)
            {
                ShowPopup();
            }
            else
            {
                Time.timeScale = 1f;
            }
        }

        private static void Shuffle(List<AugmentDefinition> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
