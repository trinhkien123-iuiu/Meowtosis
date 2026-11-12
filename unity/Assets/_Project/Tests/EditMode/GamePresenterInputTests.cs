#if UNITY_EDITOR || MEOWTOSIS_PRESENTER_TESTS
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Meowtosis.Domain;
using Meowtosis.Gameplay;
using Meowtosis.Presentation;
using NUnit.Framework;

namespace Meowtosis.Tests
{
    public class GamePresenterInputTests
    {
        [Test]
        public async Task BackgroundHintChargesOnlyOnceWhenTheBoardStillMatches()
        {
            using var game = HintGame();
            game.Start();
            var search = game.BeginHintSearch();
            game.Hud.Emit("HintClicked");
            Assert.That(game.Progress.BoosterCount(BoosterKind.Hint), Is.EqualTo(PlayerProgress.StartingBoosters));
            await search;
            game.Presenter.Tick(0f);
            game.Hud.Emit("HintClicked");
            Assert.That(game.Board.CallsTo("ShowHint"), Is.EqualTo(1));
            Assert.That(game.Progress.BoosterCount(BoosterKind.Hint), Is.EqualTo(PlayerProgress.StartingBoosters - 1));
        }

        [Test]
        public async Task SwipingDuringHintSearchDiscardsTheOldResultWithoutCharging()
        {
            using var game = HintGame();
            game.Start();
            var search = game.BeginHintSearch();
            game.Swipe(0, Direction.Right);
            Assert.That(game.Board.CallsTo("Play"), Is.EqualTo(1), "Searching must not lock board input");
            await search;
            game.Presenter.Tick(0f);
            Assert.That(game.Board.CallsTo("ShowHint"), Is.Zero);
            Assert.That(game.Progress.BoosterCount(BoosterKind.Hint), Is.EqualTo(PlayerProgress.StartingBoosters));
        }

        [Test]
        public async Task RestartingDuringHintSearchDiscardsTheOldResultWithoutCharging()
        {
            using var game = HintGame();
            game.Start();
            var search = game.BeginHintSearch();
            game.Hud.Emit("RestartClicked");
            await search;
            game.Presenter.Tick(0f);
            Assert.That(game.Board.CallsTo("ShowHint"), Is.Zero);
            Assert.That(game.Progress.BoosterCount(BoosterKind.Hint), Is.EqualTo(PlayerProgress.StartingBoosters));
        }

        private static TestGame HintGame()
        {
            var game = new TestGame("background-hint", 2, "A.", "..");
            game.Definition.Solution.Add(Move.Parse("0 0 right"));
            game.Definition.Solution.Add(Move.Parse("0 1 down"));
            game.Definition.Solution.Add(Move.Parse("0 0 down"));
            return game;
        }

        [Test]
        public async Task IdleHelpRunsInTheBackgroundAndDoesNotSpendABooster()
        {
            using var game = HintGame();
            game.Start();
            game.Presenter.Tick(4.1f);
            var search = game.PendingHintSearch;
            Assert.That(search, Is.Not.Null);
            Assert.That(game.Board.CallsTo("ShowTutorial"), Is.Zero);
            await search;
            game.Presenter.Tick(0f);
            Assert.That(game.Board.CallsTo("ShowTutorial"), Is.EqualTo(1));
            Assert.That(game.Progress.BoosterCount(BoosterKind.Hint), Is.EqualTo(PlayerProgress.StartingBoosters));
        }

        [Test]
        public async Task PaidHintCanReplaceAnIdleSearchWithoutDisplayingTheOldResult()
        {
            using var game = HintGame();
            game.Start();
            game.Presenter.Tick(4.1f);
            var search = game.BeginHintSearch();
            await search;
            game.Presenter.Tick(0f);
            Assert.That(game.Board.CallsTo("ShowTutorial"), Is.Zero);
            Assert.That(game.Board.CallsTo("ShowHint"), Is.EqualTo(1));
            Assert.That(game.Progress.BoosterCount(BoosterKind.Hint), Is.EqualTo(PlayerProgress.StartingBoosters - 1));
        }

