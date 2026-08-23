using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.Achievements
{
    /// <summary>
    /// 칭호 로스터를 들고 해금 여부/측정값을 SaveHandler에 저장·조회하며, 값이 갱신될 때마다 로스터를 순회해 threshold를 넘긴 칭호를 직접 해금한다.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        private const string UnlockKeyPrefix = "achievement_unlocked_";
        private const string CounterKeyPrefix = "achievement_counter_";
        private const string BestKeyPrefix = "achievement_best_";

        [SerializeField] private AchievementRoster roster;

        public bool IsUnlocked(AchievementDefinition definition)
        {
            return definition && SaveHandler.GetBool(UnlockKeyPrefix + definition.name);
        }

        private void Unlock(AchievementDefinition definition)
        {
            if (!definition || IsUnlocked(definition)) return;

            SaveHandler.SetBool(UnlockKeyPrefix + definition.name, true);
            Debug.Log($"[AchievementManager] 칭호 해금: {definition.DisplayName}");
        }

        /// <summary>
        /// 평생 누적 카운터(처치수/무기파괴수/플레이횟수/무기장착횟수)를 증가시키고 새 값으로 threshold 판정까지 바로 처리한다.
        /// </summary>
        public void IncrementMetric(AchievementMetric metric, int amount = 1)
        {
            string key = CounterKeyPrefix + metric;
            int newValue = SaveHandler.GetInt(key) + amount;
            SaveHandler.SetInt(key, newValue);

            CheckThreshold(metric, newValue);
        }

        /// <summary>
        /// 순간값(생존시간/맨손시간/컨디션 등급)의 역대 최고 기록을 저장한다 - 새 기록을 세웠을 때만 저장 후 판정한다.
        /// </summary>
        public void UpdateMetric(AchievementMetric metric, float value)
        {
            string key = BestKeyPrefix + metric;
            float best = SaveHandler.GetFloat(key);
            if (value <= best) return;

            SaveHandler.SetFloat(key, value);
            CheckThreshold(metric, value);
        }

        /// <summary>
        /// 평생 누적 카운터의 현재 값을 읽기만 하는 조회 전용 API(디버그 오버레이 등에서 사용).
        /// </summary>
        public int GetCounter(AchievementMetric metric)
        {
            return SaveHandler.GetInt(CounterKeyPrefix + metric);
        }

        /// <summary>
        /// UpdateMetric이 저장한 역대 최고 기록을 읽기만 하는 조회 전용 API.
        /// </summary>
        public float GetMetricBest(AchievementMetric metric)
        {
            return SaveHandler.GetFloat(BestKeyPrefix + metric);
        }

        // 로스터를 아는 유일한 곳 - 이 metric을 쓰는 항목 중 threshold를 넘겼는데 아직 안 열린 걸 찾아 해금한다. 칭호별 분기 코드는 없다.
        private void CheckThreshold(AchievementMetric metric, float value)
        {
            foreach (AchievementDefinition definition in roster.Achievements)
            {
                if (definition.Metric != metric) continue;
                if (IsUnlocked(definition)) continue;
                if (value >= definition.Threshold)
                {
                    Unlock(definition);
                }
            }
        }
    }
}
