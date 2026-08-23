using System.Collections.Generic;
using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.Enemies
{
    /// <summary>
    /// 플레이어를 향해 추격하되, Boids 분리(separation) 규칙을 더해 무리가 완전히 겹치지 않으면서 모여들게 한다(GDD 6.1).
    /// </summary>
    // 분리 규칙만 구현했다 - 응집(cohesion)/정렬(alignment)은 추격 방향이 이미 모두를 한 지점으로 끌어당기므로 필요하지 않다.
    // 죽었을 때 스포너 풀로 반납하는 것도 여기서 담당한다 - Active 리스트 등록/해제와 같은 "생애주기 관리" 책임이라, XP 지급(EnemyXPReward)과는 분리해뒀다.
    [RequireComponent(typeof(HealthComponent))]
    public class EnemyAI : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3.5f;

        // MVP 단계의 적 수는 매 프레임 이 리스트를 O(n^2)로 순회해도 될 만큼 적어서,
        // 공간 분할 구조보다 이 방식이 더 단순하고 아직은 그 정도로 충분히 저렴하다.
        private static readonly List<EnemyAI> Active = new List<EnemyAI>();

        // 적 타입이 하나뿐이라 인스턴스별로 값이 갈릴 이유가 없어 static으로 공유한다 - 값 하나만 바꾸면 활성 상태인 모든 적에 즉시 반영된다.
        private static float _separationRadius = 1.5f; // 임시값, 추후 조정 대상 - GDD 6.1
        private static float _separationWeight = 1.5f; // 임시값, 추후 조정 대상 - GDD 6.1

        private Transform _target;
        private HealthComponent _health;
        private EnemySpawner _spawner;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
        }

        public void SetTarget(Transform playerTransform)
        {
            _target = playerTransform;
        }

        // EnemySpawner가 스폰 시점에 자기 자신을 건네준다(SetTarget과 동일한 패턴) - 씬 전용 오브젝트인
        // EnemySpawner를 프리팹 쪽이 static Instance 없이도 참조할 수 있게 하는 방법이다.
        public void SetSpawner(EnemySpawner spawner)
        {
            _spawner = spawner;
        }

        private void OnEnable()
        {
            Active.Add(this);
            _health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            Active.Remove(this);
            _health.OnDeath -= HandleDeath;
        }

        // Destroy 대신 스포너의 풀로 반납한다 - 이 호출이 발행하는 EnemySpawner.OnEnemyKilled로 RunRecordManager/AchievementTracker 등이 "적 처치"를 알게 된다.
        private void HandleDeath()
        {
            _spawner?.ReturnEnemy(this);
        }

        private void Update()
        {
            if (!_target) return;

            Vector3 moveDirection = ComputeChaseDirection() + ComputeSeparation() * _separationWeight;
            if (moveDirection.sqrMagnitude < 0.0001f) return;
            moveDirection.Normalize();

            transform.position += moveDirection * (moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        }

        private Vector3 ComputeChaseDirection()
        {
            Vector3 chaseDirection = _target.position - transform.position;
            chaseDirection.y = 0f;
            if (chaseDirection.sqrMagnitude > 0.0001f) chaseDirection.Normalize();
            return chaseDirection;
        }

        // Boids 분리(separation) 규칙: 가까운 다른 적일수록 더 강하게 밀어낸다.
        private Vector3 ComputeSeparation()
        {
            Vector3 separation = Vector3.zero;
            foreach (EnemyAI other in Active)
            {
                if (other == this) continue;

                Vector3 offset = transform.position - other.transform.position;
                offset.y = 0f;
                float distance = offset.magnitude;
                if (distance > 0.0001f && distance < _separationRadius)
                {
                    separation += offset.normalized * (1f - distance / _separationRadius);
                }
            }

            return separation;
        }
    }
}
