using Meowtosis.Domain;
using Meowtosis.Presentation;
using UnityEngine;

namespace Meowtosis.Bootstrap
{
    // Điểm khởi đầu của scene Home: đọc danh sách level, nạp tiến độ và nối presenter với các view trong scene.
    public sealed class HomeBootstrap : MonoBehaviour
    {
        [SerializeField] private TextAsset levelCatalog;
        [SerializeField] private HomeView homeView;
        [SerializeField] private LevelMapView levelMapView;
        [SerializeField] private SoundPlayer soundPlayer;

        private HomePresenter presenter;

        private void Start()
        {
            var store = new PlayerPrefsProgressStore();
            var levels = LevelCatalogParser.Parse(levelCatalog.text);
            presenter = new HomePresenter(levels, store.Load(), store, new SystemClock(), homeView, levelMapView, soundPlayer, new SceneNavigator());
            presenter.Start();
        }

        private void Update() => presenter?.Tick(Time.unscaledDeltaTime);

        private void OnDestroy() => presenter?.Dispose();
    }
}
