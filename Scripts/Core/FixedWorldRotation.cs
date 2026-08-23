using UnityEngine;

namespace WeaponMaster.Core
{
    /// <summary>
    /// 부모가 매 프레임 회전해도(예: 적이 이동 방향을 바라보도록 회전하는 EnemyAI) 이 트랜스폼의 월드 회전만은 고정값으로 유지한다.
    /// </summary>
    // 카메라가 고정 오프셋으로만 따라오고 회전하지 않으므로(QuarterViewCameraFollow), 매 프레임 카메라를 보게 계산하는 진짜 빌보드 대신 이 방식으로 충분하다.
    // 목표 각도가 안 바뀌니 카메라 참조나 LookRotation 계산이 필요 없다.
    public class FixedWorldRotation : MonoBehaviour
    {
        [SerializeField] private Vector3 worldEulerAngles;

        private void LateUpdate()
        {
            transform.rotation = Quaternion.Euler(worldEulerAngles);
        }
    }
}
