using System;
using UnityEngine;

namespace WeaponMaster.Effects
{
    /// <summary>
    /// 스파크 입자와 선택 조명을 재생하고 종료를 알린다.
    /// </summary>
    public class HitSparkEffect : MonoBehaviour
    {
        [SerializeField] private Light flashLight;
        [SerializeField, Min(0f)] private float lightIntensity = 3f;
        [SerializeField, Min(0.001f)] private float lightDuration = 0.07f;

        private ParticleSystem[] _particles;
        private float _elapsed;
        private bool _playing;

        /// <summary>
        /// 입자와 조명이 모두 끝났을 때 발생한다.
        /// </summary>
        public event Action<HitSparkEffect> Finished;

        /// <summary>
        /// 현재 조명이 켜져 있는지 반환한다.
        /// </summary>
        public bool IsLightActive => flashLight && flashLight.enabled;

        private void Awake()
        {
            _particles = GetComponentsInChildren<ParticleSystem>(true);
        }

        /// <summary>
        /// 이전 입자를 지우고 새 스파크를 재생한다.
        /// </summary>
        public void Play(bool withLight)
        {
            _elapsed = 0f;
            _playing = true;
            foreach (ParticleSystem particles in _particles)
            {
                particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                particles.Play(false);
            }
            if (!flashLight) return;
            flashLight.intensity = withLight ? lightIntensity : 0f;
            flashLight.enabled = withLight && lightIntensity > 0f;
        }

        private void Update()
        {
            if (!_playing) return;
            _elapsed += Time.deltaTime;
            if (IsLightActive)
            {
                float remaining = 1f - Mathf.Clamp01(_elapsed / lightDuration);
                flashLight.intensity = lightIntensity * remaining * remaining;
                flashLight.enabled = remaining > 0f;
            }

            foreach (ParticleSystem particles in _particles)
                if (particles.IsAlive(false)) return;
            if (IsLightActive) return;

            _playing = false;
            Finished?.Invoke(this);
        }

        private void OnDisable()
        {
            _playing = false;
            _elapsed = 0f;
            if (_particles != null)
                foreach (ParticleSystem particles in _particles)
                    particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (!flashLight) return;
            flashLight.enabled = false;
            flashLight.intensity = 0f;
        }
    }
}
