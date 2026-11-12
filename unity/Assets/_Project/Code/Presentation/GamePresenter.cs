using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Meowtosis.Domain;
using Meowtosis.Gameplay;
using UnityEngine;

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
        private BoosterKind offeredBooster;
        private readonly Queue<Mechanic> mechanicCards = new Queue<Mechanic>();
        private LevelRun run;
        private int levelIndex;
        private bool isPlayingMove;
        private bool isSpotlighting;
        private bool isHintShowing;
        private Task<Hint> hintSearch;
        private Board hintSearchBoard;
        private bool hintSearchIsIdle;
        private Action pendingAction;
        private float pendingDelay;
        private bool pendingBlocksInput;
        private float secondsSinceClockUpdate;
        private string trapExplanation;
        private string praiseTip;   // lời khen tạm thời khi vừa xong hướng dẫn trong level
        private int cheatLevelIndex;
        private const float IdleHelpThresholdSeconds = 4f;
        private float idleHelpSeconds;
        private bool isIdleHandShowing;

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
            && !isPlayingMove && !isSpotlighting && !(pendingAction != null && pendingBlocksInput)
            && !views.Mechanic.IsOpen && !views.Cheat.IsOpen
            && !views.BoosterOffer.IsOpen && !views.Settings.IsOpen && !views.NoHearts.IsOpen;

        // Người chơi đang rảnh, không có bước hướng dẫn hay bảng nào mở: lúc này mới có sự kiện vui.
        private bool IsIdle => CanPlay && !targeting.IsActive && run.GuidedStep == null
            && !views.Settings.IsOpen && !views.NoHearts.IsOpen;

        public void Dispose()
        {
            CancelHintSearch();
            Subscribe(false);
        }

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
            CompleteHintSearch();
            if (pendingAction != null && (pendingDelay -= deltaTime) <= 0f)
            {
                var action = pendingAction;
                pendingAction = null;
                action();
            }
            views.Ambient.Tick(deltaTime, IsIdle, run.Session.Board);

            // Người chơi click/chạm vào màn hình thì thu lại ngay bong bóng khen/cảnh báo (để chơi luôn, không phải đợi lâu)
            if ((!string.IsNullOrEmpty(praiseTip) || !string.IsNullOrEmpty(trapExplanation))
                && (Input.GetMouseButtonDown(0) || Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                praiseTip = null;
                trapExplanation = null;
                CancelFeedbackDelay();
                RefreshHud(true);
            }

            // Các màn onboarding (13 màn đầu, levelIndex < 13) và Level 14 (levelIndex == 13):
            // nếu người chơi đứng yên lâu không thao tác thì xuất hiện bàn tay hướng dẫn
            var isHandTutorialEligible = levelIndex < 14;
            if (CanPlay && !targeting.IsActive && run.GuidedStep == null && isHandTutorialEligible)
            {
                if (!isIdleHandShowing)
                {
                    idleHelpSeconds += deltaTime;
                    if (idleHelpSeconds >= IdleHelpThresholdSeconds)
                    {
                        ShowIdleHelp();
                    }
                }
            }
            else
            {
                if (isIdleHandShowing)
                {
                    views.Board.HideTutorial();
                    isIdleHandShowing = false;
                }
                idleHelpSeconds = 0f;
            }

            if ((secondsSinceClockUpdate += deltaTime) < 1f) return;
            secondsSinceClockUpdate = 0f;
            if (progress.RecoverHearts(clock.UtcNow)) store.Save(progress);
            ShowHearts();
            if (views.NoHearts.IsOpen && progress.Hearts > 0) views.NoHearts.Hide();
        }

        private void ShowIdleHelp()
        {
            // A failed search must wait before retrying, rather than blocking every frame.
            idleHelpSeconds = 0f;
            if (hintSearch == null) BeginHintSearch(true, TimeSpan.FromMilliseconds(50));
        }

        private void OpenLevel(int index)
        {
            CancelHintSearch();
            levelIndex = Math.Max(0, Math.Min(index, levels.Count - 1));
            progress.LastLevelIndex = levelIndex;
            store.Save(progress);
            run = new LevelRun(levels[levelIndex], progress, store, clock);
            if (levels[levelIndex].Trap != null && progress.BestStarsFor(levels[levelIndex].Id) == 0)
                run.EnableTrap();
            trapExplanation = null;
            praiseTip = null;
            isSpotlighting = false;
            pendingAction = null;
            isPlayingMove = false;
            isHintShowing = false;
            idleHelpSeconds = 0f;
            isIdleHandShowing = false;
            views.Win.Hide();
            views.Lose.Hide();
            views.Board.ClearSpotlight();
            views.Board.Build(run.Session.Level);
            targeting.Cancel();
            views.Board.Show(run.Session.Board);
            QueueNewMechanics();
            views.Mechanic.Hide();
            RefreshHud(true);
            if (progress.Hearts == 0) ShowNoHearts();
        }

        // Đánh dấu cơ chế đã gặp trong progress để lưu dữ liệu, không hiện popup làm gián đoạn người chơi.
        // Bốn luật cơ bản được dạy ngay trên bàn ở level 1, 6, 10, 13: bàn tay chỉ nước và bóng chat ngắn (tutorial của level).
        private void QueueNewMechanics()
        {
            foreach (var mechanic in MechanicDetector.In(run.Session.Definition))
            {
                progress.MarkMechanicSeen(mechanic.ToString());
            }
            store.Save(progress);
        }

        private void ShowNextMechanic()
        {
            views.Mechanic.Hide();
        }

        private void OnMechanicContinue()
        {
            views.Mechanic.Hide();
            RefreshHud(false);
            if (progress.Hearts == 0) ShowNoHearts();
        }

        private bool Play(IGameCommand command)
        {
            if (!run.TryPlay(command, out var outcome)) return false;
            CancelFeedbackDelay();
            targeting.Cancel();
            if (isIdleHandShowing)
            {
                views.Board.HideTutorial();
                isIdleHandShowing = false;
            }
            idleHelpSeconds = 0f;
            views.Board.HideTutorial();
            ClearHint();
            isPlayingMove = true;
            RefreshHud(false);
            views.Board.Play(outcome.Move, () => OnMoveFinished(outcome), outcome.SprungTrap);
            return true;
        }

        private void OnMoveFinished(PlayOutcome outcome)
        {
            isPlayingMove = false;
            if (outcome.Stars > 0)
            {
                views.Board.Celebrate(() =>
                {
                    views.Sound.Play(SoundId.PopupOpen);
                    views.Win.Show(outcome.Stars, outcome.CoinsEarned, progress.Coins);
                });
            }
            else if (outcome.SprungTrap)
            {
                views.Board.Mourn();
                Later(() =>
                {
                    views.Sound.Play(SoundId.PopupOpen);
                    views.Lose.Show(GameTexts.Lost(LossReport.Of(outcome.Move)), 0, false, showButtons: false);
                    Later(RestartAfterTrap, 1.8f, blocksInput: true);
                });
            }
            else if (outcome.LostHeart)
            {
                views.Board.Mourn();
                Later(() => ShowLose(outcome.Move));
            }
            else if (run.Session.Definition.Tutorial.Count > 0 && run.Session.MovesMade == run.Session.Definition.Tutorial.Count)
            {
                if (run.Session.Definition.Trap != null)
                {
                    int survivorCell = -1;
                    for (int i = 0; i < run.Session.Board.CellCount; i++)
                    {
                        if (run.Session.Board[i] is Piece piece && piece.IsCat && piece.Species == Species.Red)
                        {
                            survivorCell = i;
                            break;
                        }
                    }

                    isSpotlighting = true;
                    views.Board.Spotlight(survivorCell >= 0 ? survivorCell : run.Session.Level.IndexOf(1, 1));
                    trapExplanation = GameTexts.TrapKeepOne;
                    RefreshHud(true);
                    Later(() =>
                    {
                        views.Board.ClearSpotlight();
                        trapExplanation = GameTexts.TrapFillSafely;
                        isSpotlighting = false;
                        RefreshHud(true);
                        Later(() =>
                        {
                            trapExplanation = null;
                            RefreshHud(true);
                        }, 4f);
                    }, 4.5f);   // đủ lâu cho bóng chat hiện hết hình, chữ và người chơi kịp đọc
                }
                else
                {
                    if (levelIndex == 10 || levelIndex == 13 || levelIndex == 19)
                    {
                        // Level 11, 14, 20 chỉ hiện boxchat đầu tiên, không hiện lời khen hay câu nào khác tiếp theo
                        praiseTip = null;
                        RefreshHud(true);
                    }
                    else
                    {
                        // Mèo dẫn chuyện reo lên khen, rồi bong bóng tắt để người chơi tự chơi tiếp.
                        praiseTip = GameTexts.TutorialDone;
                        RefreshHud(true);
                        Later(() =>
                        {
                            praiseTip = null;
                            RefreshHud(true);
                        }, 3f);
                    }
                }
            }
            RefreshHud(false);
        }

        // Người chơi vừa tự thấy luật qua nước bẫy: đóng bảng thua, đưa bàn về đầu và hướng dẫn chơi thật.
        private void RestartAfterTrap()
        {
            views.Lose.Hide();
            run.FinishTrap();
            views.Board.Show(run.Session.Board);
            trapExplanation = GameTexts.TrapLost;
            RefreshHud(true);
        }

        private void OnCellSwiped(int cell, Direction direction)
        {
            if (isSpotlighting) return;
            if (!string.IsNullOrEmpty(praiseTip))
            {
                praiseTip = null;
                RefreshHud(true);
            }
            if (isIdleHandShowing)
            {
                views.Board.HideTutorial();
                isIdleHandShowing = false;
            }
            idleHelpSeconds = 0f;
            if (!CanPlay || targeting.IsActive || !run.Session.Board[cell].HasValue) return;

            // Khóa các nước đi khác trong giai đoạn hướng dẫn/onboarding, chỉ cho đi đúng nước được chỉ dẫn
            if (run.GuidedStep != null)
            {
                var step = run.GuidedStep;
                var expectedCell = run.Session.Level.IndexOf(step.Move.Row, step.Move.Column);
                if (cell != expectedCell || direction != step.Move.Direction)
                {
                    views.Board.Refuse(cell);
                    return;
                }
            }

            // Level 5 (meow-158): nếu người chơi cố tình vuốt mèo đỏ 1 hay 2 sang phải (sẽ triệt tiêu hết màu đỏ) thì chặn lại,
            // cảnh báo "Làm vậy sẽ triệt tiêu hết mèo đỏ" và lúc này mới hiện bàn tay hướng dẫn vuốt xuống.
            if (run.Session.Definition.Id == "meow-158" && direction == Direction.Right
                && (cell == run.Session.Level.IndexOf(0, 0) || cell == run.Session.Level.IndexOf(0, 1)))
            {
                views.Board.Refuse(cell);
                trapExplanation = GameTexts.TrapWarnTrietTieu;
                RefreshHud(true);
                views.Sound.Play(SoundId.AngryMeow);
                isIdleHandShowing = true;
                views.Board.ShowTutorial(run.Session.Level.IndexOf(0, 1), Direction.Down);
                Later(() =>
                {
                    if (trapExplanation == GameTexts.TrapWarnTrietTieu)
                    {
                        trapExplanation = null;
                        RefreshHud(true);
                    }
                }, 3.5f);
                return;
            }

            // Level 12 (meow-165): nếu người chơi vuốt mèo xanh (0, 2) xuống đè lên mèo vàng duy nhất ở (1, 2) sẽ thua,
            // thì chặn lại, cảnh báo "Làm vậy sẽ triệt tiêu hết mèo vàng" và hướng dẫn vuốt mèo vàng sang trái.
            // Khi người chơi đã vuốt mèo vàng sang (đã nhân đôi, trên bàn có từ 2 mèo vàng trở lên) thì cho phép đi bình thường.
            if (run.Session.Definition.Id == "meow-165" && direction == Direction.Down
                && cell == run.Session.Level.IndexOf(0, 2)
                && run.Session.Board[run.Session.Level.IndexOf(1, 2)] is Piece targetPiece
                && targetPiece.IsCat && targetPiece.Species == Species.Yellow)
            {
                var yellowCount = 0;
                for (var i = 0; i < run.Session.Board.CellCount; i++)
                {
                    if (run.Session.Board[i] is Piece p && p.IsCat && p.Species == Species.Yellow)
                        yellowCount++;
                }

                if (yellowCount <= 1)
                {
                    views.Board.Refuse(cell);
                    trapExplanation = GameTexts.TrapWarnTrietTieuVang;
                    RefreshHud(true);
                    views.Sound.Play(SoundId.AngryMeow);
                    isIdleHandShowing = true;
                    views.Board.ShowTutorial(run.Session.Level.IndexOf(1, 2), Direction.Left);
                    Later(() =>
                    {
                        if (trapExplanation == GameTexts.TrapWarnTrietTieuVang)
                        {
                            trapExplanation = null;
                            RefreshHud(true);
                        }
                    }, 3.5f);
                    return;
                }
            }

            // Level 12 (meow-165): nếu người chơi vuốt mèo vàng lên (0, 2) rồi định vuốt sang phải đè lên mèo xanh duy nhất ở (0, 3) sẽ thua,
            // thì chặn lại và cảnh báo "Làm vậy sẽ triệt tiêu hết mèo xanh".
            if (run.Session.Definition.Id == "meow-165" && direction == Direction.Right
                && cell == run.Session.Level.IndexOf(0, 2)
                && run.Session.Board[run.Session.Level.IndexOf(0, 3)] is Piece targetPieceBlue
                && targetPieceBlue.IsCat && targetPieceBlue.Species == Species.Blue)
            {
                var blueCount = 0;
                for (var i = 0; i < run.Session.Board.CellCount; i++)
                {
                    if (run.Session.Board[i] is Piece p && p.IsCat && p.Species == Species.Blue)
                        blueCount++;
                }

                if (blueCount <= 1)
                {
                    views.Board.Refuse(cell);
                    trapExplanation = GameTexts.TrapWarnTrietTieuXanh;
                    RefreshHud(true);
                    views.Sound.Play(SoundId.AngryMeow);
                    if (run.Session.Board[run.Session.Level.IndexOf(1, 1)] == null)
                    {
                        isIdleHandShowing = true;
                        views.Board.ShowTutorial(run.Session.Level.IndexOf(1, 2), Direction.Left);
                    }
                    Later(() =>
                    {
                        if (trapExplanation == GameTexts.TrapWarnTrietTieuXanh)
                        {
                            trapExplanation = null;
                            RefreshHud(true);
                        }
                    }, 3.5f);
                    return;
                }
            }

            if (!string.IsNullOrEmpty(trapExplanation) && trapExplanation != GameTexts.TrapLost)
            {
                trapExplanation = null;
                pendingAction = null;
                RefreshHud(true);
            }

            var command = new SwipeCommand(rules.Swipes, cell, direction);
            if (!command.TryApply(run.Session.Board, out var preview))
            {
                views.Board.Refuse(cell);
                views.Hud.ShowToast(GameTexts.NothingChanges);
                return;
            }

            // Ở các màn onboarding/hướng dẫn (Block 1 hoặc các level có tutorial):
            // Chặn tất cả các nước đi làm mất hết 1 màu mèo (do triệt 3 hoặc bị đè mất con cuối cùng).
            var isOnboarding = run.Session.Definition.Block == 1 || run.Session.Definition.Tutorial.Count > 0;
            if (isOnboarding && GameOutcome.IsLost(preview.After))
            {
                views.Board.Refuse(cell);
                views.Sound.Play(SoundId.AngryMeow);
                var extinct = GameOutcome.ExtinctColors(preview.After);
                var extinctColor = extinct.Count > 0 ? extinct[0] : Species.Red;
                var colorName = GameTexts.ColorName(extinctColor);
                trapExplanation = $"Làm vậy sẽ làm mất hết mèo {colorName}!";
                RefreshHud(true);
                Later(() =>
                {
                    if (trapExplanation == $"Làm vậy sẽ làm mất hết mèo {colorName}!")
                    {
                        trapExplanation = null;
                        RefreshHud(true);
                    }
                }, 3.5f);
                return;
            }

            var previousExplanation = trapExplanation;
            if (trapExplanation == GameTexts.TrapLost) trapExplanation = null;
            if (!Play(command))
            {
                trapExplanation = previousExplanation;
                views.Board.Refuse(cell);
                views.Hud.ShowToast(GameTexts.NothingChanges);
            }
        }

        private void OnCellTapped(int cell)
        {
            if (!string.IsNullOrEmpty(praiseTip))
            {
                praiseTip = null;
                RefreshHud(true);
            }
            if (isIdleHandShowing)
            {
                views.Board.HideTutorial();
                isIdleHandShowing = false;
            }
            idleHelpSeconds = 0f;
            if (!CanPlay || run.GuidedStep != null) return;
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
            if (!CanPlay || run.GuidedStep != null) return;
            ClearHint();
            if (targeting.Kind == kind) targeting.Cancel();
            else if (progress.BoosterCount(kind) > 0) targeting.Begin(kind, run.Session.Board);
            else OfferBooster(kind);
            RefreshHud(false);
        }

        private void OnStarLost(int lost) => views.Sound.Play(SoundId.StarLost);

        private void OnStarGained(int gained) => views.Sound.Play(SoundId.Coin);

        private void OnBoosterCancelClicked()
        {
            views.Sound.Play(SoundId.ButtonTap);
            targeting.Cancel();
            RefreshHud(false);
        }

        // Hết lượt: mở popup giới thiệu booster kèm nút mua gói bằng xu.
        private void OfferBooster(BoosterKind kind)
        {
            targeting.Cancel();
            offeredBooster = kind;
            views.Sound.Play(SoundId.PopupOpen);
            views.BoosterOffer.Show(kind, PlayerProgress.BoosterPrice, PlayerProgress.BoosterPackSize, progress.Coins);
        }

        private void OnBoosterBuyClicked()
        {
            if (progress.TryBuyBooster(offeredBooster))
            {
                store.Save(progress);
                views.Sound.Play(SoundId.Cashout);
                views.BoosterOffer.Hide();
                views.Hud.ShowToast(GameTexts.BoughtPack(offeredBooster, PlayerProgress.BoosterPackSize));
                RefreshHud(false);
            }
            else
            {
                views.Sound.Play(SoundId.AngryMeow);
                views.BoosterOffer.ShowNotEnoughCoins();
            }
        }

        private void ClearHint()
        {
            isHintShowing = false;
            views.Board.HideHint();
        }

        private void OnHintClicked()
        {
            if (!CanPlay || run.GuidedStep != null || hintSearch != null && !hintSearchIsIdle) return;
            targeting.Cancel();
            // Mũi tên còn đang hiện thì đã trả lượt cho gợi ý này rồi, bấm lại không trừ thêm.
            if (isHintShowing) return;
            if (progress.BoosterCount(BoosterKind.Hint) == 0)
            {
                OfferBooster(BoosterKind.Hint);
                RefreshHud(false);
                return;
            }

            CancelHintSearch();
            BeginHintSearch(false, HintAdvisor.DefaultTimeLimit);
        }

        private void BeginHintSearch(bool idle, TimeSpan timeLimit)
        {
            // Only immutable game data goes to the worker. Views and progress stay on the main thread.
            var board = run.Session.Board;
            var definition = run.Session.Definition;
            var history = run.Session.PreviousBoards.ToArray();
            var advisor = new HintAdvisor(rules.Swipes, rules.Solver);
            hintSearchBoard = board;
            hintSearchIsIdle = idle;
            if (!idle) views.Hud.ShowToast("Đang tìm nước đi...");
#if UNITY_WEBGL && !UNITY_EDITOR
            hintSearch = Task.FromResult(advisor.Suggest(board, definition, history, timeLimit));
#else
            hintSearch = Task.Run(() => advisor.Suggest(board, definition, history, timeLimit));
#endif
        }

        private void CompleteHintSearch()
        {
            if (hintSearch == null || !hintSearch.IsCompleted) return;
            var search = hintSearch;
            var searchedBoard = hintSearchBoard;
            var idle = hintSearchIsIdle;
            hintSearch = null;
            hintSearchBoard = null;
            if (search.IsFaulted)
            {
                Debug.LogException(search.Exception);
                return;
            }
            if (search.IsCanceled || !ReferenceEquals(searchedBoard, run.Session.Board) || !CanPlay) return;
            var hint = search.Result;
            if (idle)
            {
                if (hint.Kind == HintKind.Move && IsIdle && !isHintShowing && !isIdleHandShowing)
                {
                    isIdleHandShowing = true;
                    views.Board.ShowTutorial(run.Session.Level.IndexOf(hint.Move.Row, hint.Move.Column), hint.Move.Direction);
                }
                return;
            }
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

        private void CancelHintSearch()
        {
            var abandoned = hintSearch;
            hintSearch = null;
            hintSearchBoard = null;
            // Observe failures even if this scene is destroyed before the bounded search finishes.
            abandoned?.ContinueWith(completed => { _ = completed.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
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
            // Do not let an old playback callback modify a freshly restarted run.
            if (isPlayingMove || run.Session.Status == SessionStatus.Won) return;
            CancelHintSearch();
            pendingAction = null;
            pendingDelay = 0f;
            idleHelpSeconds = 0f;
            isIdleHandShowing = false;
            trapExplanation = null;
            praiseTip = null;
            isSpotlighting = false;
            views.Board.ClearSpotlight();
            views.Lose.Hide();
            views.Settings.Hide();
            targeting.Cancel();
            ClearHint();
            run.Restart();
            if (levels[levelIndex].Trap != null && progress.BestStarsFor(levels[levelIndex].Id) == 0)
                run.EnableTrap();
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

        // Bản thử (bảng cheat): nạp đầy tim và booster.
        private void OnRefillClicked()
        {
            progress.RefillHearts();
            progress.RefillBoosters();
            store.Save(progress);
            views.NoHearts.Hide();
            views.Sound.Play(SoundId.Revive);
            RefreshHud(false);
        }

        // ---- Bảng cheat của bản thử (chạm nhanh 3 lần vào ô Lv) ----

        private const int CheatCoinAmount = 500;

        private void OnCheatOpened()
        {
            cheatLevelIndex = levelIndex;
            views.Sound.Play(SoundId.PopupOpen);
            ShowCheat();
        }

        private void ShowCheat() => views.Cheat.Show(cheatLevelIndex + 1, levels.Count);

        // Thắng ngay với 3 sao: ghi tiến độ như thắng thật, diễn hoạt cảnh thắng rồi hiện bảng thắng.
        private void OnCheatWin()
        {
            views.Cheat.Hide();
            if (run.Session.Status != SessionStatus.Playing || isPlayingMove) return;
            targeting.Cancel();
            ClearHint();
            views.Board.HideTutorial();
            views.Board.ClearFocus();
            var coins = progress.RecordWin(run.Session.Definition.Id, 3);
            store.Save(progress);
            isPlayingMove = true;
            views.Board.Celebrate(() =>
            {
                isPlayingMove = false;
                views.Sound.Play(SoundId.PopupOpen);
                views.Win.Show(3, coins, progress.Coins);
            });
        }

        // Đi giúp một nước đúng (lấy từ Gợi ý, không trừ lượt). Đang ở bước hướng dẫn thì đi đúng nước hướng dẫn.
        private void OnCheatSolveStep()
        {
            views.Cheat.Hide();
            if (!CanPlay) return;
            var step = run.GuidedStep;
            Move move;
            if (step != null) move = step.Move;
            else
            {
                var hint = rules.Hints.Suggest(run.Session, HintAdvisor.DefaultTimeLimit);
                if (hint.Kind != HintKind.Move)
                {
                    views.Hud.ShowToast(GameTexts.CheatNoMove);
                    return;
                }
                move = hint.Move;
            }
            Play(new SwipeCommand(rules.Swipes, run.Session.Level.IndexOf(move.Row, move.Column), move.Direction));
        }

        private void OnCheatLevelStepped(int delta)
        {
            cheatLevelIndex = Math.Max(0, Math.Min(levels.Count - 1, cheatLevelIndex + delta));
            ShowCheat();
        }

        private void OnCheatOpenLevel()
        {
            views.Cheat.Hide();
            OpenLevel(cheatLevelIndex);
        }

        private void OnCheatCoins()
        {
            progress.AddCoins(CheatCoinAmount);
            store.Save(progress);
            views.Sound.Play(SoundId.Coin);
            views.Hud.ShowToast(GameTexts.CheatCoins(progress.Coins));
        }

        private void OnCheatUnlockAll()
        {
            foreach (var level in levels) progress.MarkPassed(level.Id);
            store.Save(progress);
            views.Hud.ShowToast(GameTexts.CheatUnlocked);
        }

        // Chơi lại màn hướng dẫn đầu game: xoá dấu đã xong rồi mở lại màn chơi (GameBootstrap chạy hướng dẫn trước).
        // Bản thử: mở level 1 để xem lại chuỗi dạy luật từ đầu.
        private void OnCheatOnboarding()
        {
            views.Cheat.Hide();
            OpenLevel(0);
        }

        private void ShowSettings() => views.Settings.Show(progress.SoundOn, progress.MusicOn);

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
            var isGuided = session.Definition.Tutorial.Count > 0 || !string.IsNullOrEmpty(trapExplanation);
            hud.SetGuidedLayout(isGuided);
            ShowHearts();
            hud.ShowStars(run.Stars, instantly);
            hud.ShowGoal(GoalProgress.Of(session.Board), instantly);
            if (!isPlayingMove) hud.ShowTip(CurrentTip(), CurrentMood());
            hud.ShowBoosterPrompt(targeting.Kind, targeting.Prompt);
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
            if (isSpotlighting)
            {
                views.Board.HideTutorial();
                return;
            }
            var step = run.GuidedStep;
            if (step == null || !CanPlay)
            {
                if (!isIdleHandShowing) views.Board.HideTutorial();
                // Giữ vùng sáng trong lúc nước hướng dẫn đang diễn, để thấy rõ kết quả của cơ chế
                if (!isPlayingMove) views.Board.ClearFocus();
                return;
            }
            // Làm tối phần không liên quan, chừa sáng các ô nước hướng dẫn chạm tới để thấy rõ cơ chế làm gì
            if (rules.Swipes.TrySwipe(run.Session.Board, step.Move, out var preview))
                views.Board.Focus(MoveFocus.CellsOf(preview, run.Session.Level));
            views.Board.ShowTutorial(run.Session.Level.IndexOf(step.Move.Row, step.Move.Column), step.Move.Direction);
        }

        // Bỏ bong bóng chữ giải thích cơ chế dài dòng; chỉ nhắc bước khi đang chọn mục tiêu cho booster.
        private string CurrentTip()
        {
            if (!string.IsNullOrEmpty(praiseTip)) return praiseTip;
            if (!string.IsNullOrEmpty(trapExplanation)) return trapExplanation;
            // Lời nhắn khi dùng booster nằm ở thanh booster dưới bàn, không bật bong bóng đè lên bàn nữa.
            if (targeting.IsActive) return "";
            var step = run.GuidedStep;
            if (step != null && !string.IsNullOrEmpty(step.Text)) return step.Text;
            return "";
        }

        // Tâm trạng mèo dẫn chuyện theo câu đang nói: khen khi vừa làm xong hướng dẫn, lo lắng ngay sau khi thua bẫy hoặc cảnh báo triệt tiêu.
        private NarratorMood CurrentMood()
        {
            if (!string.IsNullOrEmpty(praiseTip)) return NarratorMood.Praise;
            if (trapExplanation == GameTexts.TrapLost || trapExplanation?.StartsWith("Làm vậy sẽ triệt tiêu hết mèo") == true) return NarratorMood.Warn;
            return NarratorMood.Explain;
        }

        private void CancelFeedbackDelay()
        {
            if (pendingBlocksInput) return;
            pendingAction = null;
            pendingDelay = 0f;
        }

        private void Later(Action action, float delay, bool blocksInput = false)
        {
            pendingAction = action;
            pendingDelay = delay;
            pendingBlocksInput = blocksInput || isSpotlighting;
        }

        private void Later(Action action) => Later(action, ResultPopupDelay, blocksInput: true);

        private void Subscribe(bool subscribe)
        {
            var board = views.Board;
            var hud = views.Hud;
            if (subscribe)
            {
                board.CellSwiped += OnCellSwiped;
                board.CellTapped += OnCellTapped;
                hud.SettingsClicked += OnSettingsClicked;
                hud.RestartClicked += OnRestartClicked;
                hud.LevelBadgeTripleTapped += OnCheatOpened;
                views.Cheat.WinClicked += OnCheatWin;
                views.Cheat.SolveStepClicked += OnCheatSolveStep;
                views.Cheat.LevelStepped += OnCheatLevelStepped;
                views.Cheat.OpenLevelClicked += OnCheatOpenLevel;
                views.Cheat.RefillClicked += OnRefillClicked;
                views.Cheat.CoinsClicked += OnCheatCoins;
                views.Cheat.UnlockAllClicked += OnCheatUnlockAll;
                views.Cheat.OnboardingClicked += OnCheatOnboarding;
                hud.HintClicked += OnHintClicked;
                hud.BoosterClicked += OnBoosterClicked;
                hud.ColorChosen += OnColorChosen;
                hud.BoosterCancelClicked += OnBoosterCancelClicked;
                hud.StarLost += OnStarLost;
                hud.StarGained += OnStarGained;
                views.BoosterOffer.BuyClicked += OnBoosterBuyClicked;
                views.Win.ContinueClicked += OnContinueClicked;
                views.Win.BackClicked += OnWinBackClicked;
                views.Lose.ReviveClicked += OnReviveClicked;
                views.Lose.RestartClicked += OnRestartClicked;
                views.Settings.HomeClicked += OnHomeClicked;
                views.Settings.SoundToggled += OnSoundToggled;
                views.Settings.MusicToggled += OnMusicToggled;
                views.NoHearts.RefillClicked += OnRefillClicked;
                views.Mechanic.ContinueClicked += OnMechanicContinue;
            }
            else
            {
                board.CellSwiped -= OnCellSwiped;
                board.CellTapped -= OnCellTapped;
                hud.SettingsClicked -= OnSettingsClicked;
                hud.RestartClicked -= OnRestartClicked;
                hud.LevelBadgeTripleTapped -= OnCheatOpened;
                views.Cheat.WinClicked -= OnCheatWin;
                views.Cheat.SolveStepClicked -= OnCheatSolveStep;
                views.Cheat.LevelStepped -= OnCheatLevelStepped;
                views.Cheat.OpenLevelClicked -= OnCheatOpenLevel;
                views.Cheat.RefillClicked -= OnRefillClicked;
                views.Cheat.CoinsClicked -= OnCheatCoins;
                views.Cheat.UnlockAllClicked -= OnCheatUnlockAll;
                views.Cheat.OnboardingClicked -= OnCheatOnboarding;
                hud.HintClicked -= OnHintClicked;
                hud.BoosterClicked -= OnBoosterClicked;
                hud.ColorChosen -= OnColorChosen;
                hud.BoosterCancelClicked -= OnBoosterCancelClicked;
                hud.StarLost -= OnStarLost;
                hud.StarGained -= OnStarGained;
                views.BoosterOffer.BuyClicked -= OnBoosterBuyClicked;
                views.Win.ContinueClicked -= OnContinueClicked;
                views.Win.BackClicked -= OnWinBackClicked;
                views.Lose.ReviveClicked -= OnReviveClicked;
                views.Lose.RestartClicked -= OnRestartClicked;
                views.Settings.HomeClicked -= OnHomeClicked;
                views.Settings.SoundToggled -= OnSoundToggled;
                views.Settings.MusicToggled -= OnMusicToggled;
                views.NoHearts.RefillClicked -= OnRefillClicked;
                views.Mechanic.ContinueClicked -= OnMechanicContinue;
            }
        }
    }
}
