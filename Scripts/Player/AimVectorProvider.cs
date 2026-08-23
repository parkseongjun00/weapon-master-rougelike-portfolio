using UnityEngine;

namespace WeaponMaster.Player
{
    /// <summary>
    /// 현재 조준 입력을 단일 월드 스페이스 방향 벡터로 변환한다.
    /// </summary>
    // PC는 마우스의 화면 좌표를 읽고, 향후 모바일 스틱 입력도 동일한 AimDirection 출력을 만들어내므로 이동/조준/무기 코드는 입력 방식이 무엇인지 알 필요가 없다.
    public class AimVectorProvider : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Camera aimCamera;

        public Vector3 AimDirection { get; private set; } = Vector3.forward;

        private void Update()
        {
            Ray ray = aimCamera.ScreenPointToRay(input.AimScreenPosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 hitPoint = ray.GetPoint(distance);
                Vector3 direction = hitPoint - transform.position;
                direction.y = 0f;

                if (direction.sqrMagnitude > 0.0001f)
                {
                    AimDirection = direction.normalized;
                }
            }
        }
    }
}
