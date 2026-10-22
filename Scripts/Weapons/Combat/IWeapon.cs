using UnityEngine;

namespace WeaponMaster.Weapons
{
    public interface IWeapon
    {
        /// <summary>
        /// 무기의 공격 카테고리다.
        /// </summary>
        WeaponCategory Category { get; }

        /// <summary>
        /// 공격속도 배율이 적용되기 전의 저작 쿨다운 값이다.
        /// </summary>
        float Cooldown { get; }

        bool IsAttacking { get; }
        bool CanAttack { get; }
        void SetAttackContext(Component owner, LayerMask targetLayers, int attackLayer);
        void ResetForReuse();

        /// <summary>
        /// 공격을 시도한다.
        /// </summary>
        /// <param name="damageMultiplier">호출자가 정한 피해 배율.</param>
        /// <param name="attackSpeedMultiplier">호출자가 정한 공격 속도 배율.</param>
        /// <returns>공격이 실제로 실행됐는지 여부(쿨다운 중이면 false).</returns>
        bool TryStartAttack(Vector3 originPosition, Vector3 aimDirection, float damageMultiplier, float attackSpeedMultiplier);

        /// <summary>
        /// 근접 공격의 활성 타격 구간을 시작한다.
        /// </summary>
        /// <param name="originPosition">히트박스가 없는 임시 근접 공격의 판정 원점.</param>
        /// <param name="aimDirection">히트박스가 없는 임시 근접 공격의 조준 방향.</param>
        void EnableMeleeHitbox(Vector3 originPosition, Vector3 aimDirection, float damageMultiplier);

        /// <summary>
        /// 근접 공격의 활성 타격 구간을 끝낸다.
        /// </summary>
        void DisableMeleeHitbox();

        /// <summary>
        /// 근접 공격을 안전하게 끝낸다.
        /// </summary>
        void EndMeleeAttack();

        /// <summary>
        /// 진행 중이거나 대기 중인 근접 공격을 취소한다.
        /// </summary>
        void CancelAttack();
    }
}
