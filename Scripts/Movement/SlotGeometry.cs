using System;
using UnityEngine;

namespace WeaponMaster.Movement
{
    public enum RadiusSource { Auto, Manual }

    /// <summary>
    /// 슬롯 배치에 필요한 몸체 치수와 원의 수용량을 계산한다.
    /// </summary>
    public static class SlotGeometry
    {
        /// <summary>
        /// 루트 몸체 또는 수동 값에서 수평 반경을 읽는다.
        /// </summary>
        public static bool TryReadRadius(GameObject owner, RadiusSource source, float manual, out float radius)
        {
            radius = manual;
            if (source == RadiusSource.Auto)
            {
                if (owner.TryGetComponent(out CharacterController controller))
                {
                    Vector3 scale = controller.transform.lossyScale;
                    radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                }
                else if (owner.TryGetComponent(out CapsuleCollider capsule) && capsule.direction == 1)
                {
                    Vector3 scale = capsule.transform.lossyScale;
                    radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                }
                else return false;
            }
            return IsPositive(radius);
        }

        /// <summary>
        /// 인접 중심의 직선 간격을 만족하는 원의 최대 슬롯 수를 반환한다.
        /// </summary>
        public static int Capacity(float radius, float distance, int limit)
        {
            if (!IsPositive(radius) || !IsPositive(distance) || limit < 1) return 0;
            if (2d * radius < distance || limit == 1) return 1;
            double angle = Math.Asin(Math.Min(1d, distance / (2d * radius)));
            int count = (int)Math.Min(limit, Math.Floor(Math.PI / angle));
            while (count > 1 && 2d * radius * Math.Sin(Math.PI / count) < distance) count--;
            return Mathf.Max(1, count);
        }

        /// <summary>
        /// 반경·간격 입력이 유한한 양수인지 확인한다.
        /// </summary>
        public static bool IsPositive(float value) => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
