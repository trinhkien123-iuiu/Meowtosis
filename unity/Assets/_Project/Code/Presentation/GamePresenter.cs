using System;
using System.Collections.Generic;
using Meowtosis.Domain;
using Meowtosis.Gameplay;

namespace Meowtosis.Presentation
{
    // Điều phối màn chơi: nhận thao tác từ view, gọi LevelRun (luật + tim, xu, booster), rồi diễn nước đi và vẽ lại HUD, popup.
    // Chỉ biết các interface view nên đổi cách vẽ không phải sửa lớp này.
    public sealed class GamePresenter : IDisposable
    {
        private const float ResultPopupDelay = 0.9f;   // để người chơi kịp thấy mèo nhảy vui hoặc mặt buồn rồi mới hiện popup

        private readonly IReadOnlyList<LevelDefinition> levels;
        private readonly GameRules rules;
        private readonly PlayerProgress progress;
        private readonly IProgressStore store;
        private readonly IClock clock;
        private readonly GameViews views;
        private readonly ISceneNavigator navigator;
        private readonly BoosterTargeting targeting;
        private readonly Queue<Mechanic> mechanicCards = new Queue<Mechanic>();
        private LevelRun run;
        private int levelIndex;
        private bool isPlayingMove;
        private bool isHintShowing;
        private Action pendingAction;
        private float pendingDelay;
        private float secondsSinceClockUpdate;

        public GamePresenter(IReadOnlyList<LevelDefinition> levels, GameRules rules, PlayerProgress progress,
            IProgressStore store, IClock clock, GameViews views, ISceneNavigator navigator)
        {
            this.levels = levels;
            this.rules = rules;
            this.progress = progress;
            this.store = store;
            this.clock = clock;
            this.views = views;
            this.navigator = navigator;
            targeting = new BoosterTargeting(rules, views.Board, views.Hud);
            Subscribe(true);
        }

        private bool CanPlay => run.Session.Status == SessionStatus.Playing && progress.Hearts > 0
            && !isPlayingMove && pendingAction == null && !views.Mechanic.IsOpen;

        // Người chơi đang rảnh, không có bước hướng dẫn hay bảng nào mở: lúc này mới có sự kiện vui.
        private bool IsIdle => CanPlay && !targeting.IsActive && run.GuidedStep == null
            && !views.Settings.IsOpen && !views.NoHearts.IsOpen;

        public void Dispose() => Subscribe(false);

        public void Start(int levelNumberOverride)
        {
            if (progress.RecoverHearts(clock.UtcNow)) store.Save(progress);
            views.Sound.SetSoundOn(progress.SoundOn);
            views.Sound.SetMusicOn(progress.MusicOn);
            OpenLevel(levelNumberOverride > 0 ? levelNumberOverride - 1 : progress.LastLevelIndex);
        }

        // Gọi mỗi khung hình: chạy việc hẹn giờ, mỗi giây cập nhật tim và đồng hồ hồi tim.
        public void Tick(float deltaTime)
        {
            if (pendingAction != null && (pendingDelay -= deltaTime) <= 0f)
            {
                var action = pendingAction;
                pendingAction = null;
                action();
            }
            views.Ambient.Tick(deltaTime, IsIdle, run.Session.Board);
            if ((secondsSinceClockUpdate += deltaTime) < 1f) return;
            secondsSinceClockUpdate = 0f;
            if (progress.RecoverHearts(clock.UtcNow)) store.Save(progress);
            ShowHearts();
            if (views.NoHearts.IsOpen && progress.Hearts > 0) views.NoHearts.Hide();
        }

        private void OpenLevel(int index)
        {
            levelIndex = Math.Max(0, Math.Min(index, levels.Count - 1));
            progress.LastLevelIndex = levelIndex;
            store.Save(progress);
            run = new LevelRun(levels[levelIndex], progress, store, clock);
            pendingAction = null;
            isPlayingMove = false;
            isHintShowing = false;
            views.Win.Hide();
            views.Lose.Hide();
            views.Board.Build(run.Session.Level);
            targeting.Cancel();
            views.Board.Show(run.Session.Board);
            QueueNewMechanics();
            ShowNextMechanic();
            RefreshHud(true);
            if (progress.Hearts == 0 && !views.Mechanic.IsOpen) ShowNoHearts();
        }

