using UnityEngine;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 고정된 필드 스폰 지점에 주기적으로 무기를 보충한다(GDD 5.4). 각 지점의 재생성 타이머는 그 지점의 무기가 실제로 회수된 후에야 시작된다.
    /// </summary>
    // 등급(무기 고유 고정값)도 컨디션(스폰마다 랜덤)도 전부 WeaponDefinition/WeaponBase가 스스로 처리하므로, 이 클래스는 "어떤 프리팹을, 언제, 어디에" 스폰할지만 알면 된다. 등급 틴트조차 이 클래스가 다루지 않는다 - 등급이 고정값이라 런타임에 매번 다시 읽어 칠할 이유가 없으므로 프리팹 빌드 시점에 이미 구워져 있다(Stage1SceneBuilder 참고).
    public class WeaponSpawner : MonoBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private GameObject[] weaponPrefabs;
        [SerializeField] private float respawnInterval = 8f;

        private float[] _nextSpawnTime;
        private GameObject[] _occupants;

        private void Awake()
        {
            _nextSpawnTime = new float[spawnPoints.Length];
            _occupants = new GameObject[spawnPoints.Length];
        }

        private void Update()
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (_occupants[i]) continue;
                if (Time.time < _nextSpawnTime[i]) continue;

                SpawnAt(i);
            }
        }

        private void SpawnAt(int index)
        {
            GameObject prefab = weaponPrefabs[Random.Range(0, weaponPrefabs.Length)];
            GameObject instance = Instantiate(prefab, spawnPoints[index].position, Quaternion.identity);

            if (instance.TryGetComponent(out WeaponPickup pickup))
            {
                pickup.SourceSpawner = this;
                pickup.SpawnIndex = index;
            }

            _occupants[index] = instance;
        }

        public void NotifyVacated(int spawnIndex)
        {
            _occupants[spawnIndex] = null;
            _nextSpawnTime[spawnIndex] = Time.time + respawnInterval;
        }
    }
}
