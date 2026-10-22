using System;
using System.Collections.Generic;
using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.Movement
{
    /// <summary>
    /// 몸체 크기로 계산한 대상별 다중 링의 자리와 예약을 관리한다.
    /// </summary>
    public class SurroundSlots : MonoBehaviour
    {
        public enum SlotStatus { Invalid, Waiting, Assigned }
        public const int MaxSlots = 128;

        public sealed class Ticket
        {
            internal SurroundSlots Owner;
            internal Transform Requester;
            internal int Epoch;
            internal int Slot = -1;
            internal int Revision;
            internal readonly float[] ExcludedUntil = new float[MaxSlots];
        }

        /// <summary>
        /// 원의 반경·수용량과 평면 슬롯 범위다.
        /// </summary>
        public readonly struct Ring
        {
            public readonly float Radius;
            public readonly int Start;
            public readonly int Count;
            internal Ring(float radius, int start, int count) { Radius = radius; Start = start; Count = count; }
        }

        private struct Slot
        {
            internal Vector3 Offset;
            internal int Ring;
        }

        [SerializeField] private RadiusSource targetRadiusSource = RadiusSource.Auto;
        [SerializeField, Min(0.01f)] private float manualTargetRadius = 0.5f;
        [SerializeField, Min(0.001f)] private float spacing = 0.1f;
        [SerializeField, Min(0f)] private float contactInset = 0.15f;
        [SerializeField, Range(1, 16)] private int maxRings = 5;
        [SerializeField, Min(0.02f)] private float refreshInterval = 0.2f;
#if UNITY_EDITOR
        [SerializeField, HideInInspector, Min(0.01f)] private float previewAgentRadius = 0.5f;
#endif
        private readonly List<Ticket> _participants = new List<Ticket>();
        private readonly List<Ring> _rings = new List<Ring>(16);
        private readonly Slot[] _slots = new Slot[MaxSlots];
        private readonly Ticket[] _owners = new Ticket[MaxSlots];
        private HealthComponent _health;
        private int _epoch;
        private int _capacity;
        private bool _ready;
        private bool _closed;
        private bool _started;
        private bool _geometryDirty = true;
        private float _nextRefresh;
        private float _agentRadius;
        private float _targetRadius;
        private float _firstRadius;
        private float _distance;

        /// <summary>
        /// 현재 대상이 참여를 받을 수 있는지 반환한다.
        /// </summary>
        public bool IsAvailable => isActiveAndEnabled && _ready && (!_health || _health.IsAlive);
        public bool IsClosed => _closed;
        public int ActiveRingCount => _rings.Count;
        public int ActiveCapacity => _capacity;
        public int ParticipantCount => _participants.Count;
        public float LayoutAgentRadius => _agentRadius;

        private void Awake() => _health = GetComponent<HealthComponent>();
        private void OnEnable()
        {
            if (_health)
            {
                _health.OnDeath += Close;
                _health.OnHealthChanged += HandleHealthChanged;
            }
            if (_started) ResetForReuse();
        }
        private void Start() { _started = true; ResetForReuse(); }
        private void OnDisable()
        {
            if (_health)
            {
                _health.OnDeath -= Close;
                _health.OnHealthChanged -= HandleHealthChanged;
            }
            Close();
        }
        private void Update()
        {
            if (IsAvailable && Time.deltaTime > 0f && Time.time >= _nextRefresh) RefreshAssignments();
        }

        private void HandleHealthChanged(float current, float maximum)
        {
            // 풀 재사용으로 체력이 복원되면 자신의 예약 생애를 다시 시작한다.
            if (_started && !_ready && current > 0f) ResetForReuse();
        }

        /// <summary>
        /// 소유자의 체력 복원 후 새 참여 생애를 시작한다.
        /// </summary>
        public void ResetForReuse()
        {
            Close();
            _ready = isActiveAndEnabled && (!_health || _health.IsAlive);
            _closed = !_ready;
        }

        private void Close()
        {
            _ready = false;
            _closed = true;
            _epoch++;
            _participants.Clear();
            ClearLayout();
            _agentRadius = 0f;
        }

        private void ClearLayout()
        {
            Array.Clear(_owners, 0, _owners.Length);
            _rings.Clear();
            _capacity = 0;
            _geometryDirty = true;
        }

        /// <summary>
        /// 유닛과 몸체 반경을 등록하고 참여 증표를 반환한다.
        /// </summary>
        public Ticket Join(Transform requester, float agentRadius)
        {
            if (!IsAvailable || !requester || requester == transform || !SlotGeometry.IsPositive(agentRadius)
                || !SlotGeometry.TryReadRadius(gameObject, targetRadiusSource, manualTargetRadius, out _)) return null;
            Ticket ticket = null;
            foreach (var existing in _participants)
                if (existing.Requester == requester) { ticket = existing; break; }
            if (ticket == null)
            {
                ticket = new Ticket { Owner = this, Requester = requester, Epoch = _epoch };
                _participants.Add(ticket);
            }
            if (agentRadius > _agentRadius && !Mathf.Approximately(agentRadius, _agentRadius))
            {
                NavigationTrace.AgentGrowth(this, requester, _agentRadius, agentRadius);
                _agentRadius = agentRadius;
                _geometryDirty = true;
            }
            RefreshAssignments();
            return ticket;
        }

        /// <summary>
        /// 최신 예약 목표와 revision을 반환한다.
        /// </summary>
        public SlotStatus Query(Ticket ticket, out Vector3 goal, out int revision)
        {
            goal = ticket != null && ticket.Requester ? ticket.Requester.position : transform.position;
            revision = 0;
            if (!IsCurrent(ticket)) return SlotStatus.Invalid;
            if (Time.deltaTime > 0f && Time.time >= _nextRefresh) RefreshAssignments();
            if (!IsCurrent(ticket)) return SlotStatus.Invalid;
            revision = ticket.Revision;
            if (ticket.Slot < 0) return SlotStatus.Waiting;
            goal = Position(ticket.Slot, ticket.Requester.position.y);
            return SlotStatus.Assigned;
        }

        /// <summary>
        /// 현재 참여의 원 번호를 반환하며 미배정은 -1이다.
        /// </summary>
        public int RingIndex(Ticket ticket) => IsCurrent(ticket) && ticket.Slot >= 0 ? _slots[ticket.Slot].Ring : -1;

        /// <summary>
        /// 현재 생애의 참여만 즉시 해제한다.
        /// </summary>
        public void Leave(Ticket ticket)
        {
            if (!Owns(ticket)) return;
            ReleaseSlot(ticket);
            _participants.Remove(ticket);
            ticket.Epoch = -1;
            if (IsAvailable) RefreshAssignments();
        }

        /// <summary>
        /// 조회 당시 예약이 같을 때만 실패한 자리를 잠시 제외한다.
        /// </summary>
        public void Retry(Ticket ticket, int revision)
        {
            if (!IsCurrent(ticket) || ticket.Revision != revision || ticket.Slot < 0) return;
            ticket.ExcludedUntil[ticket.Slot] = Time.time + 1f;
            ReleaseSlot(ticket);
            RefreshAssignments();
        }

        private bool Owns(Ticket ticket) => ticket != null && ticket.Owner == this && ticket.Epoch == _epoch;
        private bool IsCurrent(Ticket ticket) => IsAvailable && Owns(ticket) && ticket.Requester && ticket.Requester.gameObject.activeInHierarchy;
        internal bool IsReservationCurrent(Ticket ticket, int revision) => IsCurrent(ticket) && ticket.Revision == revision;
        private void ReleaseSlot(Ticket ticket)
        {
            if (ticket.Slot >= 0 && _owners[ticket.Slot] == ticket) _owners[ticket.Slot] = null;
            ticket.Slot = -1;
            ticket.Revision++;
        }
        private Vector3 Position(int slot, float y)
        {
            Vector3 position = transform.position + _slots[slot].Offset;
            position.y = y;
            return position;
        }

        private bool PrepareGeometry()
        {
            if (!SlotGeometry.TryReadRadius(gameObject, targetRadiusSource, manualTargetRadius, out float target))
            {
                if (_capacity > 0)
                {
                    foreach (var ticket in _participants) ReleaseSlot(ticket);
                    ClearLayout();
                }
                return false;
            }
            bool wasDirty = _geometryDirty;
            if (!Mathf.Approximately(_targetRadius, target)) _geometryDirty = true;
            if (!_geometryDirty) return true;
            NavigationTrace.Rebuild(this, _targetRadius, target, wasDirty);
            _targetRadius = target;
            _distance = _agentRadius * 2f + spacing;
            _firstRadius = Mathf.Max(0.01f, target + _agentRadius - contactInset);
            ClearLayout();
            // 대상 종료와 달리 참여는 살리고 모든 이전 슬롯 목표만 무효화한다.
            foreach (var ticket in _participants)
            {
                ticket.Slot = -1;
                ticket.Revision++;
                Array.Clear(ticket.ExcludedUntil, 0, ticket.ExcludedUntil.Length);
            }
            _geometryDirty = false;
            return true;
        }

        private void AddRing()
        {
            int ringIndex = _rings.Count;
            float radius = _firstRadius + ringIndex * _distance;
            int count = SlotGeometry.Capacity(radius, _distance, MaxSlots - _capacity);
            var ring = new Ring(radius, _capacity, count);
            _rings.Add(ring);
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                _slots[_capacity + i] = new Slot { Ring = ringIndex, Offset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius };
            }
            _capacity += count;
        }

        private void RefreshAssignments()
        {
            _nextRefresh = Time.time + refreshInterval;
            if (!IsAvailable) return;
            for (int i = _participants.Count - 1; i >= 0; i--)
            {
                var ticket = _participants[i];
                if (IsCurrent(ticket)) continue;
                ReleaseSlot(ticket);
                _participants.RemoveAt(i);
                ticket.Epoch = -1;
            }
            if (_participants.Count == 0)
            {
                ClearLayout();
                _agentRadius = 0f;
                return;
            }
            if (!PrepareGeometry()) return;
            int needed = Mathf.Min(_participants.Count, MaxSlots);
            while (_capacity < needed && _rings.Count < maxRings && _capacity < MaxSlots) AddRing();
            for (int r = 0; r < _rings.Count; r++) FillRing(r);
            while (_rings.Count > 0)
            {
                var outer = _rings[_rings.Count - 1];
                if (outer.Start < needed || Occupancy(_rings.Count - 1) > 0) break;
                _capacity = outer.Start;
                _rings.RemoveAt(_rings.Count - 1);
            }
        }

        private void FillRing(int ringIndex)
        {
            var ring = _rings[ringIndex];
            int vacancies = ring.Count - Occupancy(ringIndex);
            for (int pass = 0; pass < vacancies; pass++)
            {
                Ticket best = null;
                int bestSlot = -1;
                float bestDistance = float.PositiveInfinity;
                foreach (var candidate in _participants)
                {
                    if (candidate.Slot >= 0 && _slots[candidate.Slot].Ring <= ringIndex) continue;
                    for (int slot = ring.Start; slot < ring.Start + ring.Count; slot++)
                    {
                        if (_owners[slot] != null || Time.time < candidate.ExcludedUntil[slot]) continue;
                        Vector3 offset = Position(slot, candidate.Requester.position.y) - candidate.Requester.position;
                        float distance = offset.sqrMagnitude;
                        if (distance >= bestDistance) continue;
                        best = candidate;
                        bestSlot = slot;
                        bestDistance = distance;
                    }
                }
                if (best == null) break;
                ReleaseSlot(best);
                best.Slot = bestSlot;
                best.Revision++;
                _owners[bestSlot] = best;
            }
        }

        /// <summary>
        /// 활성 원의 기하 정보를 반환한다.
        /// </summary>
        public Ring GetRing(int index) => _rings[index];
        /// <summary>
        /// 활성 원의 점유 수를 반환한다.
        /// </summary>
        public int Occupancy(int index)
        {
            var ring = _rings[index];
            int count = 0;
            for (int i = ring.Start; i < ring.Start + ring.Count; i++) if (_owners[i] != null) count++;
            return count;
        }

        /// <summary>
        /// 실행 상태를 바꾸지 않고 입력 반경의 계산 결과를 미리 본다.
        /// </summary>
        public bool TryPreviewRing(int index, float agentRadius, out Ring ring)
        {
            ring = default;
            if (index < 0 || index >= maxRings || !SlotGeometry.IsPositive(agentRadius)
                || !SlotGeometry.TryReadRadius(gameObject, targetRadiusSource, manualTargetRadius, out float target)) return false;
            float distance = agentRadius * 2f + spacing;
            float first = Mathf.Max(0.01f, target + agentRadius - contactInset);
            int start = 0;
            for (int i = 0; i <= index; i++)
            {
                if (start >= MaxSlots) return false;
                float radius = first + i * distance;
                int count = SlotGeometry.Capacity(radius, distance, MaxSlots - start);
                ring = new Ring(radius, start, count);
                start += count;
            }
            return true;
        }

        private void OnValidate()
        {
            manualTargetRadius = SlotGeometry.IsPositive(manualTargetRadius) ? Mathf.Max(0.01f, manualTargetRadius) : 0.5f;
            spacing = SlotGeometry.IsPositive(spacing) ? Mathf.Max(0.001f, spacing) : 0.1f;
            contactInset = float.IsNaN(contactInset) || float.IsInfinity(contactInset) ? 0.15f : Mathf.Max(0f, contactInset);
            maxRings = Mathf.Clamp(maxRings, 1, 16);
            refreshInterval = SlotGeometry.IsPositive(refreshInterval) ? Mathf.Max(0.02f, refreshInterval) : 0.2f;
            _geometryDirty = true;
        }

        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            float agent = Application.isPlaying && _agentRadius > 0f ? _agentRadius : previewAgentRadius;
            int rings = Application.isPlaying ? _rings.Count : maxRings;
            for (int r = 0; r < rings; r++)
            {
                Ring ring;
                if (Application.isPlaying) ring = _rings[r];
                else if (!TryPreviewRing(r, agent, out ring)) break;
                Gizmos.color = new Color(0.5f, 0.7f, 1f, 0.6f);
                Vector3 last = transform.position + Vector3.forward * ring.Radius;
                for (int i = 1; i <= 64; i++)
                {
                    float angle = i * Mathf.PI * 2f / 64;
                    Vector3 next = transform.position + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * ring.Radius;
                    Gizmos.DrawLine(last, next);
                    last = next;
                }
                for (int i = 0; i < ring.Count; i++)
                {
                    float angle = i * Mathf.PI * 2f / ring.Count;
                    Vector3 point = transform.position + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * ring.Radius;
                    Gizmos.color = Application.isPlaying && _owners[ring.Start + i] != null ? Color.red : Color.green;
                    Gizmos.DrawWireSphere(point + Vector3.up * 0.1f, 0.08f);
                }
            }
#endif
        }
    }
}
