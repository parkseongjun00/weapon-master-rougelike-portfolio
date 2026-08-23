using UnityEngine;

namespace WeaponMaster.Core
{
    /// <summary>
    /// PlayerPrefs 직접 호출을 이 클래스 하나로 몰아, 저장 백엔드를 통째로 교체해야 할 때 호출부를 건드리지 않아도 되게 한다. Set 계열은 호출 즉시 PlayerPrefs.Save()까지 실행한다(자동 flush).
    /// </summary>
    // WebGL의 PlayerPrefs/IndexedDB 저장 신뢰성 리스크에 대비해 저장 창구를 여기 하나로 통일했다. 키 이름 상수는 이 클래스가 소유하지 않고 각 호출부가 소유한다.
    // 저장이 일어나는 시점(런 종료 등)이 매우 드물어 매번 flush해도 비용 부담이 없고, 호출부가 Flush를 깜빡해 저장이 유실되는 실패를 원천 차단한다.
    public static class SaveHandler
    {
        public static void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public static void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        }

        public static float GetFloat(string key, float defaultValue = 0f)
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        public static void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        public static string GetString(string key, string defaultValue = "")
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        /// <summary>
        /// PlayerPrefs에 bool 전용 API가 없어 int 0/1로 인코딩한다.
        /// </summary>
        public static void SetBool(string key, bool value)
        {
            SetInt(key, value ? 1 : 0);
        }

        public static bool GetBool(string key, bool defaultValue = false)
        {
            return GetInt(key, defaultValue ? 1 : 0) != 0;
        }
    }
}
