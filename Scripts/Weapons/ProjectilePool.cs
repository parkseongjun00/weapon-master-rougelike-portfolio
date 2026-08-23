using System.Collections.Generic;
using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 발사체 프리팹별로 SimpleObjectPool을 생성/캐시한다.
    /// </summary>
    // WeaponDefinition이 발사체 프리팹을 데이터로 갖고 있어 무기 종류가 늘어나면 발사체 프리팹도 늘어날 수 있으므로, 프리팹 하나만 가정하지 않고 프리팹을 키로 하는 딕셔너리로 관리한다.
    public class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private int poolPrewarmCount = 20;

        private readonly Dictionary<Projectile, SimpleObjectPool<Projectile>> _pools = new();

        public static ProjectilePool Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public Projectile Get(Projectile prefab, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(prefab, out SimpleObjectPool<Projectile> pool))
            {
                pool = new SimpleObjectPool<Projectile>(prefab, transform, poolPrewarmCount);
                _pools[prefab] = pool;
            }

            Projectile instance = pool.Get(position, rotation);
            instance.SetPool(pool);
            return instance;
        }
    }
}
