using System;
using System.Collections.Generic;
using Meowtosis.Domain;
using Meowtosis.Gameplay;

namespace Meowtosis.Presentation
{
    // Điều khiển màn Home: hiện xu, tim, level hiện tại, đổi trang, bật tắt âm thanh, mở bản đồ level rồi vào màn chơi.
    public sealed class HomePresenter : IDisposable
    {
        private readonly IReadOnlyList<LevelDefinition> levels;
        private readonly PlayerProgress progress;
        private readonly IProgressStore store;
        private readonly IClock clock;
        private readonly IHomeView view;
        private readonly ILevelMapView map;
        private readonly ISoundPlayer sound;
        private readonly ISceneNavigator navigator;
        private float secondsSinceClockUpdate;

        public HomePresenter(IReadOnlyList<LevelDefinition> levels, PlayerProgress progress, IProgressStore store, IClock clock,
            IHomeView view, ILevelMapView map, ISoundPlayer sound, ISceneNavigator navigator)
        {
            this.levels = levels;
            this.progress = progress;
            this.store = store;
            this.clock = clock;
            this.view = view;
            this.map = map;
            this.sound = sound;
            this.navigator = navigator;
            view.PlayClicked += OnPlayClicked;
            view.TabClicked += OnTabClicked;
            view.SoundToggled += OnSoundToggled;
            view.MusicToggled += OnMusicToggled;
            map.LevelClicked += OnLevelClicked;
            map.BackClicked += OnMapBack;
            map.ResetClicked += OnMapReset;
            map.UnlockAllClicked += OnMapUnlockAll;
        }

        public void Dispose()
        {
            view.PlayClicked -= OnPlayClicked;
            view.TabClicked -= OnTabClicked;
            view.SoundToggled -= OnSoundToggled;
            view.MusicToggled -= OnMusicToggled;
            map.LevelClicked -= OnLevelClicked;
            map.BackClicked -= OnMapBack;
            map.ResetClicked -= OnMapReset;
            map.UnlockAllClicked -= OnMapUnlockAll;
        }

        public void Start()
        {
            if (progress.RecoverHearts(clock.UtcNow)) store.Save(progress);
            sound.SetSoundOn(progress.SoundOn);
            sound.SetMusicOn(progress.MusicOn);
            view.ShowCoins(progress.Coins);
            view.ShowLevel(LevelMap.CurrentIndex(levels, progress) + 1);
            view.ShowAudio(progress.SoundOn, progress.MusicOn);
            view.ShowTab(HomeTab.Home, false);
            ShowHearts();
            if (navigator.TakeMapRequest()) OpenMap();
        }

        // Gọi mỗi khung hình: mỗi giây cập nhật tim và đồng hồ hồi tim.
        public void Tick(float deltaTime)
        {
            if ((secondsSinceClockUpdate += deltaTime) < 1f) return;
            secondsSinceClockUpdate = 0f;
            if (progress.RecoverHearts(clock.UtcNow)) store.Save(progress);
            ShowHearts();
        }

        private void ShowHearts()
        {
            var timer = GameTexts.HeartTimer(progress.TimeToNextHeart(clock.UtcNow));
            view.ShowHearts(progress.Hearts, timer.Length > 0 ? timer : GameTexts.HeartsFull);
        }

        private void OnPlayClicked()
        {
            sound.Play(SoundId.PopupOpen);
            OpenMap();
        }

        private void OpenMap()
        {
            view.SetSwipeEnabled(false);
            map.Show(LevelMap.Build(levels, progress));
        }

        private void OnMapBack()
        {
            map.Hide();
            view.SetSwipeEnabled(true);
        }

        // Bản thử: về tiến độ của người chơi mới, chỉ giữ cài đặt âm thanh, rồi nạp lại màn Home và mở sẵn bản đồ.
        private void OnMapReset()
        {
            var fresh = PlayerProgress.CreateNew();
            fresh.SoundOn = progress.SoundOn;
            fresh.MusicOn = progress.MusicOn;
            store.Save(fresh);
            navigator.OpenMap();
        }

        // Bản thử: mở khoá toàn bộ các level trên bản đồ.
        private void OnMapUnlockAll()
        {
            foreach (var level in levels)
            {
                if (progress.BestStarsFor(level.Id) == 0)
                {
                    progress.RecordWin(level.Id, 1);
                }
            }
            store.Save(progress);
            sound.Play(SoundId.Win);
            map.Show(LevelMap.Build(levels, progress));
        }

        private void OnLevelClicked(int index)
        {
            if (index > LevelMap.CurrentIndex(levels, progress))
            {
                sound.Play(SoundId.LevelLocked);
                map.Refuse(index);
                return;
            }
            progress.LastLevelIndex = index;
            store.Save(progress);
            sound.Play(SoundId.LevelSelect);
            map.PlaySelected(index, navigator.OpenGame);
        }

        private void OnTabClicked(HomeTab tab)
        {
            view.ShowTab(tab, true);
        }

        private void OnSoundToggled()
        {
            progress.SoundOn = !progress.SoundOn;
            store.Save(progress);
            sound.SetSoundOn(progress.SoundOn);
            view.ShowAudio(progress.SoundOn, progress.MusicOn);
        }

        private void OnMusicToggled()
        {
            progress.MusicOn = !progress.MusicOn;
            store.Save(progress);
            sound.SetMusicOn(progress.MusicOn);
            view.ShowAudio(progress.SoundOn, progress.MusicOn);
        }
    }
}
