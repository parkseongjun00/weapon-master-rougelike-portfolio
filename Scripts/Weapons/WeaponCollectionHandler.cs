using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 플레이어가 실제로 만난 적 있는 고유 무기 정의를 SaveHandler에 기록/조회한다.
    /// </summary>
    // 도감 슬롯은 무기 정의(WeaponDefinition) 하나당 하나이고, 컨디션은 런 한정 값이라 기록 대상이 아니다. static 접근 가능 + 씬 컴포넌트로 실체화되지 않는 순수 static 클래스라 네이밍 컨벤션상 ~Handler 접미사를 쓴다.
    public static class WeaponCollectionHandler
    {
        private const string KeyPrefix = "weapon_collection_";

        public static void RecordDiscovery(WeaponDefinition definition)
        {
            if (!definition || IsDiscovered(definition)) return;

            SaveHandler.SetBool(KeyPrefix + definition.name, true);
            Debug.Log($"[WeaponCollectionHandler] 새 무기 발견: {definition.DisplayName}");
        }

        public static bool IsDiscovered(WeaponDefinition definition)
        {
            return definition && SaveHandler.GetBool(KeyPrefix + definition.name);
        }
    }
}