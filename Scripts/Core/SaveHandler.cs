using UnityEngine;

namespace WeaponMaster.Core
{
    /// <summary>
    /// PlayerPrefs 접근을 통합하며, Set 계열 호출을 즉시 저장소에 반영한다.
    /// </summary>
    // 저장 백엔드 교체 범위를 이 클래스 하나로 제한한다. 키의 소유권은 각 호출부에 있다.
    // 저장 빈도가 낮아 Set마다 flush하여 호출부의 저장 누락을 방지한다.
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
