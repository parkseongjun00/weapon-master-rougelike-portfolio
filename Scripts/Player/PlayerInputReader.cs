using System;
using UnityEngine;
using UnityEngine.InputSystem;
using WeaponMaster.Input;

namespace WeaponMaster.Player
{
    /// <summary>
    /// Input System의 원시 액션을 게임플레이 스크립트가 사용하는 단순한 값/이벤트로 변환한다.
    /// </summary>
    // 게임플레이 코드는 InputAction을 직접 다루지 않고 변환된 값과 이벤트를 사용한다.
    public class PlayerInputReader : MonoBehaviour
    {
        private GameInputActions _actions;

        private InputAction _moveAction;
        private InputAction _aimPointAction;
        private InputAction _attackAction;
        private InputAction _interactAction;

        public Vector2 MoveInput { get; private set; }
        public Vector2 AimScreenPosition { get; private set; }
        public bool AttackHeld { get; private set; }

        /// <summary>
        /// 상호작용 버튼(장착/교체/드랍을 문맥에 따라 하나로 묶은 입력)이 눌렸을 때 발행된다.
        /// </summary>
        // 입력 에셋의 액션명은 아직 DropWeapon이지만, 게임에서는 통합 상호작용으로 해석한다.
        public event Action InteractPerformed;

        private void Awake()
        {
            _actions = new GameInputActions();
            _moveAction = _actions.Player.Move;
            _aimPointAction = _actions.Player.AimPoint;
            _attackAction = _actions.Player.Attack;
            _interactAction = _actions.Player.DropWeapon;
        }

        private void OnDestroy()
        {
            _actions?.Dispose();
        }

        private void OnEnable()
        {
            _moveAction.Enable();
            _aimPointAction.Enable();
            _attackAction.Enable();
            _interactAction.Enable();

            _interactAction.performed += OnInteract;
        }

        private void OnDisable()
        {
            _interactAction.performed -= OnInteract;

            _moveAction.Disable();
            _aimPointAction.Disable();
            _attackAction.Disable();
            _interactAction.Disable();
        }

        private void Update()
        {
            // Update는 일시정지 중에도 호출되므로 게임플레이 입력을 여기서 차단한다.
            if (Time.timeScale <= 0f) return;

            MoveInput = _moveAction.ReadValue<Vector2>();
            AimScreenPosition = _aimPointAction.ReadValue<Vector2>();
            AttackHeld = _attackAction.IsPressed();
        }

        private void OnInteract(InputAction.CallbackContext context)
        {
            InteractPerformed?.Invoke();
        }
    }
}
