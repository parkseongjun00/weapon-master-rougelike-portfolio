using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WeaponMaster.EditorTools
{
    /// <summary>
    /// 기존 InputSystem_Actions 애셋에 Stage 1에 필요한 두 게임플레이 액션(마우스 조준 위치, 수동 무기 드롭)을 추가한다. 멱등적(idempotent)이라 여러 번 실행해도 안전하다.
    /// </summary>
    // 두 번째 입력 애셋을 새로 만드는 대신 기존 애셋에 추가하는 방식을 택했다.
    public static class PlayerInputActionsSetup
    {
        private const string AssetPath = "Assets/InputSystem_Actions.inputactions";

        public static void EnsureActions()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            if (asset == null)
            {
                Debug.LogError($"[PlayerInputActionsSetup] Could not find {AssetPath}");
                return;
            }

            InputActionMap playerMap = asset.FindActionMap("Player");
            if (playerMap == null)
            {
                Debug.LogError("[PlayerInputActionsSetup] 'Player' action map not found.");
                return;
            }

            bool changed = false;

            if (playerMap.FindAction("AimPoint") == null)
            {
                InputAction aimAction = playerMap.AddAction("AimPoint", InputActionType.Value, expectedControlLayout: "Vector2");
                aimAction.AddBinding("<Mouse>/position", groups: "Keyboard&Mouse");
                changed = true;
            }

            if (playerMap.FindAction("DropWeapon") == null)
            {
                InputAction dropAction = playerMap.AddAction("DropWeapon", InputActionType.Button);
                dropAction.AddBinding("<Keyboard>/q", groups: "Keyboard&Mouse");
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                Debug.Log("[PlayerInputActionsSetup] Added AimPoint/DropWeapon actions to the Player map.");
            }
        }
    }
}
