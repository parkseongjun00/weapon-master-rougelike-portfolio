using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace WeaponMaster.EditorTools
{
    /// <summary>
    /// PlayerBaseController에 상체 전용 Animator 레이어(Avatar Mask + Override 블렌딩)를 추가해,
    /// 이동 중에도 공격 애니메이션이 다리(로코모션)를 덮지 않게 한다.
    /// </summary>
    // HANDOFF.md §3-2 / DevNotes.md §3.6 대상. Stage1SceneBuilder.BuildPlayerAnimatorController는
    // 컨트롤러 파일이 이미 있으면 그냥 반환하고 끝이라(기존 자산을 덮어쓰지 않기 위한 가드) 이
    // 레이어 추가를 알지 못한다 - 그 함수를 고치는 대신 기존 컨트롤러를 그 자리에서 수정하는
    // 별도 스크립트로 분리했다(그 함수 자체가 §3.4 파일명 정리 이후로도 갱신 안 된 옛 fbx 경로를
    // 참조하고 있어 이미 최신 상태가 아님 - 이번 작업 범위 밖이라 손 안 댐).
    public static class PlayerUpperBodyLayerSetup
    {
        private const string ControllerPath = "Assets/Animation/PlayerBaseController.controller";
        private const string MaskPath = "Assets/Animation/UpperBodyMask.mask";
        private const string UpperLayerName = "Upper Body";

        private static readonly string[] AttackStateNames =
        {
            "Attack_MeleeWeapon", "Attack_UnarmedLeft", "Attack_UnarmedRight", "RangedFire"
        };

        /// <summary>
        /// Upper Body 레이어를 생성/이관한다. 이미 적용돼 있으면(Base Layer에 공격 상태가 더는
        /// 없으면) 아무 것도 하지 않고 끝난다 - 재실행해도 안전하다.
        /// </summary>
        [MenuItem("Weapon Master/Stage 4/Setup Upper Body Animation Layer")]
        public static void Setup()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError($"[PlayerUpperBodyLayerSetup] {ControllerPath}를 찾지 못했다.");
                return;
            }

            AnimatorStateMachine baseSM = controller.layers[0].stateMachine;

            AnimatorState[] attackStates = FindAttackStates(baseSM);
            if (attackStates == null)
            {
                Debug.Log("[PlayerUpperBodyLayerSetup] 이미 적용된 상태로 보임(Base Layer에 공격 상태 없음) - 변경 없이 종료.");
                return;
            }

            AnimatorState baseLocomotion = FindState(baseSM, "Locomotion");
            if (baseLocomotion == null)
            {
                Debug.LogError("[PlayerUpperBodyLayerSetup] Base Layer에서 Locomotion 상태를 찾지 못했다.");
                return;
            }

            AvatarMask mask = EnsureUpperBodyMask();
            AnimatorStateMachine upperSM = EnsureFreshUpperLayer(controller, mask);

            // 공격 중이 아닐 때도 팔이 같이 움직이도록, Base Layer와 같은 블렌드 트리 에셋을 그대로 공유한다.
            AnimatorState upperLocomotion = upperSM.AddState("Locomotion");
            upperLocomotion.motion = baseLocomotion.motion;
            upperSM.defaultState = upperLocomotion;

            foreach (AnimatorState source in attackStates)
            {
                AnimatorState copy = upperSM.AddState(source.name);
                copy.motion = source.motion;

                AnimatorStateTransition sourceEntry = FindAnyStateTransition(baseSM, source);
                CopyTransitionSettings(sourceEntry, upperSM.AddAnyStateTransition(copy));

                if (source.transitions.Length > 0)
                {
                    CopyTransitionSettings(source.transitions[0], copy.AddTransition(upperLocomotion));
                }
            }

            RemoveAttackStatesFromBaseLayer(baseSM, attackStates);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[PlayerUpperBodyLayerSetup] Upper Body 레이어 적용 완료.");
        }

        // 4개 공격 상태가 전부 Base Layer에 남아있으면 그 배열을, 하나도 없으면(이미 이관 완료) null을 반환한다.
        private static AnimatorState[] FindAttackStates(AnimatorStateMachine baseSM)
        {
            var states = new AnimatorState[AttackStateNames.Length];
            int found = 0;
            for (int i = 0; i < AttackStateNames.Length; i++)
            {
                states[i] = FindState(baseSM, AttackStateNames[i]);
                if (states[i] != null) found++;
            }

            if (found == 0) return null;
            if (found != AttackStateNames.Length)
            {
                Debug.LogError("[PlayerUpperBodyLayerSetup] Base Layer에 공격 상태가 일부만 남아있다 - 수동으로 확인 필요.");
                return null;
            }

            return states;
        }

        private static AnimatorState FindState(AnimatorStateMachine sm, string name)
        {
            foreach (ChildAnimatorState child in sm.states)
            {
                if (child.state.name == name) return child.state;
            }

            return null;
        }

        private static AnimatorStateTransition FindAnyStateTransition(AnimatorStateMachine sm, AnimatorState destination)
        {
            foreach (AnimatorStateTransition transition in sm.anyStateTransitions)
            {
                if (transition.destinationState == destination) return transition;
            }

            Debug.LogError($"[PlayerUpperBodyLayerSetup] {destination.name}로 가는 AnyState 전환을 찾지 못했다.");
            return null;
        }

        private static void CopyTransitionSettings(AnimatorStateTransition source, AnimatorStateTransition target)
        {
            if (source == null) return;

            foreach (AnimatorCondition condition in source.conditions)
            {
                target.AddCondition(condition.mode, condition.threshold, condition.parameter);
            }

            target.hasExitTime = source.hasExitTime;
            target.exitTime = source.exitTime;
            target.hasFixedDuration = source.hasFixedDuration;
            target.duration = source.duration;
            target.offset = source.offset;
            target.interruptionSource = source.interruptionSource;
            target.orderedInterruption = source.orderedInterruption;
            target.canTransitionToSelf = source.canTransitionToSelf;
        }

        private static void RemoveAttackStatesFromBaseLayer(AnimatorStateMachine baseSM, AnimatorState[] attackStates)
        {
            foreach (AnimatorState state in attackStates)
            {
                AnimatorStateTransition anyTransition = FindAnyStateTransition(baseSM, state);
                if (anyTransition != null) baseSM.RemoveAnyStateTransition(anyTransition);
                baseSM.RemoveState(state);
            }
        }

        // 상체(척추/머리/팔/손가락)만 켜고 하체/루트/IK는 꺼서, 이 마스크가 적용된 레이어가
        // 다리 애니메이션에 전혀 관여하지 않게 한다.
        private static AvatarMask EnsureUpperBodyMask()
        {
            AvatarMask existing = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
            if (existing != null) return existing;

            var mask = new AvatarMask();
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, false);

            AssetDatabase.CreateAsset(mask, MaskPath);
            return mask;
        }

        // "Upper Body"라는 이름의 레이어가 이미 있으면(직전 실행이 상태 이관 도중 실패한 경우 등)
        // 지우고 새로 만들어 항상 깨끗한 상태에서 시작한다.
        private static AnimatorStateMachine EnsureFreshUpperLayer(AnimatorController controller, AvatarMask mask)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == UpperLayerName)
                {
                    controller.RemoveLayer(i);
                    break;
                }
            }

            controller.AddLayer(UpperLayerName);

            layers = controller.layers;
            int newIndex = layers.Length - 1;
            layers[newIndex].avatarMask = mask;
            layers[newIndex].blendingMode = AnimatorLayerBlendingMode.Override;
            layers[newIndex].defaultWeight = 1f;
            controller.layers = layers;

            return controller.layers[newIndex].stateMachine;
        }
    }
}
