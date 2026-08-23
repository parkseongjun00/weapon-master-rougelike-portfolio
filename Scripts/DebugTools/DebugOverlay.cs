using System.Linq;
using UnityEngine;
using WeaponMaster.Achievements;
using WeaponMaster.Weapons;

namespace WeaponMaster.DebugTools
{
    /// <summary>
    /// 3단계 전용 일회용 개발자 확인 도구 - 칭호/도감/원본 카운터 상태를 화면에서 바로 확인한다.
    /// </summary>
    // 정식 칭호/도감 화면은 4단계에서야 생기므로, 그때까지 Debug.Log 대신 화면에서 바로 상태를 확인하는 임시 창이다 - 폴리싱 대상 아님. 새 판정 로직은 없고 AchievementRoster/WeaponRoster/AchievementManager를 그대로 순회·조회만 한다.
    public class DebugOverlay : MonoBehaviour
    {
        // 이름을 Debug로 짓지 않은 이유: UnityEngine.Debug와 겹쳐 같은 파일 안에서
        // Debug.Log/Debug.isDebugBuild를 쓸 때 모호해지기 때문(DebugTools로 회피).
        [SerializeField] private AchievementRoster achievementRoster;
        [SerializeField] private WeaponRoster weaponRoster;
        [SerializeField] private AchievementManager achievementManager;

        // 강제 트리거 대상 - 그라인딩이 오래 걸리는 누적 조건 3개.
        private static readonly AchievementMetric[] ForceTriggerMetrics =
        {
            AchievementMetric.EnemyKillCount,
            AchievementMetric.WeaponDestroyedCount,
            AchievementMetric.RunPlayedCount,
        };

        private Vector2 scroll;

        private void OnGUI()
        {
            // 에디터 Play 모드도 isDebugBuild=true라 별도 UNITY_EDITOR 분기가 필요 없다.
            // 전처리기로 컴포넌트 자체를 스트립하지 않는 이유: 실빌드에서도 컴포넌트는
            // 남기고 그림만 안 그려야 씬에 남은 참조가 안 깨진다.
            if (!Debug.isDebugBuild) return;

            GUILayout.BeginArea(new Rect(10, 10, 380, Screen.height - 20), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);

            DrawAchievements();
            GUILayout.Space(10);
            DrawWeaponCodex();
            GUILayout.Space(10);
            DrawRawCounters();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawAchievements()
        {
            GUILayout.Label("=== 칭호 ===");
            foreach (AchievementDefinition definition in achievementRoster.Achievements)
            {
                bool unlocked = achievementManager.IsUnlocked(definition);
                GUILayout.Label($"[{(unlocked ? "O" : "X")}] {definition.DisplayName}");
            }
        }

        private void DrawWeaponCodex()
        {
            GUILayout.Label("=== 무기 도감 ===");
            foreach (WeaponDefinition definition in weaponRoster.Weapons)
            {
                bool discovered = weaponRoster.IsDiscovered(definition);
                GUILayout.Label($"[{(discovered ? "O" : "X")}] {definition.DisplayName}");
            }
        }

        private void DrawRawCounters()
        {
            GUILayout.Label("=== 원본 카운터 ===");
            GUILayout.Label($"WeaponEquipCount: {achievementManager.GetCounter(AchievementMetric.WeaponEquipCount)}");
            GUILayout.Label($"EnemyKillCount: {achievementManager.GetCounter(AchievementMetric.EnemyKillCount)}");
            GUILayout.Label($"WeaponDestroyedCount: {achievementManager.GetCounter(AchievementMetric.WeaponDestroyedCount)}");
            GUILayout.Label($"RunPlayedCount: {achievementManager.GetCounter(AchievementMetric.RunPlayedCount)}");

            GUILayout.Space(6);
            GUILayout.Label("=== 역대 최고 기록 ===");
            GUILayout.Label($"SurvivalSeconds: {achievementManager.GetMetricBest(AchievementMetric.SurvivalSeconds):F1}");
            GUILayout.Label($"UnarmedSeconds: {achievementManager.GetMetricBest(AchievementMetric.UnarmedSeconds):F1}");
            GUILayout.Label($"WeaponConditionTier: {achievementManager.GetMetricBest(AchievementMetric.WeaponConditionTier)}");

            GUILayout.Space(6);
            GUILayout.Label("=== 강제 트리거 ===");
            foreach (AchievementMetric metric in ForceTriggerMetrics)
            {
                if (GUILayout.Button($"{metric} 즉시 달성"))
                {
                    ForceReachThreshold(metric);
                }
            }
        }

        // threshold 숫자를 여기 하드코딩하지 않고 로스터(SO 데이터)에서 읽어온다 - 밸런스
        // 값이 바뀌어도 이 파일을 안 고쳐도 되게 하기 위함. 같은 metric을 쓰는 칭호가
        // 여럿이면(예: EnemyKillCount는 "첫 처치"/"학살자" 둘 다 씀) 가장 큰 threshold를
        // 목표치로 삼아 한 번에 다 해금되게 한다.
        private void ForceReachThreshold(AchievementMetric metric)
        {
            int target = (int)achievementRoster.Achievements
                .Where(definition => definition.Metric == metric)
                .Max(definition => definition.Threshold);

            int deficit = target - achievementManager.GetCounter(metric);
            if (deficit > 0)
            {
                achievementManager.IncrementMetric(metric, deficit);
            }
        }
    }
}
