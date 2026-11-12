using System;
using System.Collections;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Màn Home dựng sẵn trong scene Home: ba trang Shop, Home, Setting trượt ngang (HomePager) và thanh dưới (HomeBottomBar).
    public sealed class HomeView : MonoBehaviour, IHomeView
    {
        [SerializeField] private HomePager pager;
        [SerializeField] private HomeBottomBar bottomBar;

        [Header("Trang Home")]
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private Button coinsShopButton;
        [SerializeField] private TMP_Text heartCountText;
        [SerializeField] private TMP_Text heartStatusText;
        [SerializeField] private Button playButton;
        [SerializeField] private TMP_Text levelText;

        [Header("Avatar")]
        [SerializeField] private Button avatarButton;
        [SerializeField] private AvatarSlot avatarSlot;
        [SerializeField] private AvatarPopup avatarPopup;

        [Header("Trang Setting")]
        [SerializeField] private HomeToggleRow soundRow;
        [SerializeField] private HomeToggleRow musicRow;
        [Header("Shop illustration")]
        [SerializeField] private RectTransform shopIllustration;
        [SerializeField] private SkeletonDataAsset shopIllustrationData;
        [SerializeField] private Material shopIllustrationMaterial;
        private bool avatarOpening;

        public event Action PlayClicked;

        public event Action<HomeTab> TabClicked;

        public event Action SoundToggled;

        public event Action MusicToggled;

        public event Action AvatarClicked;

        public event Action<string> AvatarChosen;

        public void ShowCoins(int coins) => coinsText.text = coins.ToString();

        public void ShowHearts(int hearts, string status)
        {
            heartCountText.text = hearts.ToString();
            heartStatusText.text = status;
        }

        public void ShowLevel(int levelNumber) => levelText.text = GameTexts.LevelBadge(levelNumber);

        public void ShowTab(HomeTab tab, bool animate) => pager.Show((int)tab, animate);

        public void SetSwipeEnabled(bool isEnabled) => pager.IsLocked = !isEnabled;

        public void ShowAudio(bool soundOn, bool musicOn)
        {
            soundRow.Show(soundOn);
            musicRow.Show(musicOn);
        }

        public void ShowAvatar(string avatarId)
        {
            avatarSlot.Show(avatarId);
        }

        public void ShowAvatarPicker(string selectedId) => avatarPopup.Show(selectedId);

        private void Awake()
        {
            InitializeShopIllustration();
            if (coinsShopButton != null)
                coinsShopButton.onClick.AddListener(() => TabClicked?.Invoke(HomeTab.Shop));
            avatarButton.onClick.AddListener(OnAvatarButtonClicked);
            avatarPopup.Chosen += id => AvatarChosen?.Invoke(id);
            playButton.onClick.AddListener(() => PlayClicked?.Invoke());
            bottomBar.TabClicked += index => TabClicked?.Invoke((HomeTab)index);
            pager.PositionChanged += bottomBar.ShowPosition;
            soundRow.Clicked += () => SoundToggled?.Invoke();
            musicRow.Clicked += () => MusicToggled?.Invoke();
        }

        private void InitializeShopIllustration()
        {
            if (shopIllustration == null || shopIllustrationData == null || shopIllustrationMaterial == null) return;
            var data = shopIllustrationData.GetSkeletonData(true);
            if (data == null) return;
            var graphic = SkeletonGraphic.NewSkeletonGraphicGameObject(shopIllustrationData, shopIllustration, shopIllustrationMaterial);
            graphic.raycastTarget = false;
            graphic.UnscaledTime = true;
            var pixels = shopIllustrationData.scale * (graphic.canvas != null ? graphic.canvas.referencePixelsPerUnit : 100f);
            var scale = Mathf.Min(shopIllustration.rect.width / Mathf.Max(1f, data.Width * pixels),
                shopIllustration.rect.height / Mathf.Max(1f, data.Height * pixels));
            graphic.rectTransform.localScale = Vector3.one * scale;
            graphic.rectTransform.anchorMin = graphic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            graphic.rectTransform.anchoredPosition = -new Vector2(data.X + data.Width * 0.5f, data.Y + data.Height * 0.5f) * (pixels * scale);
            graphic.AnimationState.SetAnimation(0, "shop_on_idle", true);
            var previous = shopIllustration.GetComponent<Image>();
            if (previous != null) previous.enabled = false;
        }

        private void OnAvatarButtonClicked()
        {
            if (avatarOpening || avatarPopup.IsOpen) return;
            StartCoroutine(OpenAvatarWithFeedback());
        }

        private IEnumerator OpenAvatarWithFeedback()
        {
            avatarOpening = true;
            yield return avatarSlot.PlayClickFeedback();
            avatarOpening = false;
            if (avatarButton.gameObject.activeInHierarchy) AvatarClicked?.Invoke();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            avatarOpening = false;
            if (avatarSlot != null) avatarSlot.ResetClickFeedback();
        }
    }
}
