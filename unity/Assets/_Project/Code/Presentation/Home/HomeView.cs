using System;
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
            avatarButton.onClick.AddListener(() => AvatarClicked?.Invoke());
            avatarPopup.Chosen += id => AvatarChosen?.Invoke(id);
            playButton.onClick.AddListener(() => PlayClicked?.Invoke());
            bottomBar.TabClicked += index => TabClicked?.Invoke((HomeTab)index);
            pager.PositionChanged += bottomBar.ShowPosition;
            soundRow.Clicked += () => SoundToggled?.Invoke();
            musicRow.Clicked += () => MusicToggled?.Invoke();
        }
    }
}
