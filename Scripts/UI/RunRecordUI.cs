using TMPro;
using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.UI
{
    /// <summary>
    /// RunRecordManager의 생존 시간/킬 수를 HUD에 반영한다.
    /// </summary>
    // 이벤트를 구독하는 대신 Update()에서 폴링한다 - 생존 시간은 어차피 매 프레임 바뀌므로 여기서는 이벤트를 써도 이득이 없다.
    public class RunRecordUI : MonoBehaviour
    {
        [SerializeField] private RunRecordManager runRecord;
        [SerializeField] private TMP_Text survivalTimeText;
        [SerializeField] private TMP_Text killCountText;

        private void Update()
        {
            int totalSeconds = Mathf.FloorToInt(runRecord.SurvivalTime);
            survivalTimeText.text = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
            killCountText.text = $"Kills: {runRecord.KillCount}";
        }
    }
}
