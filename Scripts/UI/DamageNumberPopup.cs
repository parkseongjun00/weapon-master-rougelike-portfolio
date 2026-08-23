using TMPro;
using UnityEngine;
using WeaponMaster.Core;

namespace WeaponMaster.UI
{
    /// <summary>
    /// 피격 지점에서 위로 떠오르며 페이드 아웃하는 데미지 숫자 하나.
    /// </summary>
    // Canvas 기반 UGUI가 아니라 순수 TextMeshPro(3D 월드 텍스트)로 구현한다. 지속시간/속도는 placeholder이며 플레이테스트하며 조정 대상.
    public class DamageNumberPopup : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.8f;
        [SerializeField] private float riseSpeed = 1.5f;

        private TextMeshPro _text;
        private SimpleObjectPool<DamageNumberPopup> _pool;
        private float _elapsed;

        private void Awake()
        {
            _text = GetComponent<TextMeshPro>();
        }

        public void SetPool(SimpleObjectPool<DamageNumberPopup> owningPool)
        {
            _pool = owningPool;
        }

        public void Show(Vector3 position, int amount)
        {
            transform.position = position;
            _text.text = amount.ToString();

            Color color = _text.color;
            color.a = 1f;
            _text.color = color;

            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

            float t = Mathf.Clamp01(_elapsed / lifetime);
            Color color = _text.color;
            color.a = 1f - t;
            _text.color = color;

            if (_elapsed >= lifetime)
            {
                _pool.Release(this);
            }
        }
    }
}
