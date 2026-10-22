using UnityEngine;

namespace WeaponMaster.Effects
{
    /// <summary>
    /// 무기 궤적의 생성과 잔상 정리를 담당한다.
    /// </summary>
    [RequireComponent(typeof(TrailRenderer))]
    public sealed class WeaponTrailEffect : MonoBehaviour
    {
        private TrailRenderer _trail;

        private void Awake()
        {
            _trail = GetComponent<TrailRenderer>();
            Clear();
        }

        /// <summary>
        /// 이전 잔상을 지우고 궤적 생성을 시작한다.
        /// </summary>
        public void Begin()
        {
            if (!isActiveAndEnabled) return;
            _trail.Clear();
            _trail.emitting = true;
        }

        /// <summary>
        /// 생성을 멈추고 기존 잔상은 자연스럽게 소멸시킨다.
        /// </summary>
        public void Stop()
        {
            if (_trail) _trail.emitting = false;
        }

        /// <summary>
        /// 생성과 남은 잔상을 즉시 정리한다.
        /// </summary>
        public void Clear()
        {
            if (!_trail) return;
            _trail.emitting = false;
            _trail.Clear();
        }

        private void OnDisable() => Clear();
    }
}
