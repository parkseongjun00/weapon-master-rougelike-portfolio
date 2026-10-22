using System.Collections.Generic;
using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.Effects
{
    /// <summary>
    /// 스파크를 사전 생성하고 동시 재생·점등 수를 제한해 재사용한다.
    /// </summary>
    public class HitSparkPool : MonoBehaviour
    {
        [SerializeField] private HitSparkEffect sparkPrefab;
        [SerializeField, Min(1)] private int capacity = 16;
        [SerializeField] private bool enableLights = true;
        [SerializeField, Min(0)] private int maxActiveLights = 4;

        private SimpleObjectPool<HitSparkEffect> _pool;
        private readonly List<HitSparkEffect> _active = new();

        /// <summary>
        /// 현재 씬의 스파크 풀이다.
        /// </summary>
        public static HitSparkPool Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            if (sparkPrefab)
                _pool = new SimpleObjectPool<HitSparkEffect>(sparkPrefab, transform, capacity);
        }

        /// <summary>
        /// 여유 인스턴스가 있을 때 스파크를 재생한다.
        /// </summary>
        public void Spawn(Pose pose)
        {
            if (!isActiveAndEnabled || _pool == null || _active.Count >= capacity) return;
            int activeLights = 0;
            foreach (HitSparkEffect active in _active)
                if (active.IsLightActive) activeLights++;

            HitSparkEffect effect = _pool.Get(pose.position, pose.rotation);
            _active.Add(effect);
            effect.Finished += ReturnEffect;
            effect.Play(enableLights && activeLights < maxActiveLights);
        }

        private void ReturnEffect(HitSparkEffect effect)
        {
            if (!_active.Remove(effect)) return;
            effect.Finished -= ReturnEffect;
            _pool.Release(effect);
        }

        private void OnDisable()
        {
            while (_active.Count > 0)
                ReturnEffect(_active[_active.Count - 1]);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
