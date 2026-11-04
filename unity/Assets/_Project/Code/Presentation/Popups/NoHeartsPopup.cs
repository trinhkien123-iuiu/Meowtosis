using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    public interface INoHeartsPopup
    {
        event Action RefillClicked;

        bool IsOpen { get; }

        void Show(string timer);

        void Hide();
    }

    // Popup hết tim: đếm ngược tới tim kế tiếp; nút nạp đầy chỉ có trong bản thử.
    public sealed class NoHeartsPopup : PopupView, INoHeartsPopup
    {
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Button refillButton;

        public event Action RefillClicked;

        public void Show(string timer)
        {
            timerText.text = GameTexts.NextHeart(timer);
            if (!IsOpen) Open();
        }

        private void Awake() => refillButton.onClick.AddListener(() => RefillClicked?.Invoke());
    }
}
