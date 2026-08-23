using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WeaponMaster.UI
{
    /// <summary>
    /// 증강 3택 팝업의 시각 표현 - 버튼/라벨을 켜고 끄고, 클릭을 이벤트로 알린다. 어떤 후보가 자격이 있는지, 선택 후 무슨 일이 벌어지는지는 모른다.
    /// </summary>
    // 나중에 이 팝업에 비주얼(페이드/하이라이트 등)을 추가한다면 Time.deltaTime이 아니라 Time.unscaledDeltaTime을 써야 한다 - 팝업이 떠 있는 동안은 게임이 일시정지 상태(Time.timeScale=0)라서다.
    public class AugmentPopupView : MonoBehaviour
    {
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private TMP_Text[] optionLabels;

        public event Action<int> OptionChosen;

        private void Awake()
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                optionButtons[i].onClick.AddListener(() => OptionChosen?.Invoke(index));
            }
        }

        public void Show(IReadOnlyList<AugmentPopupOption> options)
        {
            gameObject.SetActive(true);

            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool active = i < options.Count;
                optionButtons[i].gameObject.SetActive(active);
                if (active)
                {
                    AugmentPopupOption option = options[i];
                    optionLabels[i].text = $"{option.DisplayName} (Lv.{option.Level})";
                }
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
