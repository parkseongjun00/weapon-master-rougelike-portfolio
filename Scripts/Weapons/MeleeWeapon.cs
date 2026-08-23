using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 근접 무기 - 사거리 내 모든 적을 동시에 타격하며(멀티히트), 실제로 명중했을 때만 내구도를 소모한다(GDD 5.3/6.4).
    /// </summary>
    // 무기 없는 "맨손" 공격에도 수정 없이 그대로 사용된다(Stage1SceneBuilder) - 증강이 이 하나의 공용 클래스를 통해 적용되므로, 글로벌 증강이 별도 코드 경로 없이도 맨손 공격에 자동으로 반영된다.
    public class MeleeWeapon : WeaponBase
    {
        private static readonly Collider[] HitBuffer = new Collider[16];

        protected override void PerformAttack(Vector3 originPosition, Vector3 aimDirection, float effectiveDamage)
        {
            Vector3 center = originPosition + aimDirection.normalized * definition.Range;
            int count = Physics.OverlapSphereNonAlloc(center, definition.HitRadius, HitBuffer, ~0, QueryTriggerInteraction.Collide);

            bool hitSomething = false;
            Transform wielderRoot = transform.root;

            for (int i = 0; i < count; i++)
            {
                Collider hit = HitBuffer[i];
                if (hit.transform.root == wielderRoot) continue;

                if (hit.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(effectiveDamage);
                    hitSomething = true;
                }
            }

            if (hitSomething) Durability.Consume();
        }
    }
}
