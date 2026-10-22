using UnityEngine;

namespace WeaponMaster.Movement
{
    /// <summary>
    /// 대상의 접근 자리를 예약하고 이동·도착·재시도를 조정한다.
    /// </summary>
    public class SlotFollower : MonoBehaviour
    {
        public enum FollowState { Inactive, Waiting, Approaching, Arrived }

        /// <summary>
        /// 조회한 예약을 식별하는 이동 목표다.
        /// </summary>
        public readonly struct Goal
        {
            internal readonly SurroundSlots.Ticket Ticket;
            public readonly Vector3 Position;
            public readonly int Revision;
            public readonly bool IsAssigned;
            public readonly int RingIndex;

            internal Goal(SurroundSlots.Ticket ticket, Vector3 position, int revision, bool assigned, int ringIndex)
            {
                Ticket = ticket;
                Position = position;
                Revision = revision;
                IsAssigned = assigned;
                RingIndex = ringIndex;
            }
        }

        [SerializeField] private RadiusSource agentRadiusSource = RadiusSource.Auto;
        [SerializeField, Min(0.01f)] private float manualAgentRadius = 0.5f;
        [SerializeField, Min(0.001f)] private float arrivalDistance = 0.1f;
        [SerializeField, Min(0.002f)] private float resumeDistance = 0.2f;
        private SurroundSlots _provider;
        private SurroundSlots.Ticket _ticket;
        private float _joinedRadius;
        private NavigationMovement _movement;
        private float _stalledTime;
        private int _revision;
        private bool _paused;
        public bool IsPaused => _paused;

        public void Pause()
        {
            _paused = true;
            ResetProgress();
            if (_movement) _movement.Stop("FollowPaused");
        }

        public void Resume()
        {
            _paused = false;
            ResetProgress();
        }

        private void Awake() => TryGetComponent(out _movement);

        /// <summary>
        /// 지정한 대상을 슬롯을 이용해 추종한다.
        /// </summary>
        public void Follow(Transform target)
        {
            Begin(target ? target.GetComponent<SurroundSlots>() : null);
        }

        /// <summary>
        /// 추종과 이동을 중단하고 예약을 반환한다.
        /// </summary>
        public void Stop() => StopFollowing();

        /// <summary>
        /// 현재 슬롯 추종 상태다.
        /// </summary>
        public FollowState State { get; private set; }
        /// <summary>
        /// 살아 있는 대상으로 추종 중인지 반환한다.
        /// </summary>
        public bool IsFollowing => isActiveAndEnabled && State != FollowState.Inactive;
        /// <summary>
        /// 목표에 도착한 뒤 바라볼 대상 위치다.
        /// </summary>
        public Vector3 TargetPosition => _provider ? _provider.transform.position : transform.position;

        /// <summary>
        /// 이전 참여를 정리하고 지정한 대상의 슬롯을 추종한다.
        /// </summary>
        public void Begin(SurroundSlots provider)
        {
            StopFollowing();
            if (!isActiveAndEnabled || !provider) return;
            _provider = provider;
            State = FollowState.Waiting;
        }

        /// <summary>
        /// 예약과 대기를 해제하고 추종을 중단한다.
        /// </summary>
        public void StopFollowing()
        {
            _paused = false;
            if (_movement) _movement.Stop("FollowStopped");
            ResetProgress();
            if (_provider && _ticket != null) _provider.Leave(_ticket);
            _ticket = null;
            _provider = null;
            State = FollowState.Inactive;
        }

        private void OnDisable() => StopFollowing();

        private void Update()
        {
            if (!_movement || !_movement.isActiveAndEnabled) return;
            if (Time.deltaTime <= 0f || !RefreshTarget()) { _movement.Stop(Time.deltaTime <= 0f ? "ZeroDeltaTime" : "TargetUnavailable"); return; }
            if (_paused) return;
            if (!_movement.IsReady)
            {
                ReleaseReservation();
                return;
            }
            if (!TryGetGoal(out Goal goal) || !goal.IsAssigned)
            {
                _movement.Stop("NoAssignedGoal");
                ResetProgress();
                return;
            }
            if (_revision != goal.Revision)
            {
                NavigationTrace.Reservation(this, _provider, _revision, goal.Revision, _ticket.Slot, goal.Position);
                _movement.Stop("ReservationChanged");
                ResetProgress();
                _revision = goal.Revision;
            }
            if (State == FollowState.Arrived)
            {
                _movement.Stop("ArrivedHold");
                _movement.Face(TargetPosition - transform.position);
                return;
            }
            _movement.SetDestination(goal.Position);
            NavigationTrace.Sample(this, _provider, _movement, goal.Position, goal.Revision);
            switch (_movement.State)
            {
                case NavigationMovement.MoveState.OffNavMesh:
                    _movement.Stop("OffNavMesh");
                    ReleaseReservation();
                    ResetProgress();
                    return;
                case NavigationMovement.MoveState.Unreachable:
                    ReportBlocked(goal);
                    return;
                case NavigationMovement.MoveState.PathPending:
                case NavigationMovement.MoveState.Inactive:
                    ResetProgress();
                    return;
            }
            UpdateArrival(goal);
            if (State == FollowState.Arrived)
            {
                _movement.Stop("Arrived");
                ResetProgress();
                return;
            }
            // 우회로가 슬롯에서 멀어지는 방향이어도 실제로 이동 중이면 진전으로 취급한다.
            _stalledTime = _movement.CurrentSpeed < 0.05f ? _stalledTime + Time.deltaTime : 0f;
            if (_stalledTime >= 3f) ReportBlocked(goal);
        }

