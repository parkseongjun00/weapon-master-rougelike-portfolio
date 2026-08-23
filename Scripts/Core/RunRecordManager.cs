using UnityEngine;
using WeaponMaster.Enemies;

namespace WeaponMaster.Core
{
    /// <summary>
    /// 이번 런의 생존 시간/킬 수를 추적하고, 사망 시 마지막 런의 값을 SaveHandler에 저장한다.
    /// </summary>
    // RunManager(씬 재시작만 담당)와 분리해 각 컴포넌트가 하나의 역할만 갖도록 했다.
    public class RunRecordManager : MonoBehaviour
    {
        private const string SurvivalTimeKey = "LastRun_SurvivalTime";
        private const string KillCountKey = "LastRun_KillCount";

        [SerializeField] private HealthComponent playerHealth;
        [SerializeField] private EnemySpawner enemySpawner;

        public float SurvivalTime { get; private set; }
        public int KillCount { get; private set; }

        private void OnEnable()
        {
            playerHealth.OnDeath += HandlePlayerDeath;
            enemySpawner.OnEnemyKilled += AddKill;
        }

        private void OnDisable()
        {
            playerHealth.OnDeath -= HandlePlayerDeath;
            enemySpawner.OnEnemyKilled -= AddKill;
        }

        private void Update()
        {
            // Time.timeScale=0(증강 팝업 일시정지) 상태에서는 Time.deltaTime이 이미 0이므로, 별도 처리 없이도 생존 시간이 자연스럽게 멈춘다.
            SurvivalTime += Time.deltaTime;
        }

        private void AddKill()
        {
            KillCount++;
        }

        private void HandlePlayerDeath()
        {
            SaveHandler.SetFloat(SurvivalTimeKey, SurvivalTime);
            SaveHandler.SetInt(KillCountKey, KillCount);
        }
    }
}
