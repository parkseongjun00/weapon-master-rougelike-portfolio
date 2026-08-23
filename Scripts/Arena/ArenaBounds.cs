using UnityEngine;

namespace WeaponMaster.Arena
{
    /// <summary>
    /// 아레나의 직사각형 플레이 영역을 정의하고, "경계 근처 어디에 스폰할 수 있는가"라는 질의에 답한다(GDD 6.1).
    /// </summary>
    public class ArenaBounds : MonoBehaviour
    {
        [SerializeField] private Vector2 halfExtents = new(20f, 20f);
        [SerializeField] private float edgeMargin = 2f;

        public Vector2 HalfExtents => halfExtents;

        public Vector3 GetRandomEdgePoint(Vector3 playerPosition, float minDistanceFromPlayer)
        {
            const int maxAttempts = 30;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Vector3 candidate = RandomPerimeterPoint();
                if (Vector3.Distance(candidate, playerPosition) >= minDistanceFromPlayer)
                {
                    return candidate;
                }
            }

            return RandomPerimeterPoint();
        }

        private Vector3 RandomPerimeterPoint()
        {
            float innerX = halfExtents.x - edgeMargin;
            float innerZ = halfExtents.y - edgeMargin;

            float x, z;
            switch (Random.Range(0, 4))
            {
                case 0: // 위쪽 가장자리
                    x = Random.Range(-innerX, innerX);
                    z = innerZ;
                    break;
                case 1: // 아래쪽 가장자리
                    x = Random.Range(-innerX, innerX);
                    z = -innerZ;
                    break;
                case 2: // 왼쪽 가장자리
                    x = -innerX;
                    z = Random.Range(-innerZ, innerZ);
                    break;
                default: // 오른쪽 가장자리
                    x = innerX;
                    z = Random.Range(-innerZ, innerZ);
                    break;
            }

            return transform.position + new Vector3(x, 0f, z);
        }
    }
}
