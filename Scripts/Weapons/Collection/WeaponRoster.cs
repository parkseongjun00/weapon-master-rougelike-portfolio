using System.Collections.Generic;
using UnityEngine;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 필드에 등장하는 무기 프리팹 목록과 발견 여부 조회를 제공한다.
    /// </summary>
    // 런타임 폴더 스캔 없이 명시적으로 등록된 프리팹만 사용하며, 목록 갱신은 에디터 도구가 담당한다.
    [CreateAssetMenu(menuName = "Weapon Master/Weapon Roster", fileName = "WeaponRoster")]
    public class WeaponRoster : ScriptableObject
    {
        [SerializeField] private List<WeaponBase> weapons = new();

        public IReadOnlyList<WeaponBase> Weapons => weapons;

        /// <summary>
        /// Stage1SceneBuilder가 알고 있는 필드 무기 프리팹이 리스트에 빠져 있으면 추가한다.
        /// </summary>
        // 리스트를 통째로 덮어쓰지 않는 이유는, 사용자가 에디터 버튼으로 손수 추가해둔 항목(빌더가 모르는 무기)을 재실행할 때마다 지우지 않기 위해서다.
        public void EnsureRegistered(WeaponBase weaponPrefab)
        {
            if (!weaponPrefab || weapons.Contains(weaponPrefab)) return;
            weapons.Add(weaponPrefab);
        }

        /// <summary>
        /// 해당 무기의 도감 발견 여부를 반환한다.
        /// </summary>
        public bool IsDiscovered(WeaponBase weaponPrefab)
        {
            if (!weaponPrefab) return false;
            return weaponPrefab.Data.StartsDiscovered || WeaponCollectionHandler.IsDiscovered(weaponPrefab);
        }
    }
}
