using UnityEngine;
using WeaponMaster.Movement;

namespace WeaponMaster.Player
{
    /// <summary>
    /// 아레나의 평평한 바닥 위에서 플레이어를 이동시킨다.
    /// </summary>
    // 평평한 단일 아레나를 전제로 중력과 경사를 처리하지 않으며, 외부 배율의 출처도 알지 않는다.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour, IMovementSpeed
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private float moveSpeed = 6f;

        private CharacterController _controller;
        private float _speedMultiplier = 1f;

        /// <summary>
        /// 현재 프레임의 실제 이동 속도(월드 단위/초).
        /// </summary>
        public float CurrentSpeed { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            float effectiveSpeed = moveSpeed * _speedMultiplier;
            Vector2 moveInput = input.MoveInput;
            Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y) * effectiveSpeed;
            Vector3 before = transform.position;
            _controller.Move(move * Time.deltaTime);
            Vector3 displacement = transform.position - before;
            displacement.y = 0f;
            CurrentSpeed = Time.deltaTime > 0f ? displacement.magnitude / Time.deltaTime : 0f;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = multiplier;
        }
    }
}
