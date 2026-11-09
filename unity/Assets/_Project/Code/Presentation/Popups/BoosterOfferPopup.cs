using System;
using Meowtosis.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    public interface IBoosterOfferPopup
    {
        bool IsOpen { get; }

        // Bấm nút mua (giá xu, kèm số lượt nhận được).
        event Action BuyClicked;

        void Show(BoosterKind kind, int price, int amount, int coins);

        // Không đủ xu: nút mua rung, dòng số xu đỏ lên.
        void ShowNotEnoughCoins();

        void Hide();
    }

    // Popup giới thiệu booster khi đã hết lượt (dựng trong scene Game, kiểu Bounce Blast): thẻ tím viền vàng, tab tên booster,
    // khung kem có icon xoay hào quang và lời giới thiệu, nút xanh giá xu kèm huy hiệu "x3", nút X đỏ để đóng.
    public sealed class BoosterOfferPopup : PopupView, IBoosterOfferPopup
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private Image icon;
        [SerializeField] private RectTransform rays;
        [SerializeField] private TMP_Text description;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Sprite mutateIcon;
        [SerializeField] private Sprite transplantIcon;
        [SerializeField] private Sprite hintIcon;
        [SerializeField] private float raysDegreesPerSecond = 22f;
        [SerializeField] private float iconBobHeight = 10f;
        [SerializeField] private float iconBobSeconds = 1.6f;
        [SerializeField] private Color coinsColor = new Color(0.36f, 0.2f, 0.12f);
        [SerializeField] private Color warningColor = new Color(0.85f, 0.2f, 0.18f);
        [SerializeField] private float shakeSeconds = 0.35f;

        private Vector2 iconHome;
        private Vector2 buyHome;
        private float shakeLeft;
        private int coins;

        public event Action BuyClicked;

        public void Show(BoosterKind kind, int price, int amount, int coinsOwned)
        {
            coins = coinsOwned;
            title.text = GameTexts.BoosterName(kind).ToUpper();
            icon.sprite = kind == BoosterKind.Mutate ? mutateIcon : kind == BoosterKind.Transplant ? transplantIcon : hintIcon;
            description.text = GameTexts.BoosterInfo(kind);
            priceText.text = price.ToString();
            amountText.text = "x" + amount;
            coinsText.text = GameTexts.CoinsOwned(coinsOwned);
            coinsText.color = coinsColor;
            Open();
        }

        public void ShowNotEnoughCoins()
        {
            coinsText.text = GameTexts.NotEnoughCoins(coins);
            coinsText.color = warningColor;
            shakeLeft = shakeSeconds;
        }

        private void Awake()
        {
            iconHome = icon.rectTransform.anchoredPosition;
            buyHome = ((RectTransform)buyButton.transform).anchoredPosition;
            buyButton.onClick.AddListener(() => BuyClicked?.Invoke());
            closeButton.onClick.AddListener(Hide);
        }

        private void Update()
        {
            if (rays != null) rays.Rotate(0f, 0f, -raysDegreesPerSecond * Time.unscaledDeltaTime);
            var bob = Mathf.Sin(Time.unscaledTime / iconBobSeconds * Mathf.PI * 2f) * iconBobHeight;
            icon.rectTransform.anchoredPosition = iconHome + new Vector2(0f, bob);
            var buy = (RectTransform)buyButton.transform;
            if (shakeLeft > 0f)
            {
                shakeLeft -= Time.unscaledDeltaTime;
                buy.anchoredPosition = buyHome + new Vector2(Mathf.Sin(shakeLeft * 60f) * 14f * (shakeLeft / shakeSeconds), 0f);
            }
            else buy.anchoredPosition = buyHome;
        }
    }
}
