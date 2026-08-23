using UnityEditor;
using WeaponMaster.Characters;

namespace WeaponMaster.EditorTools
{
    /// <summary>
    /// CharacterRoster 인스펙터에 "폴더 다시 스캔" 버튼 하나를 추가한다.
    /// </summary>
    [CustomEditor(typeof(CharacterRoster))]
    public class CharacterRosterEditor : RosterEditor<CharacterRoster, CharacterDefinition>
    {
        protected override string FolderPath => "Assets/Data/Characters";
        protected override string FieldName => "characters";
    }
}
