using UnityEditor;
using WeaponMaster.Augments;

namespace WeaponMaster.EditorTools
{
    /// <summary>
    /// AugmentRoster 인스펙터에 "폴더 다시 스캔" 버튼 하나를 추가한다.
    /// </summary>
    [CustomEditor(typeof(AugmentRoster))]
    public class AugmentRosterEditor : RosterEditor<AugmentRoster, AugmentDefinition>
    {
        protected override string FolderPath => "Assets/Data/Augments";
        protected override string FieldName => "augments";
    }
}