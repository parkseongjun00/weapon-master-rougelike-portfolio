using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using System.Collections.Generic;
#endif

namespace WeaponMaster.Movement
{
    // 원인 확인 뒤 제거할 Editor 전용 계측이다. 이동·예약 상태는 변경하지 않는다.
    internal static class NavigationTrace
    {
#if UNITY_EDITOR
        private sealed class Counter
        {
            public float NextTime;
            public int Pending;
        }

        private static readonly Dictionary<string, Counter> Counters = new Dictionary<string, Counter>();
        private static readonly Dictionary<int, int> Layouts = new Dictionary<int, int>();
        private static Transform _watched;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Counters.Clear();
            Layouts.Clear();
            _watched = null;
        }

        private static bool Watch(Component actor)
        {
            if (!actor || !actor.gameObject.activeInHierarchy) return false;
            if (!_watched || !_watched.gameObject.activeInHierarchy)
            {
                _watched = actor.transform;
                UnityEngine.Debug.Log($"[NavTrace] frame={Time.frameCount} t={Time.unscaledTime:F3} Watch actor={_watched.GetInstanceID()} name={actor.name}", actor);
            }
            return _watched == actor.transform;
        }

        private static bool Allow(string key, out int count)
        {
            if (!Counters.TryGetValue(key, out Counter counter))
            {
                counter = new Counter();
                Counters.Add(key, counter);
            }
            counter.Pending++;
            count = counter.Pending;
            if (Time.unscaledTime < counter.NextTime) return false;
            counter.Pending = 0;
            counter.NextTime = Time.unscaledTime + 0.5f;
            return true;
        }

        private static int Sequence(SurroundSlots slots) => slots && Layouts.TryGetValue(slots.GetInstanceID(), out int sequence) ? sequence : 0;
        private static string Header(string kind, Component owner, int count) =>
            $"[NavTrace] frame={Time.frameCount} t={Time.unscaledTime:F3} {kind} owner={owner.transform.GetInstanceID()} eventsSinceLast={count}";

        private static string AgentState(NavMeshAgent agent)
        {
            if (!agent || !agent.enabled || !agent.isOnNavMesh) return "agent=unavailable";
            return $"stopped={agent.isStopped} pending={agent.pathPending} hasPath={agent.hasPath} path={agent.pathStatus} remaining={agent.remainingDistance:R} velocity={agent.velocity.ToString("F4")}";
        }
#endif

        [Conditional("UNITY_EDITOR")]
        public static void Rebuild(SurroundSlots slots, float previous, float current, bool wasDirty)
        {
#if UNITY_EDITOR
            int sequence = Sequence(slots) + 1;
            Layouts[slots.GetInstanceID()] = sequence;
            if (!Allow("Layout:" + slots.GetInstanceID(), out int count)) return;
            UnityEngine.Debug.Log($"{Header("LayoutRebuild", slots, count)} layoutSeq={sequence} dirtyBefore={wasDirty} targetChanged={previous != current} oldRadius={previous:R} newRadius={current:R} delta={(current - previous):R} agentRadius={slots.LayoutAgentRadius:R} participants={slots.ParticipantCount} targetPosition={slots.transform.position.ToString("F4")} targetScale={slots.transform.lossyScale.ToString("F9")}", slots);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void AgentGrowth(SurroundSlots slots, Transform requester, float previous, float current)
        {
#if UNITY_EDITOR
            if (!Allow("Growth:" + slots.GetInstanceID(), out int count)) return;
            UnityEngine.Debug.Log($"{Header("AgentRadiusGrowth", slots, count)} requester={requester.GetInstanceID()} oldRadius={previous:R} newRadius={current:R} delta={(current - previous):R}", slots);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void Rejoin(SlotFollower follower, float previous, float current, bool hadTicket)
        {
#if UNITY_EDITOR
            if (!Watch(follower) || !Allow("Rejoin", out int count)) return;
            UnityEngine.Debug.Log($"{Header("Join", follower, count)} hadTicket={hadTicket} oldRadius={previous:R} newRadius={current:R} delta={(current - previous):R}", follower);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void Reservation(SlotFollower follower, SurroundSlots slots, int previous, int current, int slot, Vector3 goal)
        {
#if UNITY_EDITOR
            if (!Watch(follower) || !Allow("Reservation", out int count)) return;
            UnityEngine.Debug.Log($"{Header("ReservationChanged", follower, count)} provider={slots.GetInstanceID()} layoutSeq={Sequence(slots)} revision={previous}->{current} slot={slot} goal={goal.ToString("F4")} distance={Vector3.Distance(follower.transform.position, goal):F4}", follower);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void Stop(NavigationMovement movement, NavMeshAgent agent, string reason, bool hadDestination)
        {
#if UNITY_EDITOR
            if (!Watch(movement) || !Allow("Stop:" + reason, out int count)) return;
            UnityEngine.Debug.Log($"{Header("Stop", movement, count)} reason={reason} hadDestination={hadDestination} move={movement.State} speed={movement.CurrentSpeed:F4} {AgentState(agent)}", movement);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void Sample(SlotFollower follower, SurroundSlots slots, NavigationMovement movement, Vector3 goal, int revision)
        {
#if UNITY_EDITOR
            if (!Watch(follower) || !Allow("Sample", out int count)) return;
            UnityEngine.Debug.Log($"{Header("Sample", follower, count)} provider={slots.GetInstanceID()} layoutSeq={Sequence(slots)} revision={revision} follow={follower.State} move={movement.State} distance={Vector3.Distance(follower.transform.position, goal):F4} position={follower.transform.position.ToString("F4")} speed={movement.CurrentSpeed:F4} {AgentState(movement.GetComponent<NavMeshAgent>())}", follower);
#endif
        }
    }
}
