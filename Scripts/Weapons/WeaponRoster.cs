using System.Collections.Generic;
using UnityEngine;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// "게임에 실제로 포함되는 무기 정의" 목록을 사람이 확정한 채로 보관하고, 각 무기의 발견 여부를 조회하는 단일 창구를 제공한다.
    /// </summary>
    // Assets/Data/Weapons 폴더를 런타임에 통째로 스캔하는 방식(Resources.LoadAll 등)은 쓰지 않는다 - 이 워크스페이스에서 Resources 폴더는 "게임에 쓸 수도 안 쓸 수도 있는 날것 자산 보관소"라는 별도 의미를 갖고 있어, 폴더 안의 모든 것을 자동 포함시키면 그 의미와 충돌한다. 대신 이 리스트에 명시적으로 등록된 것만 로스터로 취급하고, 채우는 작업은 WeaponRosterEditor(에디터 전용 버튼)가 폴더 스캔으로 대신해준다.
    [CreateAssetMenu(menuName = "Weapon Master/Weapon Roster", fileName = "WeaponRoster")]
    public class WeaponRoster : ScriptableObject
    {
        [SerializeField] private List<WeaponDefinition> weapons = new();

        public IReadOnlyList<WeaponDefinition> Weapons => weapons;

        /// <summary>
        /// Stage1SceneBuilder가 알고 있는 무기 정의가 리스트에 빠져 있으면 추가한다.
        /// </summary>
        // 리스트를 통째로 덮어쓰지 않는 이유는, 사용자가 에디터 버튼으로 손수 추가해둔 항목(빌더가 모르는 무기)을 재실행할 때마다 지우지 않기 위해서다.
        public void EnsureRegistered(WeaponDefinition definition)
        {
            if (!definition || weapons.Contains(definition)) return;
            weapons.Add(definition);
        }

        /// <summary>
        /// 도감 표시 코드가 호출할 단일 창구.
        /// </summary>
        // definition.StartsDiscovered(맨손처럼 발견 이벤트가 존재하지 않는 무기용)를 여기서 함께 확인하므로 호출부는 그 예외를 몰라도 된다.
        public bool IsDiscovered(WeaponDefinition definition)
        {
            if (!definition) return false;
            return definition.StartsDiscovered || WeaponCollectionHandler.IsDiscovered(definition);
        }
    }
}