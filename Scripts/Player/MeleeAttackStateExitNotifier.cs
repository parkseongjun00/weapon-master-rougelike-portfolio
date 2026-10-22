using UnityEngine;
using WeaponMaster.Characters;

namespace WeaponMaster.Player
{
    /// <summary>
    /// 근접 공격 상태의 진입·종료를 공통 모션에 알린다.
    /// </summary>
    public class MeleeAttackStateExitNotifier : StateMachineBehaviour
    {
        private CharacterAnimationController _animation;
        private int _version;
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _animation = animator.GetComponent<CharacterAnimationController>();
            _version = _animation ? _animation.EnterAttackState(stateInfo.fullPathHash) : -1;
        }
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (_animation) _animation.ExitAttackState(stateInfo.fullPathHash, _version);
        }
    }
}
