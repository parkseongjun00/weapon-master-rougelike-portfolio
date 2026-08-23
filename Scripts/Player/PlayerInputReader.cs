using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WeaponMaster.Player
{
    /// <summary>
    /// Input System의 원시 액션을 게임플레이 스크립트가 사용하는 단순한 값/이벤트로 변환한다.
    /// </summary>
    // 이후 단계의 코드는 InputAction을 직접 다루면 안 된다 - PC와 모바일의 바인딩 차이를 나중에 흡수할 지점이 바로 여기다.
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;

        private InputAction _moveAction;
        private InputAction _aimPointAction;
        private InputAction _attackAction;
        private InputAction _dropAction;

        public Vector2 MoveInput { get; private set; }
        public Vector2 AimScreenPosition { get; private set; }
        public bool AttackHeld { get; private set; }

        public event Action DropPerformed;

        private void Awake()
        {
            InputActionMap map = inputActions.FindActionMap("Player");
            _moveAction = map.FindAction("Move");
            _aimPointAction = map.FindAction("AimPoint");
            _attackAction = map.FindAction("Attack");
            _dropAction = map.FindAction("DropWeapon");
        }

        private void OnEnable()
        {
            _moveAction.Enable();
            _aimPointAction.Enable();
            _attackAction.Enable();
            _dropAction.Enable();

            _dropAction.performed += OnDrop;
        }

        private void OnDisable()
        {
            _dropAction.performed -= OnDrop;

            _moveAction.Disable();
            _aimPointAction.Disable();
            _attackAction.Disable();
            _dropAction.Disable();
        }

        private void Update()
        {
            // 일시정지 중에도 Update()는 계속 호출된다(멈추는 건 deltaTime/FixedUpdate뿐) - 이후 코드가 반응하지 않도록 여기서 입력을 멈춘다.
            if (Time.timeScale <= 0f) return;

            MoveInput = _moveAction.ReadValue<Vector2>();
            AimScreenPosition = _aimPointAction.ReadValue<Vector2>();
            AttackHeld = _attackAction.IsPressed();
        }

        private void OnDrop(InputAction.CallbackContext context)
        {
            DropPerformed?.Invoke();
        }
    }
}
