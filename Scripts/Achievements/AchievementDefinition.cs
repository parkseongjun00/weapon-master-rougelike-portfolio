using UnityEngine;

namespace WeaponMaster.Achievements
{
    /// <summary>
    /// 칭호 하나의 정적 데이터. 판정 규칙은 하나뿐이다 - "metric으로 보고된 값이 threshold 이상이면 달성".
    /// </summary>
    // threshold의 단위는 metric마다 다르다(횟수/초/서수) - AchievementMetric 각 값의 주석 참고. 
    // 저장/조회 키는 WeaponDefinition과 동일하게 Object.name(애셋 파일명)을 재사용하므로 별도 id 필드가 없다.
    [CreateAssetMenu(menuName = "Weapon Master/Achievement Definition", fileName = "AchievementDefinition")]
    public class AchievementDefinition : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private AchievementMetric metric;
        [SerializeField] private float threshold;

        public string DisplayName => displayName;
        public string Description => description;
        public AchievementMetric Metric => metric;
        public float Threshold => threshold;
    }
}
