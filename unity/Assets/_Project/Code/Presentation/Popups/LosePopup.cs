using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    public interface ILosePopup
    {
        event Action ReviveClicked;

        event Action RestartClicked;

        void Show(string message, int revivesLeft, bool canRevive, bool showButtons = true);

        void SetButtonsVisible(bool visible);

        void Hide();
    }

    // Popup thua: lý do thua (màu nào hết mèo), nút Hồi sinh (kèm số lượt còn) và Chơi lại.
    public sealed class LosePopup : PopupView, ILosePopup
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button reviveButton;
        [SerializeField] private TMP_Text reviveCountText;
        [SerializeField] private Button restartButton;

        public event Action ReviveClicked;

        public event Action RestartClicked;

        public void Show(string message, int revivesLeft, bool canRevive, bool showButtons = true)
        {
            messageText.text = message;
            if (reviveCountText != null) reviveCountText.text = revivesLeft.ToString();
            if (reviveButton != null) reviveButton.interactable = canRevive;
            SetButtonsVisible(showButtons);
            Open();
        }

        public void SetButtonsVisible(bool visible)
        {
            if (reviveButton != null) reviveButton.gameObject.SetActive(visible);
            if (restartButton != null) restartButton.gameObject.SetActive(visible);
        }

        private void Awake()
        {
            reviveButton.onClick.AddListener(() => ReviveClicked?.Invoke());
            restartButton.onClick.AddListener(() => RestartClicked?.Invoke());
        }
    }
}
