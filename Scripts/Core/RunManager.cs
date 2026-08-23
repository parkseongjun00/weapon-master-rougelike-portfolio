using UnityEngine;
using UnityEngine.SceneManagement;

namespace WeaponMaster.Core
{
    /// <summary>
    /// 플레이어의 사망을 감지하고 런을 재시작한다.
    /// </summary>
    // 생존 시간/킬 수 추적은 RunRecordManager가 맡는다 - 여기서는 재시작만 담당한다.
    public class RunManager : MonoBehaviour
    {
        [SerializeField] private HealthComponent playerHealth;
        [SerializeField] private float restartDelay = 1f;

        private void OnEnable()
        {
            playerHealth.OnDeath += HandlePlayerDeath;
        }

        private void OnDisable()
        {
            playerHealth.OnDeath -= HandlePlayerDeath;
        }

        private void HandlePlayerDeath()
        {
            Invoke(nameof(RestartRun), restartDelay);
        }

        private void RestartRun()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }
    }
}
