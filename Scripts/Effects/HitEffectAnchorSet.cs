using UnityEngine;

namespace WeaponMaster.Effects
{
    /// <summary>
    /// 본에 부착한 발생 지점 중 명중 위치와 방향에 맞는 지점을 선택한다.
    /// </summary>
    public class HitEffectAnchorSet : MonoBehaviour
    {
        [Tooltip("Each anchor's forward axis points out of the body.")]
        [SerializeField] private Transform[] anchors;

        /// <summary>
        /// 공격을 향하는 지점 중 명중 추정 위치에 가장 가까운 월드 자세를 반환한다.
        /// </summary>
        public bool TryGetPose(Vector3 hitPosition, Vector3 attackDirection, out Pose pose)
        {
            pose = default;
            if (!isActiveAndEnabled || anchors == null) return false;

            Transform best = null;
            float bestDistance = float.PositiveInfinity;
            bool bestFacing = false;
            Vector3 towardAttacker = -attackDirection.normalized;

            foreach (Transform anchor in anchors)
            {
                if (!anchor || !anchor.gameObject.activeInHierarchy) continue;
                bool facing = Vector3.Dot(anchor.forward, towardAttacker) > 0f;
                float distance = (anchor.position - hitPosition).sqrMagnitude;
                if (best)
                {
                    if (bestFacing && !facing) continue;
                    if (bestFacing == facing && distance >= bestDistance) continue;
                }

                best = anchor;
                bestFacing = facing;
                bestDistance = distance;
            }

            if (!best) return false;
            pose = new Pose(best.position, best.rotation);
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (anchors == null) return;
            Gizmos.color = Color.yellow;
            foreach (Transform anchor in anchors)
            {
                if (!anchor) continue;
                Gizmos.DrawWireSphere(anchor.position, 0.025f);
                Gizmos.DrawLine(anchor.position, anchor.position + anchor.forward * 0.12f);
            }
        }
    }
}