        // Thẻ "Cơ chế mới": mỗi cơ chế chỉ hiện một lần, lúc mở level đầu tiên có nó, trước khi người chơi đi nước nào.
        private void QueueNewMechanics()
        {
            foreach (var mechanic in MechanicDetector.In(run.Session.Definition, levelIndex))
            {
                if (progress.MarkMechanicSeen(mechanic.ToString())) mechanicCards.Enqueue(mechanic);
            }
            store.Save(progress);
        }

        private void ShowNextMechanic()
        {
            if (mechanicCards.Count > 0)
            {
                views.Mechanic.Show(mechanicCards.Dequeue());
                views.Sound.Play(SoundId.PopupOpen);
                return;
            }
            views.Mechanic.Hide();
        }

        private void OnMechanicContinue()
        {
            ShowNextMechanic();
            RefreshHud(false);
            if (progress.Hearts == 0 && !views.Mechanic.IsOpen) ShowNoHearts();
        }

        private void Play(IGameCommand command)
        {
            if (!run.TryPlay(command, out var outcome)) return;
            targeting.Cancel();
            views.Board.HideTutorial();
            ClearHint();
            isPlayingMove = true;
            RefreshHud(false);
            views.Board.Play(outcome.Move, () => OnMoveFinished(outcome), outcome.SprungTrap);
        }

        private void OnMoveFinished(PlayOutcome outcome)
        {
            isPlayingMove = false;
            if (outcome.Stars > 0)
            {
                views.Board.Celebrate();
                Later(() =>
                {
                    views.Sound.Play(SoundId.PopupOpen);
                    views.Win.Show(outcome.Stars, outcome.CoinsEarned, progress.Coins);
                });
            }
            else if (outcome.SprungTrap)
            {
                views.Board.Mourn();
                Later(RestartAfterTrap);
            }
            else if (outcome.LostHeart)
            {
                views.Board.Mourn();
                Later(() => ShowLose(outcome.Move));
            }
            else if (run.Session.Definition.Tutorial.Count > 0 && run.Session.MovesMade == run.Session.Definition.Tutorial.Count)
                views.Hud.ShowToast(GameTexts.TutorialDone);
            RefreshHud(false);
        }

        // Người chơi vừa tự thấy luật qua nước bẫy: nhắc lại một câu, đưa bàn về đầu để chơi thật.
        private void RestartAfterTrap()
        {
            run.FinishTrap();
            views.Board.Show(run.Session.Board);
            views.Hud.ShowToast(GameTexts.TrapSeen);
            RefreshHud(true);
        }

        private void OnCellSwiped(int cell, Direction direction)
        {
            if (!CanPlay || targeting.IsActive || !run.Session.Board[cell].HasValue) return;
            var step = run.GuidedStep;
            if (step != null && (run.Session.Level.IndexOf(step.Move.Row, step.Move.Column) != cell || step.Move.Direction != direction))
            {
                views.Board.Refuse(cell);
                views.Hud.ShowToast(GameTexts.SwipeLikeTheHand);
                return;
            }

            var command = new SwipeCommand(rules.Swipes, cell, direction);
            if (command.TryApply(run.Session.Board, out _)) Play(command);
            else
            {
                views.Board.Refuse(cell);
                views.Hud.ShowToast(GameTexts.NothingChanges);
            }
        }

        private void OnCellTapped(int cell)
        {
            if (!CanPlay) return;
            if (!targeting.IsActive)
            {
                views.Board.Pet(cell);
                return;
            }
            var command = targeting.Tap(cell, run.Session.Board);
            if (command != null) Play(command);
            else RefreshHud(false);
        }

        private void OnColorChosen(Species color)
        {
            var command = targeting.ChooseColor(color);
            if (command != null && CanPlay) Play(command);
        }

        private void OnBoosterClicked(BoosterKind kind)
        {
            if (!CanPlay) return;
            if (run.GuidedStep != null)
            {
                views.Hud.ShowToast(GameTexts.FollowTheHand);
                return;
            }
            ClearHint();
            if (targeting.Kind == kind) targeting.Cancel();
            else if (progress.BoosterCount(kind) > 0) targeting.Begin(kind, run.Session.Board);
            else BuyBooster(kind);
            RefreshHud(false);
        }

        private void BuyBooster(BoosterKind kind)
        {
            if (progress.TryBuyBooster(kind))
            {
                store.Save(progress);
                views.Sound.Play(SoundId.Cashout);
                views.Hud.ShowToast(GameTexts.Bought(kind));
            }
            else
            {
                views.Sound.Play(SoundId.AngryMeow);
                views.Hud.ShowToast(GameTexts.NeedCoins(PlayerProgress.BoosterPrice));
            }
        }

