using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 플레이어가 획득한 무기를 SaveHandler에 기록하고 조회한다.
    /// </summary>
    // 도감은 무기 ID 단위이며, 런 한정 값인 컨디션은 기록하지 않는다.
    public static class WeaponCollectionHandler
    {
        private const string KeyPrefix = "weapon_collection_";

        public static void RecordDiscovery(WeaponBase weapon)
        {
            if (!weapon || IsDiscovered(weapon)) return;

            SaveHandler.SetBool(KeyPrefix + weapon.Data.Id, true);
            Debug.Log($"[WeaponCollectionHandler] 새 무기 발견: {weapon.Data.DisplayName}");
        }

        public static bool IsDiscovered(WeaponBase weapon)
        {
            return weapon && SaveHandler.GetBool(KeyPrefix + weapon.Data.Id);
        }
    }
}
