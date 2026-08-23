using System.Collections.Generic;
using UnityEngine;

namespace WeaponMaster.Achievements
{
    /// <summary>
    /// "게임에 실제로 포함되는 칭호 정의" 목록을 사람이 확정한 채로 보관한다.
    /// </summary>
    // 폴더를 런타임에 스캔하지 않고, 이 리스트에 명시적으로 등록된 것만 다룬다 - 채우는 건 AchievementRosterEditor(에디터 버튼) 몫.
    [CreateAssetMenu(menuName = "Weapon Master/Achievement Roster", fileName = "AchievementRoster")]
    public class AchievementRoster : ScriptableObject
    {
        [SerializeField] private List<AchievementDefinition> achievements = new();

        public IReadOnlyList<AchievementDefinition> Achievements => achievements;

        /// <summary>
        /// Stage1SceneBuilder가 아는 칭호 정의가 리스트에 빠져 있으면 추가한다.
        /// </summary>
        // 리스트를 통째로 덮어쓰지 않는 이유: 에디터 버튼을 다시 눌러도 기존에 추가해둔 항목이 지워지지 않게 하기 위함.
        public void EnsureRegistered(AchievementDefinition definition)
        {
            if (!definition || achievements.Contains(definition)) return;
            achievements.Add(definition);
        }
    }
}
