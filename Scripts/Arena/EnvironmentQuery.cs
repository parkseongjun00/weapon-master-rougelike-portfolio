using UnityEngine;

namespace WeaponMaster.Arena
{
    /// <summary>
    /// 수직 캡슐 몸체의 경계·환경 질의를 제공한다.
    /// </summary>
    public static class EnvironmentQuery
    {
        /// <summary>
        /// 현재 적의 수직 캡슐을 월드 크기로 표현한다.
        /// </summary>
        public readonly struct Body
        {
            public readonly float Radius;
            public readonly float Height;
            public readonly float CenterY;

            public Body(CapsuleCollider capsule)
            {
                Vector3 scale = capsule.transform.lossyScale;
                Radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                Height = Mathf.Max(Radius * 2f, capsule.height * Mathf.Abs(scale.y));
                CenterY = capsule.center.y * scale.y;
            }

            internal void Capsule(Vector3 position, out Vector3 bottom, out Vector3 top)
            {
                Vector3 center = position + Vector3.up * CenterY;
                float line = Mathf.Max(0f, Height * 0.5f - Radius);
                bottom = center - Vector3.up * line;
                top = center + Vector3.up * line;
            }
        }

        /// <summary>
        /// 몸체의 수평 반경까지 맵 경계 안인지 확인한다.
        /// </summary>
        public static bool IsInside(ArenaBounds arena, Vector3 position, float radius)
        {
            if (!arena) return true;
            Vector3 local = position - arena.transform.position;
            return Mathf.Abs(local.x) + radius <= arena.HalfExtents.x
                && Mathf.Abs(local.z) + radius <= arena.HalfExtents.y;
        }

        /// <summary>
        /// 지정한 레이어와 몸체가 겹치는지 확인한다.
        /// </summary>
        public static bool IsClear(Vector3 position, Body body, LayerMask mask, QueryTriggerInteraction triggers = QueryTriggerInteraction.Ignore)
        {
            body.Capsule(position, out Vector3 bottom, out Vector3 top);
            return !Physics.CheckCapsule(bottom, top, body.Radius, mask, triggers);
        }

        /// <summary>
        /// 경계와 장애물 앞까지 이동할 수 있는 거리를 반환한다.
        /// </summary>
        public static float ClearDistance(ArenaBounds arena, Vector3 position, Vector3 delta, Body body, LayerMask mask)
        {
            float distance = delta.magnitude;
            if (distance < 0.00001f) return 0f;
            Vector3 direction = delta / distance;
            if (arena)
            {
                Vector3 local = position - arena.transform.position;
                Vector2 limits = arena.HalfExtents - Vector2.one * body.Radius;
                if (Mathf.Abs(direction.x) > 0.00001f)
                    distance = Mathf.Min(distance, (Mathf.Sign(direction.x) * limits.x - local.x) / direction.x);
                if (Mathf.Abs(direction.z) > 0.00001f)
                    distance = Mathf.Min(distance, (Mathf.Sign(direction.z) * limits.y - local.z) / direction.z);
            }
            distance = Mathf.Max(0f, distance);
            if (distance <= 0f) return 0f;
            body.Capsule(position, out Vector3 bottom, out Vector3 top);
            if (Physics.CapsuleCast(bottom, top, body.Radius, direction, out RaycastHit hit, distance, mask, QueryTriggerInteraction.Ignore))
                distance = Mathf.Max(0f, hit.distance - 0.005f);
            return distance;
        }
    }
}
