using UnityEngine;

namespace WeaponMaster.Weapons
{
    public interface IWeapon
    {
        /// <summary>
        /// 애니메이션 브릿지 등이 현재 무기의 카테고리를 알아야 할 때 참조한다.
        /// </summary>
        WeaponCategory Category { get; }

        /// <summary>
        /// 공격을 시도한다.
        /// </summary>
        /// <param name="damageMultiplier">증강 등으로 정해지는 배율을 호출부(PlayerWeaponController)가 실어서 넘긴다.</param>
        /// <param name="attackSpeedMultiplier">증강 등으로 정해지는 배율을 호출부(PlayerWeaponController)가 실어서 넘긴다.</param>
        /// <returns>공격이 실제로 실행됐는지 여부(쿨다운 중이면 false).</returns>
        bool TryAttack(Vector3 originPosition, Vector3 aimDirection, float damageMultiplier, float attackSpeedMultiplier);
    }
}
