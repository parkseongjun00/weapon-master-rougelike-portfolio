using WeaponMaster.Movement;
using System;
using System.Collections.Generic;
using UnityEngine;
using WeaponMaster.Arena;
using WeaponMaster.Core;

namespace WeaponMaster.Enemies
{
    /// <summary>
    /// 아레나 경계 근처에서 플레이어와 거리를 두고 적을 스폰한다.
    /// </summary>
    // 스폰 간격은 경과 시간에 따라 선형으로 줄어드는 임시 난이도 곡선이다.
    // 적의 사망·퇴장 완료를 구독해 처치 집계와 풀 반납을 담당한다.
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private ArenaBounds arena;
        [SerializeField] private Transform player;
        [SerializeField] private float spawnInterval = 2.5f;
        [SerializeField] private float minSpawnInterval = 0.5f;
        [SerializeField] private float difficultyRampPerMinute = 0.3f;
        [SerializeField] private float minDistanceFromPlayer = 8f;
        [SerializeField] private int poolPrewarmCount = 10;
        [SerializeField] private LayerMask groundMask = 1 << 11;
        [SerializeField] private LayerMask obstacleMask = 1 << 12;
        [SerializeField] private LayerMask characterMask = (1 << 8) | (1 << 10);

        private float _nextSpawnTime;
        private SimpleObjectPool<EnemyAI> _pool;
        private EnvironmentQuery.Body _body;
        private NavigationMovement _navigationPrefab;
        private readonly HashSet<EnemyAI> _activeEnemies = new HashSet<EnemyAI>();

        /// <summary>
        /// 적 이동에 적용할 맵 경계다.
        /// </summary>
        public ArenaBounds Arena => arena;
        /// <summary>
        /// 몸체를 포개지 않고 배치할 최소 중심 간격이다.
        /// </summary>
        public float SpawnSpacing => _body.Radius * 2f + 0.1f;

        /// <summary>
        /// 적이 사망하면 퇴장 연출 길이와 관계없이 즉시 발생한다.
        /// </summary>
        public event Action OnEnemyKilled;

        /// <summary>
        /// 현재 스폰 대상 플레이어가 살아 있는지 반환한다.
        /// </summary>
        public bool HasLivingTarget => player && player.TryGetComponent(out HealthComponent health) && health.IsAlive;

        private void Awake()
        {
            _body = new EnvironmentQuery.Body(enemyPrefab.GetComponent<CapsuleCollider>());
            _navigationPrefab = enemyPrefab.GetComponent<NavigationMovement>();
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
            if (!arena || !HasLivingTarget) return;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                Vector3 candidate = arena.RandomPerimeterPoint();
                if (Vector3.Distance(candidate, player.position) < minDistanceFromPlayer) continue;
                if (SpawnAt(candidate)) return;
            }
        }

        /// <summary>
        /// 지정한 위치에 풀의 적을 생성하고 전투 상태를 초기화한다.
        /// </summary>
        /// <returns>생성한 적이며, 안전하게 생성할 수 없으면 null이다.</returns>
        public EnemyAI SpawnAt(Vector3 spawnPosition)
        {
            if (_pool == null || !HasLivingTarget || !EnvironmentQuery.IsInside(arena, spawnPosition, _body.Radius)) return null;
            Physics.SyncTransforms();
            // 현재 평면 바닥 근처에서만 높이를 보정하여 다른 층이나 공중 위치를 허용하지 않는다.
            if (!Physics.Raycast(spawnPosition + Vector3.up * 0.25f, Vector3.down, out RaycastHit floor, 0.5f, groundMask, QueryTriggerInteraction.Ignore)) return null;
            spawnPosition.y = floor.point.y + _body.Height * 0.5f - _body.CenterY;
            if (!_navigationPrefab || !_navigationPrefab.TryProjectPosition(spawnPosition, out spawnPosition)
                || !EnvironmentQuery.IsInside(arena, spawnPosition, _body.Radius)
                || !EnvironmentQuery.IsClear(spawnPosition, _body, obstacleMask)
                || !EnvironmentQuery.IsClear(spawnPosition, _body, characterMask, QueryTriggerInteraction.Collide)) return null;
            EnemyAI ai = _pool.Get(spawnPosition, Quaternion.identity);
            PrepareEnemy(ai);
            if (!ai.GetComponent<NavigationMovement>().IsReady)
            {
                ReturnEnemy(ai);
                return null;
            }
            return ai;
        }

        private void PrepareEnemy(EnemyAI enemy)
        {
            _activeEnemies.Add(enemy);
            enemy.Died += HandleEnemyDeath;
            enemy.ExitCompleted += ReturnEnemy;
            enemy.SetTarget(player);

            if (enemy.TryGetComponent(out HealthComponent health))
            {
                health.ResetForReuse();
            }

            // 풀 활성화 직후 같은 프레임에서 전투와 표현 상태를 모두 복원한다.
            enemy.ResetForReuse();
        }

        private void HandleEnemyDeath(EnemyAI enemy)
        {
            OnEnemyKilled?.Invoke();
        }

        public void ReturnEnemy(EnemyAI ai)
        {
            if (!_activeEnemies.Remove(ai)) return;
            UnsubscribeEnemy(ai);
            _pool.Release(ai);
        }

        private void UnsubscribeEnemy(EnemyAI enemy)
        {
            enemy.Died -= HandleEnemyDeath;
            enemy.ExitCompleted -= ReturnEnemy;
        }

        private void OnDestroy()
        {
            foreach (EnemyAI enemy in _activeEnemies)
                if (enemy) UnsubscribeEnemy(enemy);
            _activeEnemies.Clear();
        }
    }
}
