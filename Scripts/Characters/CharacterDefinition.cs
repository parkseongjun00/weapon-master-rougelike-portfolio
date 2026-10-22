using UnityEngine;

namespace WeaponMaster.Characters
{
    /// <summary>
    /// 플레이어블 캐릭터 하나의 정의 - 겉모습/신체(프리팹)와 애니메이션을 담는다.
    /// </summary>
    [CreateAssetMenu(menuName = "Weapon Master/Character Definition", fileName = "CharacterDefinition")]
    public class CharacterDefinition : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private GameObject characterPrefab;
        [SerializeField] private AnimatorOverrideController animatorOverrideController;

        public string DisplayName => displayName;
        public GameObject CharacterPrefab => characterPrefab;

        // 비어 있으면 공용 베이스 컨트롤러를 사용한다. 상태 그래프가 다르면 별도 베이스가 필요하다.
        public AnimatorOverrideController AnimatorOverrideController => animatorOverrideController;
    }
}
