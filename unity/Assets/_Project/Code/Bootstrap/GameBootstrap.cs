using Meowtosis.Domain;
using Meowtosis.Gameplay;
using Meowtosis.Presentation;
using UnityEngine;

namespace Meowtosis.Bootstrap
{
    // Điểm khởi đầu của scene Game: nơi duy nhất tạo bộ luật, đọc danh sách level, nạp tiến độ
    // và nối presenter với các view trong scene.
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private TextAsset levelCatalog;
        [SerializeField] private BoardView boardView;
        [SerializeField] private HudView hudView;
        [SerializeField] private WinPopup winPopup;
        [SerializeField] private LosePopup losePopup;
        [SerializeField] private SettingsPopup settingsPopup;
        [SerializeField] private NoHeartsPopup noHeartsPopup;
        [SerializeField] private MechanicPopup mechanicPopup;
        [SerializeField] private SoundPlayer soundPlayer;
        [SerializeField] private AmbientEvents ambientEvents;
        [Tooltip("Để test: lớn hơn 0 thì luôn mở level này thay vì level chơi gần nhất.")]
        [SerializeField, Min(0)] private int openLevelNumber;

        private GamePresenter presenter;

        private void Start()
        {
            var levels = LevelCatalogParser.Parse(levelCatalog.text);
            var store = new PlayerPrefsProgressStore();
            var views = new GameViews(boardView, hudView, winPopup, losePopup, settingsPopup, noHeartsPopup, mechanicPopup, soundPlayer, ambientEvents);
            presenter = new GamePresenter(levels, GameRules.CreateStandard(), store.Load(), store, new SystemClock(), views, new SceneNavigator());
            presenter.Start(openLevelNumber);
        }

        private void Update() => presenter?.Tick(Time.unscaledDeltaTime);

        private void OnDestroy() => presenter?.Dispose();
    }
}
