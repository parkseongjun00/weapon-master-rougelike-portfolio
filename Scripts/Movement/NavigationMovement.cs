using UnityEngine;
using UnityEngine.AI;

namespace WeaponMaster.Movement
{
    /// <summary>
    /// 목적지 이동·정지·회전과 내비게이션 상태를 제공한다.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(CapsuleCollider))]
    public class NavigationMovement : MonoBehaviour, IMovementSpeed
    {
        public enum MoveState { Inactive, PathPending, Moving, Unreachable, OffNavMesh }

        [SerializeField, Min(0.01f)] private float destinationSampleDistance = 0.15f;
        [SerializeField, Min(0.02f)] private float repathInterval = 0.2f;
        [SerializeField, Min(0.01f)] private float repathDistance = 0.1f;
        private NavMeshAgent _agent;
        private CapsuleCollider _capsule;
        private Vector3 _requestedGoal;
        private Vector3 _destination;
        private Vector3 _lastPosition;
        private float _nextRepathTime;
        private int _requestFrame;
        private bool _ready;
        private bool _hasDestination;
        private bool _unreachable;

        /// <summary>
        /// 재배치를 제외한 이번 프레임의 실제 수평 이동 속도다.
        /// </summary>
        public float CurrentSpeed { get; private set; }

        /// <summary>
        /// 이번 생애의 이동 준비가 끝났는지 반환한다.
        /// </summary>
        public bool IsReady => _ready && _agent.enabled && _agent.isOnNavMesh;

        /// <summary>
        /// 현재 경로 계산·이동 상태다.
        /// </summary>
        public MoveState State
        {
            get
            {
                if (!_ready || !isActiveAndEnabled) return MoveState.Inactive;
                if (!_agent.enabled || !_agent.isOnNavMesh) return MoveState.OffNavMesh;
                if (_unreachable) return MoveState.Unreachable;
                if (!_hasDestination) return MoveState.Inactive;
                if (_agent.pathPending || Time.frameCount <= _requestFrame) return MoveState.PathPending;
                if (_agent.pathStatus != NavMeshPathStatus.PathComplete) return MoveState.Unreachable;
                if (!_agent.hasPath && HorizontalDistance(transform.position, _destination) > _agent.stoppingDistance + 0.01f)
                    return MoveState.Unreachable;
                return MoveState.Moving;
            }
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _capsule = GetComponent<CapsuleCollider>();
            _agent.updateRotation = false;
        }

        private void OnEnable()
        {
            // 활성화만으로 이전 생애의 경로를 재개하지 않는다.
            _ready = false;
            _hasDestination = false;
            _unreachable = false;
            _agent.enabled = false;
            CurrentSpeed = 0f;
            _lastPosition = transform.position;
        }

        private void OnDisable() => Suspend();

        /// <summary>
        /// 현재 위치에서 이전 경로를 지우고 새 이동 생애를 준비한다.
        /// </summary>
        public bool ResetForReuse()
        {
            Suspend();
            if (!isActiveAndEnabled || !_capsule.enabled || !TryProjectPosition(transform.position, out Vector3 position))
                return false;
            transform.position = position;
            _agent.enabled = true;
            if (!_agent.isOnNavMesh || !_agent.Warp(position))
            {
                _agent.enabled = false;
                return false;
            }
            _ready = true;
            Stop("ReuseReady");
            _lastPosition = transform.position;
            return true;
        }

        /// <summary>
        /// 이동과 회피 참여를 종료한다.
        /// </summary>
        public void Suspend()
        {
            Stop("Suspend");
            _ready = false;
            if (_agent) _agent.enabled = false;
        }

        /// <summary>
        /// 목적지와 작은 보정 범위 안에서 경로를 요청한다.
        /// </summary>
        public void SetDestination(Vector3 destination)
        {
            if (!_ready || !_agent.enabled || !_agent.isOnNavMesh) return;
            bool changed = !_hasDestination || HorizontalDistance(destination, _requestedGoal) >= repathDistance;
            if (!changed || (_hasDestination && Time.time < _nextRepathTime)) return;
            _nextRepathTime = Time.time + repathInterval;
            if (!TryProjectPosition(destination, out Vector3 projected))
            {
                Stop("ProjectionFailed");
                _unreachable = true;
                return;
            }
            _requestedGoal = destination;
            _destination = projected;
            _requestFrame = Time.frameCount;
            _unreachable = false;
            _hasDestination = true;
            _agent.isStopped = false;
            if (!_agent.SetDestination(projected))
            {
                Stop("DestinationRejected");
                _unreachable = true;
            }
        }

        /// <summary>
        /// 경로를 지우고 실제 이동을 정지한다.
        /// </summary>
        public void Stop(string reason = "Caller")
        {
            NavigationTrace.Stop(this, _agent, reason, _hasDestination);
            if (_agent && _agent.enabled && _agent.isOnNavMesh
                && (_hasDestination || !_agent.isStopped || _agent.hasPath))
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
            }
            _hasDestination = false;
            _unreachable = false;
            _nextRepathTime = 0f;
            CurrentSpeed = 0f;
            _lastPosition = transform.position;
        }

        /// <summary>
        /// 완전한 경로로 지정 위치의 허용 거리 안에 도착했는지 반환한다.
        /// </summary>
        public bool HasArrived(Vector3 goal, float tolerance)
        {
            return State == MoveState.Moving
                && HorizontalDistance(transform.position, goal) <= tolerance
                && _agent.remainingDistance <= tolerance;
        }

        /// <summary>
        /// 몸체 발밑의 작은 범위에서 유효한 내비게이션 위치를 찾는다.
        /// </summary>
        public bool TryProjectPosition(Vector3 position, out Vector3 projected)
        {
            // 프리팹에서도 호출하므로 Awake 캐시에 의존하지 않는다.
            var agent = _agent ? _agent : GetComponent<NavMeshAgent>();
            var capsule = _capsule ? _capsule : GetComponent<CapsuleCollider>();
            float footOffset = capsule.center.y * capsule.transform.lossyScale.y
                - capsule.height * Mathf.Abs(capsule.transform.lossyScale.y) * 0.5f;
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (NavMesh.SamplePosition(position + Vector3.up * footOffset, out NavMeshHit hit, destinationSampleDistance, filter)
                && HorizontalDistance(position, hit.position) <= 0.05f)
            {
                projected = hit.position - Vector3.up * footOffset;
                return true;
            }
            projected = default;
            return false;
        }

        /// <summary>
        /// 지정한 수평 방향을 바라본다.
        /// </summary>
        public void Face(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private void LateUpdate()
        {
            Vector3 displacement = transform.position - _lastPosition;
            displacement.y = 0f;
            CurrentSpeed = _ready && Time.deltaTime > 0f ? displacement.magnitude / Time.deltaTime : 0f;
            if (_hasDestination && CurrentSpeed > 0.01f) Face(displacement);
            _lastPosition = transform.position;
        }

        private static float HorizontalDistance(Vector3 from, Vector3 to)
        {
            Vector3 offset = to - from;
            offset.y = 0f;
            return offset.magnitude;
        }
    }
}
