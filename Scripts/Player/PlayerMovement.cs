using UnityEngine;

namespace WeaponMaster.Player
{
    /// <summary>
    /// 아레나의 평평한 바닥 위에서 플레이어를 이동시킨다.
    /// </summary>
    // 중력/경사 처리는 없다 - 아레나는 하나의 평평한 평면이므로 수직 낙하를 시뮬레이션해도 실제로 할 일이 없는 불필요한 복잡도일 뿐이다. speedMultiplier는 외부(증강 등)에서 SetSpeedMultiplier로 밀어넣는 값이고, 이 클래스는 그 값이 어디서 오는지 모른다.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
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
            _controller.Move(move * Time.deltaTime);
            CurrentSpeed = move.magnitude;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = multiplier;
        }
    }
}
