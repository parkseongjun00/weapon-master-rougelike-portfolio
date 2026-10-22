using UnityEngine;

namespace WeaponMaster.Player
{
    /// <summary>
    /// 현재 조준 입력을 단일 월드 스페이스 방향 벡터로 변환한다.
    /// </summary>
    // 조준 입력을 AimDirection으로 정규화해 후속 코드에 전달한다.
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
