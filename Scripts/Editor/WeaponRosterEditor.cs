using UnityEditor;
using WeaponMaster.Weapons;

namespace WeaponMaster.EditorTools
{
    /// <summary>
    /// WeaponRoster 인스펙터에 "폴더 다시 스캔" 버튼 하나를 추가한다.
    /// </summary>
    [CustomEditor(typeof(WeaponRoster))]
    public class WeaponRosterEditor : RosterEditor<WeaponRoster, WeaponDefinition>
    {
        protected override string FolderPath => "Assets/Data/Weapons";
        protected override string FieldName => "weapons";
    }
}