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

        void Show(string message, int revivesLeft, bool canRevive);

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

        public void Show(string message, int revivesLeft, bool canRevive)
        {
            messageText.text = message;
            reviveCountText.text = revivesLeft.ToString();
            reviveButton.interactable = canRevive;
            Open();
        }

        private void Awake()
        {
            reviveButton.onClick.AddListener(() => ReviveClicked?.Invoke());
            restartButton.onClick.AddListener(() => RestartClicked?.Invoke());
        }
    }
}
