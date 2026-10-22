using System;
using System.Collections;
using UnityEngine;
using WeaponMaster.Core;
using WeaponMaster.Movement;
using WeaponMaster.Weapons;
using WeaponMaster.Characters;

namespace WeaponMaster.Enemies
{
    /// <summary>
    /// 적의 대상 지정과 전투·표현·풀 재사용 생애주기를 관리한다.
    /// </summary>
    [RequireComponent(typeof(HealthComponent), typeof(SlotFollower), typeof(NavigationMovement))]
    public class EnemyAI : MonoBehaviour
    {
        [SerializeField] private GameObject healthBarRoot;
        [SerializeField, Min(0.1f)] private float attackDistance = 1.35f;
        private HealthComponent _health;
        private SlotFollower _follower;
        private NavigationMovement _movement;
        private Transform _target;
        private CharacterAnimationController _animation;
        private WeaponController _weapons;
        private HealthComponent _targetHealth;
        private Collider[] _colliders;
        private bool[] _initialColliderStates;
        private bool _isDead;
        private bool _exitCompleted;
        private bool _readyToFollow;

        /// <summary>
        /// 사망으로 전투에서 제외된 직후 발생한다.
        /// </summary>
        public event Action<EnemyAI> Died;

        /// <summary>
        /// 퇴장 표현이 끝나 오브젝트를 회수할 수 있을 때 발생한다.
        /// </summary>
        public event Action<EnemyAI> ExitCompleted;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
            _follower = GetComponent<SlotFollower>();
            _movement = GetComponent<NavigationMovement>();
            _animation = GetComponentInChildren<CharacterAnimationController>(true);
            TryGetComponent(out _weapons);
            CacheInitialColliderStates();
        }

        /// <summary>
        /// 추종할 대상을 지정한다.
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
            _targetHealth = target ? target.GetComponentInParent<HealthComponent>() : null;
            if (_weapons) _weapons.CancelAttack();
            _follower.Stop();
            if (_readyToFollow && !_isDead) _follower.Follow(_target);
        }

        private void OnEnable()
        {
            _health.OnDeath += HandleDeath;
            if (_readyToFollow && !_isDead && !_exitCompleted && _health.IsAlive)
                _follower.Follow(_target);
        }

        private void OnDisable()
        {
            _readyToFollow = false;
            if (_weapons) _weapons.CancelAttack();
            _follower.Stop();
            _movement.Suspend();
            _health.OnDeath -= HandleDeath;
            StopAllCoroutines();
        }

        /// <summary>
        /// 스폰 직후 이전 생애의 전투·표현 상태를 복원한다.
        /// </summary>
        public void ResetForReuse()
        {
            StopAllCoroutines();
            _isDead = false;
            _exitCompleted = false;
            RestoreColliders();
            if (_weapons) _weapons.ResetForReuse();
            if (healthBarRoot) healthBarRoot.SetActive(true);
            _readyToFollow = _movement.ResetForReuse();
            if (_readyToFollow) _follower.Follow(_target);
        }

        // 사망과 퇴장 완료를 알리고, 오브젝트 회수는 구독자가 처리한다.
        private void HandleDeath()
        {
            if (_isDead) return;
            _isDead = true;
            if (_weapons) _weapons.CancelAttack();
            _follower.Stop();
            _movement.Suspend();
            DisableColliders();
            if (healthBarRoot) healthBarRoot.SetActive(false);
            Died?.Invoke(this);
            float exitDuration = _animation ? _animation.PlayExit() : 0f;
            if (exitDuration > 0f) StartCoroutine(ReturnAfterExit(exitDuration));
            else CompleteExit();
        }

        private IEnumerator ReturnAfterExit(float duration)
        {
            // 재생이 중단되면 반납하고,정상 재생도 클립 길이를 대기 상한으로 삼는다.
            while (duration > 0f && _animation && _animation.IsExitPlaying)
            {
                yield return null;
                duration -= Time.deltaTime;
            }
            CompleteExit();
        }

        private void Update()
        {
            if (_isDead || !_readyToFollow || !_weapons) return;
            if (!_target || (_targetHealth && !_targetHealth.IsAlive) || !_follower.RefreshTarget())
            {
                _weapons.CancelAttack();
                _follower.Stop();
                return;
            }
            if (_weapons.IsAttacking) return;
            if (_follower.IsPaused) _follower.Resume();
            if (!_weapons.CanAttack) return;
            Vector3 direction = _target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > attackDistance * attackDistance || direction.sqrMagnitude < 0.0001f) return;
            _follower.Pause();
            _movement.Face(direction);
            if (!_weapons.TryAttack(direction)) _follower.Resume();
        }

        private void CacheInitialColliderStates()
        {
            // 비활성 자식을 포함한 모든 Collider의 최초 활성 상태를 보존한다.
            _colliders = GetComponentsInChildren<Collider>(true);
            _initialColliderStates = new bool[_colliders.Length];
            for (int i = 0; i < _colliders.Length; i++)
                _initialColliderStates[i] = _colliders[i].enabled;
        }

        private void DisableColliders()
        {
            foreach (Collider collider in _colliders)
                collider.enabled = false;
        }

        private void RestoreColliders()
        {
            for (int i = 0; i < _colliders.Length; i++)
                _colliders[i].enabled = _initialColliderStates[i];
        }

        private void CompleteExit()
        {
            if (_exitCompleted) return;
            _exitCompleted = true;
            ExitCompleted?.Invoke(this);
        }
    }
}
