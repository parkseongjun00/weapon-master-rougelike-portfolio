using UnityEngine;

namespace WeaponMaster.CameraControl
{
    /// <summary>
    /// 플레이어에 대해 고정된 쿼터뷰 오프셋을 유지한다(GDD 1/3).
    /// </summary>
    public class QuarterViewCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -9f);
        [SerializeField] private float followSpeed = 10f;

        private void LateUpdate()
        {
            if (!target) return;

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
            transform.LookAt(target.position);
            
        }
    }
}