        [Test]
        public void TutorialPraiseDoesNotDelayTheNextSwipe()
        {
            using var game = new TestGame("tutorial", 1, "A..", "...");
            game.Definition.Tutorial.Add(new TutorialStep(Move.Parse("0 0 right"), "Swipe right"));
            game.Start();
            game.Swipe(0, Direction.Right);
            game.FinishMove();
            Assert.That(game.Hud.LastTip, Is.EqualTo(GameTexts.TutorialDone));

            game.Swipe(0, Direction.Down);
            Assert.That(game.Board.CallsTo("Play"), Is.EqualTo(2), "No timer tick is needed after praise");
        }

        [Test]
        public void SpecificWarningAllowsAnImmediateSafeSwipe()
        {
            using var game = new TestGame("meow-158", 1, "AA.", "#.B");
            game.Start();
            game.Swipe(1, Direction.Right);
            Assert.That(game.Board.CallsTo("Play"), Is.Zero);
            Assert.That(game.Hud.LastTip, Is.EqualTo(GameTexts.TrapWarnTrietTieu));

            game.Swipe(1, Direction.Down);
            Assert.That(game.Board.CallsTo("Play"), Is.EqualTo(1), "The safe swipe must not wait 3.5 seconds");
        }

        [Test]
        public void GeneralOnboardingWarningAllowsAnImmediateSafeSwipe()
        {
            using var game = new TestGame("warning", 1, "AA.", "B..");
            game.Start();
            game.Swipe(1, Direction.Right);
            Assert.That(game.Board.CallsTo("Play"), Is.Zero);
            Assert.That(game.Hud.LastTip, Is.Not.Empty);

            game.Swipe(3, Direction.Right);
            Assert.That(game.Board.CallsTo("Play"), Is.EqualTo(1));
        }

        [Test]
        public void PlaybackStillBlocksOverlappingMoves()
        {
            using var game = new TestGame("playback", 2, "A..", "...");
            game.Start();
            game.Swipe(0, Direction.Right);
            game.Swipe(0, Direction.Down);
            Assert.That(game.Board.CallsTo("Play"), Is.EqualTo(1));
            game.FinishMove();
            game.Swipe(0, Direction.Down);
            Assert.That(game.Board.CallsTo("Play"), Is.EqualTo(2));
        }

        [Test]
        public void StartingTheNextMoveCancelsTheOldPraiseTimeout()
        {
            using var game = new TestGame("praise-timeout", 1, "A..", "...");
            game.Definition.Tutorial.Add(new TutorialStep(Move.Parse("0 0 right"), "Swipe right"));
            game.Start();
            game.Swipe(0, Direction.Right);
            game.FinishMove();
            game.Swipe(0, Direction.Down);
            var refreshes = game.Hud.CallsTo("ShowLevel");
            game.Presenter.Tick(3.1f);
            Assert.That(game.Hud.CallsTo("ShowLevel"), Is.EqualTo(refreshes),
                "An expired message must not refresh the HUD during the next playback");
        }

        [Test]
        public void SettingsStillBlockBoardInput()
        {
            using var game = new TestGame("settings", 2, "A..", "...");
            game.Start();
            game.Settings.IsOpen = true;
            game.Swipe(0, Direction.Right);
            Assert.That(game.Board.CallsTo("Play"), Is.Zero);
            game.Settings.IsOpen = false;
            game.Swipe(0, Direction.Right);
            Assert.That(game.Board.CallsTo("Play"), Is.EqualTo(1));
        }

        [Test]
        public void LossPopupStillWaitsForItsResultDelay()
        {
            using var game = new TestGame("loss", 2, "AA.", "B..");
            game.Start();
            game.Swipe(1, Direction.Right);
            game.FinishMove();
            game.Presenter.Tick(0.1f);
            Assert.That(game.Lose.CallsTo("Show"), Is.Zero);
            game.Presenter.Tick(1f);
            Assert.That(game.Lose.CallsTo("Show"), Is.EqualTo(1));
        }

        private sealed class TestGame : IDisposable
        {
            public readonly LevelDefinition Definition;
            public readonly GamePresenter Presenter;
            public readonly PlayerProgress Progress;
            public readonly PresenterViewStub Board;
            public readonly PresenterViewStub Hud;
            public readonly PresenterViewStub Settings;
            public readonly PresenterViewStub Lose;

