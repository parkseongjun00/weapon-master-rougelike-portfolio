using UnityEngine;

namespace WeaponMaster.Player
{
    /// <summary>
    /// 현재 조준 방향을 향해 플레이어를 회전시킨다.
    /// </summary>
    public class PlayerAim : MonoBehaviour
    {
        [SerializeField] private AimVectorProvider aim;

        private void Update()
        {
            Vector3 direction = aim.AimDirection;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }
    }
}
