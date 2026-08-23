using System;
using UnityEngine;
using WeaponMaster.Arena;
using WeaponMaster.Core;

namespace WeaponMaster.Enemies
{
    /// <summary>
    /// 아레나 경계 근처, 플레이어와 충분히 떨어진 위치에 타이머 기반으로 단순하게 스폰한다(GDD 6.1). 스폰 간격은 경과 시간에 따라 선형으로 줄어드는 임시 난이도 곡선을 사용한다(GDD 6.2/11-5, 정확한 함수는 추후 조정 대상).
    /// </summary>
    // 씬의 모든 적을 관장하는 유일한 지점이라, "적 처치" 통보(OnEnemyKilled)도 여기서 함께 발행한다.
    // 프리팹(EnemyAI)이 이 씬 전용 오브젝트를 참조해야 하는데 직렬화 필드로는 들 수 없어서,
    // static Instance 대신 스폰 시점에 SetTarget과 동일한 방식으로 자기 자신을 주입한다.
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private ArenaBounds arena;
        [SerializeField] private Transform player;
        [SerializeField] private float spawnInterval = 2.5f;
        [SerializeField] private float minSpawnInterval = 0.5f; // 임시값, 추후 조정 대상 - GDD 11-5
        [SerializeField] private float difficultyRampPerMinute = 0.3f; // 임시값, 추후 조정 대상 - GDD 11-5
        [SerializeField] private float minDistanceFromPlayer = 8f;
        [SerializeField] private int poolPrewarmCount = 10;

        private float _nextSpawnTime;
        private SimpleObjectPool<EnemyAI> _pool;

        // 적이 죽어 풀로 반납될 때마다 발행 - ReturnEnemy()가 유일한 발행 지점.
        public event Action OnEnemyKilled;

        private void Awake()
        {
            _pool = new SimpleObjectPool<EnemyAI>(enemyPrefab.GetComponent<EnemyAI>(), transform, poolPrewarmCount);
        }

        private void Update()
        {
            if (Time.time < _nextSpawnTime) return;
            _nextSpawnTime = Time.time + CurrentSpawnInterval();
            SpawnEnemy();
        }

        private float CurrentSpawnInterval()
        {
            float elapsedMinutes = Time.time / 60f;
            float interval = spawnInterval - difficultyRampPerMinute * elapsedMinutes;
            return Mathf.Max(minSpawnInterval, interval);
        }

        private void SpawnEnemy()
        {
            Vector3 spawnPosition = arena.GetRandomEdgePoint(player.position, minDistanceFromPlayer);
            EnemyAI ai = _pool.Get(spawnPosition, Quaternion.identity);
            ai.SetTarget(player);
            ai.SetSpawner(this);

            if (ai.TryGetComponent(out HealthComponent health))
            {
                health.ResetForReuse();
            }
        }

        public void ReturnEnemy(EnemyAI ai)
        {
            _pool.Release(ai);
            OnEnemyKilled?.Invoke();
        }
    }
}
