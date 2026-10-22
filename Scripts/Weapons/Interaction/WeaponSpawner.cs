using UnityEngine;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 고정된 필드 스폰 지점에 무기를 보충한다. 재생성 타이머는 해당 무기가 회수된 후 시작된다.
    /// </summary>
    // 정적 데이터와 컨디션은 WeaponBase의 책임이며, 여기서는 스폰 일정과 위치만 관리한다.
    public class WeaponSpawner : MonoBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private WeaponRoster weaponRoster;
        [SerializeField] private float respawnInterval = 8f;

        private float[] _nextSpawnTime;
        private GameObject[] _spawnedWeapons;

        private void Awake()
        {
            if (!weaponRoster || weaponRoster.Weapons.Count == 0)
            {
                Debug.LogError("[WeaponSpawner] WeaponRoster에 필드 무기 프리팹이 없습니다.", this);
                enabled = false;
                return;
            }

            _nextSpawnTime = new float[spawnPoints.Length];
            _spawnedWeapons = new GameObject[spawnPoints.Length];
        }

        private void Update()
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (_spawnedWeapons[i]) continue;
                if (Time.time < _nextSpawnTime[i]) continue;

                SpawnWeaponAt(i);
            }
        }

        private void SpawnWeaponAt(int spawnPointIndex)
        {
            WeaponBase prefab = weaponRoster.Weapons[Random.Range(0, weaponRoster.Weapons.Count)];
            WeaponBase instance = Instantiate(prefab, spawnPoints[spawnPointIndex].position, Quaternion.identity);

            if (instance.TryGetComponent(out WeaponPickup pickup))
            {
                pickup.OwningSpawner = this;
                pickup.SpawnPointIndex = spawnPointIndex;
            }

            _spawnedWeapons[spawnPointIndex] = instance.gameObject;
        }

        public void NotifySpawnPointVacated(int spawnPointIndex)
        {
            _spawnedWeapons[spawnPointIndex] = null;
            _nextSpawnTime[spawnPointIndex] = Time.time + respawnInterval;
        }
    }
}