        private void ClearHint()
        {
            isHintShowing = false;
            views.Board.HideHint();
        }

        private void OnHintClicked()
        {
            if (!CanPlay) return;
            if (run.GuidedStep != null)
            {
                views.Hud.ShowToast(GameTexts.FollowTheHand);
                return;
            }
            targeting.Cancel();
            // Mũi tên còn đang hiện thì đã trả lượt cho gợi ý này rồi, bấm lại không trừ thêm.
            if (isHintShowing) return;
            if (progress.BoosterCount(BoosterKind.Hint) == 0)
            {
                BuyBooster(BoosterKind.Hint);
                RefreshHud(false);
                return;
            }

            var hint = rules.Hints.Suggest(run.Session, HintAdvisor.DefaultTimeLimit);
            if (hint.Kind == HintKind.Move)
            {
                // Chỉ tính lượt khi thật sự có nước để chỉ.
                progress.TrySpendBooster(BoosterKind.Hint);
                store.Save(progress);
                isHintShowing = true;
                views.Board.ShowHint(run.Session.Level.IndexOf(hint.Move.Row, hint.Move.Column), hint.Move.Direction);
                views.Sound.Play(SoundId.BoxDone);
            }
            else views.Sound.Play(SoundId.AngryMeow);
            views.Hud.ShowToast(GameTexts.Hint(hint));
            RefreshHud(false);
        }

        private void OnReviveClicked()
        {
            if (!run.TryRevive()) return;
            views.Lose.Hide();
            ClearHint();
            views.Board.Show(run.Session.Board);
            views.Board.PlayRevive();
            views.Hud.ShowToast(GameTexts.Revived);
            RefreshHud(false);
        }

        private void OnRestartClicked()
        {
            views.Lose.Hide();
            views.Settings.Hide();
            targeting.Cancel();
            ClearHint();
            run.Restart();
            views.Board.Show(run.Session.Board);
            RefreshHud(true);
            if (progress.Hearts == 0) ShowNoHearts();
        }

        // Thắng level cuối thì không còn level kế, về bản đồ.
        private void OnContinueClicked()
        {
            if (levelIndex + 1 < levels.Count) OpenLevel(levelIndex + 1);
            else navigator.OpenMap();
        }

        private void OnWinBackClicked() => navigator.OpenMap();

        private void OnHomeClicked() => navigator.OpenHome();

        private void OnSettingsClicked()
        {
            views.Sound.Play(SoundId.PopupOpen);
            ShowSettings();
        }

        private void OnSoundToggled()
        {
            progress.SoundOn = !progress.SoundOn;
            store.Save(progress);
            views.Sound.SetSoundOn(progress.SoundOn);
            ShowSettings();
        }

        private void OnMusicToggled()
        {
            progress.MusicOn = !progress.MusicOn;
            store.Save(progress);
            views.Sound.SetMusicOn(progress.MusicOn);
            ShowSettings();
        }

        private void OnPreviousLevelClicked()
        {
            OpenLevel(levelIndex - 1);
            ShowSettings();
        }

        private void OnNextLevelClicked()
        {
            OpenLevel(levelIndex + 1);
            ShowSettings();
        }

        // Bản thử: nạp đầy tim và booster.
        private void OnRefillClicked()
        {
            progress.RefillHearts();
            progress.RefillBoosters();
            store.Save(progress);
            views.NoHearts.Hide();
            views.Sound.Play(SoundId.Revive);
            RefreshHud(false);
        }

        private void ShowSettings() => views.Settings.Show(progress.SoundOn, progress.MusicOn, levelIndex + 1);

        private void ShowLose(MoveResult lastMove) => views.Lose.Show(GameTexts.Lost(LossReport.Of(lastMove)),
            progress.BoosterCount(BoosterKind.Revive), run.CanRevive);

        private void ShowNoHearts()
        {
            views.Sound.Play(SoundId.PopupOpen);
            views.NoHearts.Show(GameTexts.HeartTimer(progress.TimeToNextHeart(clock.UtcNow)));
        }

        private void ShowHearts()
        {
            var timer = GameTexts.HeartTimer(progress.TimeToNextHeart(clock.UtcNow));
            views.Hud.ShowHearts(progress.Hearts, timer);
            if (views.NoHearts.IsOpen) views.NoHearts.Show(timer);
        }

