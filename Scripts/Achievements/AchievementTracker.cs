using UnityEngine;
using WeaponMaster.Core;
using WeaponMaster.Enemies;
using WeaponMaster.Weapons;

namespace WeaponMaster.Achievements
{
    /// <summary>
    /// 이 게임에서 실제로 일어나는 사건(무기 장착/파괴/맨손 전환, 적 처치, 플레이어 사망)을 감지해 AchievementManager에 측정값을 보고하는 유일한 담당자.
    /// </summary>
    // 로스터/threshold 판정은 AchievementManager 몫 - 이 클래스는 "무슨 이벤트가 일어났는지"만 안다.
    // 생존시간/맨손시간처럼 계속 늘어나기만 하는 값은 폴링하지 않고, 구간이 끝나는 시점(사망/재장착)에 최종값만 한 번 확인한다.
    public class AchievementTracker : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponController playerWeaponController;
        [SerializeField] private HealthComponent playerHealth;
        [SerializeField] private RunRecordManager runRecord;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private AchievementManager achievementManager;

        // null이면 무장 상태, 값이 있으면 그 시각부터 맨손이라는 뜻 - bool+float 두 필드 대신
        // nullable 하나로 합쳐서 "무장 중엔 의미 없는 값"이라는 규칙을 타입으로 강제했다.
        private float? _unarmedStreakStart;

        private void OnEnable()
        {
            playerWeaponController.WeaponEquipped += HandleWeaponEquipped;
            playerWeaponController.WeaponBecameUnarmed += HandleWeaponBecameUnarmed;
            playerWeaponController.WeaponDestroyed += HandleWeaponDestroyed;
            playerHealth.OnDeath += HandleRunEnded;
            enemySpawner.OnEnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            playerWeaponController.WeaponEquipped -= HandleWeaponEquipped;
            playerWeaponController.WeaponBecameUnarmed -= HandleWeaponBecameUnarmed;
            playerWeaponController.WeaponDestroyed -= HandleWeaponDestroyed;
            playerHealth.OnDeath -= HandleRunEnded;
            enemySpawner.OnEnemyKilled -= HandleEnemyKilled;
        }

        private void Start()
        {
            achievementManager.IncrementMetric(AchievementMetric.RunPlayedCount);

            // 런은 항상 맨손(UnarmedFists)으로 시작하므로(PlayerWeaponController.Awake) 맨손 연속 시간도 여기서부터 잰다.
            _unarmedStreakStart = Time.time;
        }

        private void HandleWeaponEquipped(WeaponBase weapon)
        {
            if (_unarmedStreakStart.HasValue)
            {
                achievementManager.UpdateMetric(AchievementMetric.UnarmedSeconds, Time.time - _unarmedStreakStart.Value);
                _unarmedStreakStart = null;
            }

            achievementManager.IncrementMetric(AchievementMetric.WeaponEquipCount);
            achievementManager.UpdateMetric(AchievementMetric.WeaponConditionTier, (float)weapon.Condition);
        }

        private void HandleWeaponBecameUnarmed()
        {
            _unarmedStreakStart = Time.time;
        }

        private void HandleWeaponDestroyed()
        {
            achievementManager.IncrementMetric(AchievementMetric.WeaponDestroyedCount);
        }

        private void HandleEnemyKilled()
        {
            achievementManager.IncrementMetric(AchievementMetric.EnemyKillCount);
        }

        private void HandleRunEnded()
        {
            float survivalTime = runRecord.SurvivalTime;
            achievementManager.UpdateMetric(AchievementMetric.SurvivalSeconds, survivalTime);

            if (_unarmedStreakStart.HasValue)
            {
                achievementManager.UpdateMetric(AchievementMetric.UnarmedSeconds, Time.time - _unarmedStreakStart.Value);
            }
        }
    }
}
