using System.Collections.Generic;
using UnityEngine;

namespace WeaponMaster.Core
{
    /// <summary>
    /// 프리팹 하나를 Instantiate/Destroy 대신 체크아웃/반납으로 재사용한다.
    /// </summary>
    // 반복 생성·파괴 비용을 줄이며, 재사용 상태 초기화는 호출부가 명시적으로 처리한다.
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
