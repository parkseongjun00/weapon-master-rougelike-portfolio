using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.Effects
{
    /// <summary>
    /// 피격 방향으로 외형을 늘리고 기울인 뒤 탄력적으로 원상 복원한다.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public class HitElasticReaction : MonoBehaviour
    {
        [SerializeField] private Transform reactionAxis;
        [SerializeField] private Transform orientationCompensator;
        [SerializeField, Min(0.01f)] private float duration = 0.24f;
        [SerializeField, Range(0f, 0.5f)] private float stretch = 0.16f;
        [SerializeField, Range(0f, 25f)] private float leanAngle = 8f;
        [SerializeField] private AnimationCurve response = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.15f, 1f),
            new Keyframe(0.65f, -0.12f), new Keyframe(1f, 0f));

        private HealthComponent _health;
        private Quaternion _baseRotation;
        private Quaternion _baseCompensatorRotation;
        private Vector3 _baseScale;
        private Vector3 _worldDirection;
        private float _elapsed;
        private bool _playing;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
            if (!reactionAxis || !orientationCompensator) return;
            _baseRotation = reactionAxis.localRotation;
            _baseScale = reactionAxis.localScale;
            _baseCompensatorRotation = orientationCompensator.localRotation;
        }

        private void OnEnable()
        {
            _health.OnDeath += Restore;
        }

        /// <summary>
        /// 지정한 월드 공격 방향의 피격 반응을 시작한다.
        /// </summary>
        public void Play(Vector3 attackDirection)
        {
            if (!isActiveAndEnabled || !_health.IsAlive || !reactionAxis || !orientationCompensator) return;
            Vector3 direction = Vector3.ProjectOnPlane(attackDirection, Vector3.up);
            if (direction.sqrMagnitude < 0.0001f) return;

            // 이전 변형을 새 기준으로 삼거나 연속 피격의 강도를 누적하지 않는다.
            Restore();
            _worldDirection = direction.normalized;
            _elapsed = 0f;
            _playing = true;
        }

        private void LateUpdate()
        {
            if (!_playing) return;
            _elapsed += Time.deltaTime;
            if (_elapsed >= duration)
            {
                Restore();
                return;
            }

            float amount = Mathf.Clamp(response.Evaluate(_elapsed / duration), -1f, 1f);
            Quaternion alignment = Quaternion.LookRotation(
                transform.InverseTransformDirection(_worldDirection), Vector3.up);
            float length = 1f + stretch * amount;
            float width = 1f / Mathf.Sqrt(length);
            reactionAxis.localRotation = _baseRotation * alignment
                * Quaternion.AngleAxis(leanAngle * amount, Vector3.right);
            reactionAxis.localScale = Vector3.Scale(_baseScale, new Vector3(width, width, length));
            // 늘어나는 축만 공격 방향에 맞추고 모델의 기본 방향은 유지한다.
            orientationCompensator.localRotation = Quaternion.Inverse(alignment) * _baseCompensatorRotation;
        }

        private void Restore()
        {
            if (!_playing) return;
            _playing = false;
            _elapsed = 0f;
            if (reactionAxis)
            {
                reactionAxis.localRotation = _baseRotation;
                reactionAxis.localScale = _baseScale;
            }
            if (orientationCompensator)
                orientationCompensator.localRotation = _baseCompensatorRotation;
        }

        private void OnDisable()
        {
            if (_health) _health.OnDeath -= Restore;
            Restore();
        }

        private void OnDestroy()
        {
            Restore();
        }
    }
}