        private void RefreshHud(bool instantly)
        {
            var hud = views.Hud;
            var session = run.Session;
            hud.ShowLevel(levelIndex + 1);
            ShowHearts();
            hud.ShowStars(run.Stars, GameTexts.Moves(session.MovesMade, session.Definition.Par), instantly);
            hud.ShowGoal(GameTexts.Goal(GoalProgress.Of(session.Board)));
            hud.ShowTip(CurrentTip());
            hud.ShowSpecies(SpeciesCensus.Of(session.Board));
            hud.ShowBooster(BoosterKind.Mutate, progress.BoosterCount(BoosterKind.Mutate), targeting.Kind == BoosterKind.Mutate);
            hud.ShowBooster(BoosterKind.Transplant, progress.BoosterCount(BoosterKind.Transplant), targeting.Kind == BoosterKind.Transplant);
            hud.ShowBooster(BoosterKind.Hint, progress.BoosterCount(BoosterKind.Hint), false);
            hud.SetHintEnabled(CanPlay);
            views.Board.IdleEnabled = CanPlay && !targeting.IsActive;
            RefreshTutorial();
        }

        // Bàn tay và lớp tối của bước hướng dẫn; tắt khi đang diễn nước đi hoặc đang mở thẻ cơ chế.
        private void RefreshTutorial()
        {
            var step = run.GuidedStep;
            if (step == null || !CanPlay)
            {
                views.Board.HideTutorial();
                return;
            }
            views.Board.ShowTutorial(run.Session.Level.IndexOf(step.Move.Row, step.Move.Column), step.Move.Direction);
        }

        // Đang chọn booster thì nhắc bước chọn; rồi lời hướng dẫn của bước hiện tại; rồi mẹo đầu màn (tắt sau nước đầu).
        private string CurrentTip()
        {
            if (targeting.IsActive) return targeting.Prompt;
            var session = run.Session;
            if (session.Status != SessionStatus.Playing) return "";
            var step = run.GuidedStep;
            if (step != null) return step.Text;
            return session.MovesMade == 0 ? session.Definition.Tip : "";
        }

        private void Later(Action action)
        {
            pendingAction = action;
            pendingDelay = ResultPopupDelay;
        }

        private void Subscribe(bool subscribe)
        {
            var board = views.Board;
            var hud = views.Hud;
            if (subscribe)
            {
                board.CellSwiped += OnCellSwiped;
                board.CellTapped += OnCellTapped;
                hud.SettingsClicked += OnSettingsClicked;
                hud.HintClicked += OnHintClicked;
                hud.BoosterClicked += OnBoosterClicked;
                hud.ColorChosen += OnColorChosen;
                views.Win.ContinueClicked += OnContinueClicked;
                views.Win.BackClicked += OnWinBackClicked;
                views.Lose.ReviveClicked += OnReviveClicked;
                views.Lose.RestartClicked += OnRestartClicked;
                views.Settings.HomeClicked += OnHomeClicked;
                views.Settings.SoundToggled += OnSoundToggled;
                views.Settings.MusicToggled += OnMusicToggled;
                views.Settings.RestartClicked += OnRestartClicked;
                views.Settings.PreviousLevelClicked += OnPreviousLevelClicked;
                views.Settings.NextLevelClicked += OnNextLevelClicked;
                views.Settings.RefillClicked += OnRefillClicked;
                views.NoHearts.RefillClicked += OnRefillClicked;
                views.Mechanic.ContinueClicked += OnMechanicContinue;
            }
            else
            {
                board.CellSwiped -= OnCellSwiped;
                board.CellTapped -= OnCellTapped;
                hud.SettingsClicked -= OnSettingsClicked;
                hud.HintClicked -= OnHintClicked;
                hud.BoosterClicked -= OnBoosterClicked;
                hud.ColorChosen -= OnColorChosen;
                views.Win.ContinueClicked -= OnContinueClicked;
                views.Win.BackClicked -= OnWinBackClicked;
                views.Lose.ReviveClicked -= OnReviveClicked;
                views.Lose.RestartClicked -= OnRestartClicked;
                views.Settings.HomeClicked -= OnHomeClicked;
                views.Settings.SoundToggled -= OnSoundToggled;
                views.Settings.MusicToggled -= OnMusicToggled;
                views.Settings.RestartClicked -= OnRestartClicked;
                views.Settings.PreviousLevelClicked -= OnPreviousLevelClicked;
                views.Settings.NextLevelClicked -= OnNextLevelClicked;
                views.Settings.RefillClicked -= OnRefillClicked;
                views.NoHearts.RefillClicked -= OnRefillClicked;
                views.Mechanic.ContinueClicked -= OnMechanicContinue;
            }
        }
    }
}
