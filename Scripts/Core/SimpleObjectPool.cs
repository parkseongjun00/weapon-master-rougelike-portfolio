using System.Collections.Generic;
using UnityEngine;

namespace WeaponMaster.Core
{
    /// <summary>
    /// 프리팹 하나를 Instantiate/Destroy 대신 체크아웃/반납으로 재사용한다.
    /// </summary>
    // 호드 서바이벌 장르 + WebGL 타겟 특성상 반복적인 Instantiate/Destroy의 GC 압박을 줄이기 위해 도입했다. 리셋 훅 인터페이스는 두지 않는다 - 재사용 시 필요한 상태 초기화는 호출부가 명시적으로 처리한다.
    public class SimpleObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _inactive = new Stack<T>();

        public SimpleObjectPool(T prefab, Transform parent = null, int prewarmCount = 0)
        {
            this._prefab = prefab;
            this._parent = parent;

            for (int i = 0; i < prewarmCount; i++)
            {
                T instance = CreateInstance();
                instance.gameObject.SetActive(false);
                _inactive.Push(instance);
            }
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            T instance = _inactive.Count > 0 ? _inactive.Pop() : CreateInstance();
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Release(T instance)
        {
            instance.gameObject.SetActive(false);
            _inactive.Push(instance);
        }

        private T CreateInstance()
        {
            return Object.Instantiate(_prefab, _parent);
        }
    }
}
