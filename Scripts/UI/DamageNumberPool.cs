using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.UI
{
    /// <summary>
    /// DamageNumberPopup 풀을 감싸는 정적 싱글턴. 게임에는 이 풀이 하나뿐이다.
    /// </summary>
    public class DamageNumberPool : MonoBehaviour
    {
        [SerializeField] private DamageNumberPopup popupPrefab;
        // 부족하면 SimpleObjectPool.Get()이 그때그때 Instantiate로 채우고 이후 재사용되므로
        // (풀은 상한이 아니라 최소 보장치), 낮게 잡고 실측하며 올리는 쪽을 택했다 - 초반/중반
        // 전투에서 쓰이지도 않을 인스턴스를 시작부터 만들어 두는 낭비를 피한다.
        [SerializeField] private int poolPrewarmCount = 5;
        // 카메라가 고정 각도라 이 값도 고정이다 - 스폰 시점에 한 번만 적용하면 되므로, 매 프레임 되돌려주는 FixedWorldRotation 없이 여기서 바로 굽는다.
        [SerializeField] private Vector3 fixedWorldEulerAngles;

        private SimpleObjectPool<DamageNumberPopup> _pool;

        public static DamageNumberPool Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            _pool = new SimpleObjectPool<DamageNumberPopup>(popupPrefab, transform, poolPrewarmCount);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Spawn(Vector3 position, int amount)
        {
            DamageNumberPopup popup = _pool.Get(position, Quaternion.Euler(fixedWorldEulerAngles));
            popup.SetPool(_pool);
            popup.Show(position, amount);
        }
    }
}
