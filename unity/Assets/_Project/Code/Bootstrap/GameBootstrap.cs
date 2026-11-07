using Meowtosis.Domain;
using Meowtosis.Gameplay;
using Meowtosis.Presentation;
using UnityEngine;

namespace Meowtosis.Bootstrap
{
    // Điểm khởi đầu của scene Game: nơi duy nhất tạo bộ luật, đọc danh sách level, nạp tiến độ
    // và nối presenter với các view trong scene. Người chơi chưa qua hướng dẫn đầu game thì chạy hướng dẫn trước.
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private TextAsset levelCatalog;
        [Tooltip("Màn hướng dẫn đầu game (Levels/Onboarding.txt), cùng định dạng với danh sách level.")]
        [SerializeField] private TextAsset onboardingScript;
        [SerializeField] private BoardView boardView;
        [SerializeField] private HudView hudView;
        [SerializeField] private OnboardingView onboardingView;
        [SerializeField] private WinPopup winPopup;
        [SerializeField] private LosePopup losePopup;
        [SerializeField] private SettingsPopup settingsPopup;
        [SerializeField] private NoHeartsPopup noHeartsPopup;
        [SerializeField] private MechanicPopup mechanicPopup;
        [SerializeField] private SoundPlayer soundPlayer;
        [SerializeField] private AmbientEvents ambientEvents;
        [SerializeField] private CheatPopup cheatPopup;
        [Tooltip("Để test: lớn hơn 0 thì luôn mở level này thay vì level chơi gần nhất.")]
        [SerializeField, Min(0)] private int openLevelNumber;
        [Tooltip("Để test: luôn chạy hướng dẫn đầu game, kể cả khi tiến độ đã ghi là xong.")]
        [SerializeField] private bool alwaysShowOnboarding;

        private GameRules rules;
        private IProgressStore store;
        private PlayerProgress progress;
        private OnboardingPresenter onboarding;
        private GamePresenter presenter;

        private void Start()
        {
            rules = GameRules.CreateStandard();
            store = new PlayerPrefsProgressStore();
            progress = store.Load();
            if (onboardingScript != null && (alwaysShowOnboarding || !progress.HasFinishedOnboarding)) StartOnboarding();
            else StartGame();
        }

        private void StartOnboarding()
        {
            var script = LevelCatalogParser.Parse(onboardingScript.text)[0];
            onboarding = new OnboardingPresenter(script, rules, progress, store, boardView, hudView, onboardingView,
                soundPlayer, StartGame);
            onboarding.Start();
        }

        private void StartGame()
        {
            onboarding?.Dispose();
            onboarding = null;
            var levels = LevelCatalogParser.Parse(levelCatalog.text);
            var views = new GameViews(boardView, hudView, winPopup, losePopup, settingsPopup, noHeartsPopup, mechanicPopup, soundPlayer, ambientEvents, cheatPopup);
            presenter = new GamePresenter(levels, rules, progress, store, new SystemClock(), views, new SceneNavigator());
            presenter.Start(openLevelNumber);
        }

        private void Update()
        {
            onboarding?.Tick(Time.unscaledDeltaTime);
            presenter?.Tick(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            onboarding?.Dispose();
            presenter?.Dispose();
        }
    }
}
