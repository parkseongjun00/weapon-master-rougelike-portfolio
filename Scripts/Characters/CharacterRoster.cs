using System.Collections.Generic;
using UnityEngine;

namespace WeaponMaster.Characters
{
    /// <summary>
    /// 게임에 실제로 포함되는 플레이어블 캐릭터 정의 목록을 보관한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Weapon Master/Character Roster", fileName = "CharacterRoster")]
    public class CharacterRoster : ScriptableObject
    {
        [SerializeField] private List<CharacterDefinition> characters = new();

        public IReadOnlyList<CharacterDefinition> Characters => characters;

        public void EnsureRegistered(CharacterDefinition definition)
        {
            if (!definition || characters.Contains(definition)) return;
            characters.Add(definition);
        }
    }
}