        private void ResetProgress() => _stalledTime = 0f;

        /// <summary>
        /// 현재 예약의 최신 목표를 조회하고 도착 상태를 평가한다.
        /// </summary>
        public bool TryGetGoal(out Goal goal)
        {
            goal = default;
            if (!RefreshTarget()) return false;
            if (!SlotGeometry.TryReadRadius(gameObject, agentRadiusSource, manualAgentRadius, out float radius))
            {
                if (_movement) _movement.Stop("InvalidBodyRadius");
                ReleaseReservation();
                return false;
            }
            if (_ticket == null || !Mathf.Approximately(radius, _joinedRadius))
            {
                if (!_provider.IsAvailable) return false;
                NavigationTrace.Rejoin(this, _joinedRadius, radius, _ticket != null);
                _ticket = _provider.Join(transform, radius);
                _joinedRadius = radius;
                if (_ticket == null) return false;
            }
            var status = _provider.Query(_ticket, out Vector3 position, out int revision);
            if (status == SurroundSlots.SlotStatus.Invalid)
            {
                StopFollowing();
                return false;
            }
            goal = new Goal(_ticket, position, revision, status == SurroundSlots.SlotStatus.Assigned, _provider.RingIndex(_ticket));
            UpdateArrival(goal);
            return true;
        }

        /// <summary>
        /// 예약을 만들지 않고 대상의 추종 가능 상태를 갱신한다.
        /// </summary>
        public bool RefreshTarget()
        {
            if (!IsFollowing) return false;
            if (!_provider || !_provider.isActiveAndEnabled || _provider.IsClosed)
            {
                StopFollowing();
                return false;
            }
            return _provider.IsAvailable;
        }

        /// <summary>
        /// 이동한 뒤 현재 위치로 도착 상태를 갱신한다.
        /// </summary>
        public void UpdateArrival(Goal goal)
        {
            if (!Matches(goal)) return;
            Vector3 offset = goal.Position - transform.position;
            offset.y = 0f;
            if (!goal.IsAssigned) State = FollowState.Waiting;
            else if (State == FollowState.Arrived)
                State = offset.magnitude <= resumeDistance ? FollowState.Arrived : FollowState.Approaching;
            else
                State = _movement && _movement.HasArrived(goal.Position, arrivalDistance)
                    ? FollowState.Arrived : FollowState.Approaching;
        }

        /// <summary>
        /// 접근 실패를 조회 당시 예약에만 적용한다.
        /// </summary>
        public void ReportBlocked(Goal goal)
        {
            if (!Matches(goal) || !goal.IsAssigned) return;
            if (_movement) _movement.Stop("BlockedRetry");
            ResetProgress();
            _provider.Retry(_ticket, goal.Revision);
            State = FollowState.Waiting;
        }

        /// <summary>
        /// 이동할 수 없는 참여자가 자리를 비우고 다시 참여할 준비를 한다.
        /// </summary>
        public void ReleaseReservation()
        {
            if (_movement) _movement.Stop("ReleaseReservation");
            ResetProgress();
            if (_provider && _ticket != null) _provider.Leave(_ticket);
            _ticket = null;
            if (IsFollowing) State = FollowState.Waiting;
        }

        private bool Matches(Goal goal)
        {
            return IsFollowing && _provider && _provider.IsAvailable && _ticket != null
                && goal.Ticket == _ticket && _provider.IsReservationCurrent(_ticket, goal.Revision);
        }

        private void OnValidate()
        {
            manualAgentRadius = SlotGeometry.IsPositive(manualAgentRadius) ? Mathf.Max(0.01f, manualAgentRadius) : 0.5f;
            arrivalDistance = Mathf.Max(0.001f, arrivalDistance);
            resumeDistance = Mathf.Max(arrivalDistance + 0.001f, resumeDistance);
        }
    }
}
