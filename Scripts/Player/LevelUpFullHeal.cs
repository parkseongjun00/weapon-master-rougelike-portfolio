using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.Player
{
    /// <summary>
    /// 레벨업하면 플레이어 체력을 완전 회복시킨다(GDD 7.1 - 어떤 증강을 골랐든 무관하게 적용).
    /// </summary>
    // 증강 선택 UI 흐름과 무관한 별개 반응이라 AugmentSelectionManager에서 분리했다 - 이 컴포넌트만 떼면 팝업 흐름은 그대로 두고 회복 보상만 없앨 수 있다(DevNotes.md §3단계 참고).
    public class LevelUpFullHeal : MonoBehaviour
    {
        [SerializeField] private PlayerXP playerXP;
        [SerializeField] private HealthComponent playerHealth;

        private void OnEnable()
        {
            playerXP.OnLevelUp += HandleLevelUp;
        }

        private void OnDisable()
        {
            playerXP.OnLevelUp -= HandleLevelUp;
        }

        private void HandleLevelUp()
        {
            playerHealth.FullHeal();
        }
    }
}
