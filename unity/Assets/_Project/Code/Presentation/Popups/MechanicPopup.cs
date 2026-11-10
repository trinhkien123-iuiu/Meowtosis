using System;
using Meowtosis.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    public interface IMechanicPopup
    {
        event Action ContinueClicked;

        bool IsOpen { get; }

        void Show(Mechanic mechanic);

        void Hide();
    }

    // Thẻ "Cơ chế mới" hiện lúc mở level có cơ chế người chơi chưa gặp: tên cơ chế, minh hoạ chạy lặp, lời giải thích.
    public sealed class MechanicPopup : PopupView, IMechanicPopup
    {
        [SerializeField] private MechanicCard[] cards = Array.Empty<MechanicCard>();
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private MechanicDemoView demo;
        [SerializeField] private Button continueButton;

        public event Action ContinueClicked;

        public void Show(Mechanic mechanic)
        {
            var card = Array.Find(cards, candidate => candidate.Mechanic == mechanic);
            if (card == null) return;
            titleText.text = card.Title;
            descriptionText.text = card.Description;
            Open();
            demo.Play(card.CreateDemoLevel(), card.DemoMoves);
        }

        private void Awake() => continueButton.onClick.AddListener(() => ContinueClicked?.Invoke());
    }
}
