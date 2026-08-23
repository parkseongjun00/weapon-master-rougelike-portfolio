using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WeaponMaster.Player;

namespace WeaponMaster.UI
{
    /// <summary>
    /// PlayerXP의 레벨/진행률을 HUD에 반영한다.
    /// </summary>
    // RunRecordUI가 다루는 생존 시간과 달리 XP는 적을 처치할 때만 바뀌므로 폴링 대신 이벤트 구독을 쓴다(PlayerXP.Instance가 아닌 직렬화된 참조로 구독). OnLevelUp도 함께 구독하는 이유: OnXPChanged는 레벨업 임계값 갱신 "이전"에 발생해서(PlayerXP.AddXP), 그 시점에만 갱신하면 진행률이 순간적으로 1.0을 넘어 보일 수 있다.
    public class PlayerXPUI : MonoBehaviour
    {
        [SerializeField] private PlayerXP playerXP;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Slider xpSlider;

        private void OnEnable()
        {
            playerXP.OnXPChanged += HandleXPChanged;
            playerXP.OnLevelUp += HandleLevelUp;
            Refresh();
        }

        private void OnDisable()
        {
            playerXP.OnXPChanged -= HandleXPChanged;
            playerXP.OnLevelUp -= HandleLevelUp;
        }

        private void HandleXPChanged(int totalXP) => Refresh();

        private void HandleLevelUp() => Refresh();

        private void Refresh()
        {
            levelText.text = $"Lv. {playerXP.CurrentLevel}";

            float levelSpan = playerXP.NextLevelThreshold - playerXP.CurrentLevelStartXP;
            float progress = levelSpan > 0f ? (playerXP.TotalXP - playerXP.CurrentLevelStartXP) / levelSpan : 0f;
            xpSlider.value = Mathf.Clamp01(progress);
        }
    }
}