            public TestGame(string id, int block, params string[] rows)
            {
                Definition = new LevelDefinition { Id = id, Block = block, Par = 10 };
                Definition.Rows.AddRange(rows);
                var board = PresenterViewStub.Create<IBoardView>(out Board);
                var hud = PresenterViewStub.Create<IHudView>(out Hud);
                var settings = PresenterViewStub.Create<ISettingsPopup>(out Settings);
                var lose = PresenterViewStub.Create<ILosePopup>(out Lose);
                var views = new GameViews(board, hud, PresenterViewStub.Create<IWinPopup>(out _), lose, settings,
                    PresenterViewStub.Create<INoHeartsPopup>(out _), PresenterViewStub.Create<IMechanicPopup>(out _),
                    PresenterViewStub.Create<ISoundPlayer>(out _), PresenterViewStub.Create<IAmbientEvents>(out _),
                    PresenterViewStub.Create<ICheatPopup>(out _), PresenterViewStub.Create<IBoosterOfferPopup>(out _));
                Progress = PlayerProgress.CreateNew();
                Presenter = new GamePresenter(new[] { Definition }, GameRules.CreateStandard(), Progress,
                    new MemoryStore(), new FixedClock(), views, PresenterViewStub.Create<ISceneNavigator>(out _));
            }

            public void Start() => Presenter.Start(1);
            public void Swipe(int cell, Direction direction) => Board.Emit("CellSwiped", cell, direction);
            public void FinishMove() => Board.FinishMove();
            public Task PendingHintSearch => (Task)typeof(GamePresenter)
                .GetField("hintSearch", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Presenter);
            public Task BeginHintSearch()
            {
                Hud.Emit("HintClicked");
                var search = PendingHintSearch;
                Assert.That(search, Is.Not.Null);
                return search;
            }
            public void Dispose() => Presenter.Dispose();
        }

        private sealed class MemoryStore : IProgressStore
        {
            public PlayerProgress Load() => PlayerProgress.CreateNew();
            public void Save(PlayerProgress progress) { }
        }

        private sealed class FixedClock : IClock
        {
            public DateTime UtcNow => new DateTime(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc);
        }
    }

    // Editor-only proxies let tests drive the real presenter without creating scene objects.
    public class PresenterViewStub : DispatchProxy
    {
        private readonly Dictionary<string, Delegate> events = new Dictionary<string, Delegate>();
        private readonly Dictionary<string, int> calls = new Dictionary<string, int>();
        private Action moveFinished;
        public bool IsOpen;
        public string LastTip = "";

        public static T Create<T>(out PresenterViewStub stub) where T : class
        {
            var view = DispatchProxy.Create<T, PresenterViewStub>();
            stub = (PresenterViewStub)(object)view;
            return view;
        }

        public int CallsTo(string method) => calls.TryGetValue(method, out var count) ? count : 0;
        public void Emit(string name, params object[] arguments)
        {
            if (events.TryGetValue(name, out var action)) action?.DynamicInvoke(arguments);
        }

        public void FinishMove()
        {
            var finished = moveFinished;
            moveFinished = null;
            Assert.That(finished, Is.Not.Null, "A move must be playing before finishing it");
            finished();
        }

        protected override object Invoke(MethodInfo method, object[] args)
        {
            var name = method.Name;
            calls[name] = CallsTo(name) + 1;
            if (name.StartsWith("add_", StringComparison.Ordinal) || name.StartsWith("remove_", StringComparison.Ordinal))
            {
                var eventName = name.Substring(name.IndexOf('_') + 1);
                events.TryGetValue(eventName, out var previous);
                events[eventName] = name.StartsWith("add_", StringComparison.Ordinal)
                    ? Delegate.Combine(previous, (Delegate)args[0]) : Delegate.Remove(previous, (Delegate)args[0]);
                return null;
            }
            if (name == "get_IsOpen") return IsOpen;
            if (name == "ShowTip") LastTip = (string)args[0];
            if (name == "Play" && method.DeclaringType == typeof(IBoardView)) moveFinished = (Action)args[1];
            if (method.ReturnType == typeof(void)) return null;
            return method.ReturnType.IsValueType ? Activator.CreateInstance(method.ReturnType) : null;
        }
    }
}
#endif
