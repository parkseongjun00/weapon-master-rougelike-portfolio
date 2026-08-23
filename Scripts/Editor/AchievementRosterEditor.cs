using UnityEditor;
using WeaponMaster.Achievements;

namespace WeaponMaster.EditorTools
{
    /// <summary>
    /// AchievementRoster 인스펙터에 "폴더 다시 스캔" 버튼 하나를 추가한다.
    /// </summary>
    [CustomEditor(typeof(AchievementRoster))]
    public class AchievementRosterEditor : RosterEditor<AchievementRoster, AchievementDefinition>
    {
        protected override string FolderPath => "Assets/Data/Achievements";
        protected override string FieldName => "achievements";
    }
}