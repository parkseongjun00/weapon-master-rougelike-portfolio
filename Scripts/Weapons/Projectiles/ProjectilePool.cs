using System.Collections.Generic;
using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.Weapons
{
    /// <summary>
    /// 발사체 프리팹별로 SimpleObjectPool을 생성/캐시한다.
    /// </summary>
    // 무기별로 다른 발사체를 지원하기 위해 프리팹을 키로 풀을 분리한다.
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

        public Projectile Spawn(Projectile prefab, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(prefab, out SimpleObjectPool<Projectile> pool))
            {
                pool = new SimpleObjectPool<Projectile>(prefab, transform, poolPrewarmCount);
                _pools[prefab] = pool;
            }

            Projectile instance = pool.Get(position, rotation);
            instance.SetOwningPool(pool);
            return instance;
        }
    }
}
