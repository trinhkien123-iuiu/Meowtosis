using System;
using Meowtosis.Domain;
using Meowtosis.Gameplay;
using NUnit.Framework;

namespace Meowtosis.Tests
{
    public class LevelRunTests
    {
        private sealed class FixedClock : IClock
        {
            public DateTime UtcNow { get; set; } = new DateTime(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);
        }

        private sealed class MemoryStore : IProgressStore
        {
            public int SaveCount { get; private set; }

            public PlayerProgress Load() => PlayerProgress.CreateNew();

            public void Save(PlayerProgress progress) => SaveCount++;
        }

        private readonly FixedClock clock = new FixedClock();
        private readonly MemoryStore store = new MemoryStore();

        private LevelRun Run(int par, params string[] rows)
        {
            var definition = new LevelDefinition { Id = "test", Par = par };
            definition.Rows.AddRange(rows);
            return new LevelRun(definition, PlayerProgress.CreateNew(), store, clock);
        }

        private static IGameCommand Swipe(LevelRun run, string move)
        {
            var parsed = Move.Parse(move);
            return new SwipeCommand(Rules.Swipes, run.Session.Level.IndexOf(parsed.Row, parsed.Column), parsed.Direction);
        }

        [Test]
        public void WinningPaysCoinsPerStarAndKeepsTheBest()
        {
            var run = Run(1, "A.");
            var savesBefore = store.SaveCount;
            Assert.That(run.TryPlay(Swipe(run, "0 0 right"), out var outcome), Is.True);
            Assert.That(outcome.Stars, Is.EqualTo(3));
            Assert.That(outcome.CoinsEarned, Is.EqualTo(3 * PlayerProgress.CoinsPerStar));
            Assert.That(run.Progress.Coins, Is.EqualTo(30));
            Assert.That(run.Progress.BestStarsFor("test"), Is.EqualTo(3));
            Assert.That(store.SaveCount, Is.EqualTo(savesBefore + 1));
        }

        [Test]
        public void LosingCostsAHeartAndReviveGivesItBack()
        {
            var run = Run(2, "AA.", "B..");
            Assert.That(run.TryPlay(Swipe(run, "0 1 right"), out var outcome), Is.True);
            Assert.That(outcome.LostHeart, Is.True);
            Assert.That(run.Progress.Hearts, Is.EqualTo(PlayerProgress.MaximumHearts - 1));
            Assert.That(run.CanUndo, Is.False, "đã thua thì phải hồi sinh hoặc chơi lại");

            Assert.That(run.TryRevive(), Is.True);
            Assert.That(run.Session.Status, Is.EqualTo(SessionStatus.Playing));
            Assert.That(run.Progress.Hearts, Is.EqualTo(PlayerProgress.MaximumHearts));
            Assert.That(run.Progress.BoosterCount(BoosterKind.Revive), Is.EqualTo(PlayerProgress.StartingBoosters - 1));
        }

        [Test]
        public void BoosterMovesDoNotCostStars()
        {
            var run = Run(1, "A..", "B..");
            var full = run.Stars;
            Assert.That(run.TryPlay(new TransplantCommand(Rules.Boosters, 0, 2), out _), Is.True);
            Assert.That(run.Session.MovesMade, Is.EqualTo(1));
            Assert.That(run.Session.SwipesMade, Is.EqualTo(0));
            Assert.That(run.Stars.Fill, Is.EqualTo(full.Fill), "dùng booster không bị trừ bước");
            Assert.That(run.Stars.Stars, Is.EqualTo(3));
        }

        [Test]
        public void BoosterIsSpentAndRefundedOnUndo()
        {
            var run = Run(2, "A..", "B..");
            var transplant = new TransplantCommand(Rules.Boosters, 0, 2);
            Assert.That(run.TryPlay(transplant, out _), Is.True);
            Assert.That(run.Progress.BoosterCount(BoosterKind.Transplant), Is.EqualTo(PlayerProgress.StartingBoosters - 1));

            Assert.That(run.TryUndo(out var refunded), Is.True);
            Assert.That(refunded, Is.EqualTo(BoosterKind.Transplant));
            Assert.That(run.Progress.BoosterCount(BoosterKind.Transplant), Is.EqualTo(PlayerProgress.StartingBoosters));
        }

        [Test]
        public void NoBoosterLeftMeansTheBoosterMoveIsRefused()
        {
            var run = Run(2, "A..", "B..");
            for (var used = 0; used < PlayerProgress.StartingBoosters; used++) run.Progress.TrySpendBooster(BoosterKind.Mutate);
            Assert.That(run.TryPlay(new MutateCommand(Rules.Boosters, 0, Species.Blue), out _), Is.False);
            Assert.That(run.Session.MovesMade, Is.EqualTo(0));
        }

        [Test]
        public void HeartsComeBackOneEveryRecoveryTime()
        {
            var progress = PlayerProgress.CreateNew();
            progress.LoseHeart(clock.UtcNow);
            progress.LoseHeart(clock.UtcNow);
            Assert.That(progress.Hearts, Is.EqualTo(1));

            Assert.That(progress.RecoverHearts(clock.UtcNow + PlayerProgress.HeartRecoveryTime), Is.True);
            Assert.That(progress.Hearts, Is.EqualTo(2));
            progress.RecoverHearts(clock.UtcNow + PlayerProgress.HeartRecoveryTime + PlayerProgress.HeartRecoveryTime);
            Assert.That(progress.Hearts, Is.EqualTo(PlayerProgress.MaximumHearts));
            Assert.That(progress.NextHeartAt, Is.Null);
        }

        [Test]
        public void BuyingABoosterCostsCoins()
        {
            var progress = PlayerProgress.CreateNew();
            Assert.That(progress.TryBuyBooster(BoosterKind.Mutate), Is.False);
            foreach (var id in new[] { "a", "b", "c", "d", "e" }) progress.RecordWin(id, 3);
            Assert.That(progress.TryBuyBooster(BoosterKind.Mutate), Is.True);
            Assert.That(progress.Coins, Is.EqualTo(150 - PlayerProgress.BoosterPrice));
            Assert.That(progress.BoosterCount(BoosterKind.Mutate), Is.EqualTo(PlayerProgress.StartingBoosters + PlayerProgress.BoosterPackSize));
        }

        [TestCase(0, 4, 1f, 3)]
        [TestCase(4, 4, 2f / 3f, 3)]
        [TestCase(5, 4, 0.5f, 2)]
        [TestCase(6, 4, 1f / 3f, 2)]
        [TestCase(7, 4, 4f / 15f, 1)]
        [TestCase(30, 4, 0f, 1)]
        public void StarMeterDrainsAsMovesPassPar(int movesMade, int par, float fill, int stars)
        {
            var meter = StarMeter.For(movesMade, par);
            Assert.That(meter.Fill, Is.EqualTo(fill).Within(0.0001f));
            Assert.That(meter.Stars, Is.EqualTo(stars));
        }

        [Test]
        public void MechanicsFollowTheLevelContent()
        {
            // Nhân đôi, đẩy, thay thế, khử 3 đã dạy ở màn hướng dẫn đầu game nên level thường không có cơ chế mới nào.
            var plain = TestLevel.FromRows("AB.", "...").Build();
            Assert.That(MechanicDetector.In(plain), Is.Empty);

            var full = TestLevel.FromRows("Aa~", "B>.").WithGoal("..B", "...").WithMembrane(1, 2, Direction.Left).Build();
            var found = MechanicDetector.In(full);
            Assert.That(found, Has.Member(Mechanic.TargetBox));
            Assert.That(found, Has.Member(Mechanic.Membrane));
            Assert.That(found, Has.Member(Mechanic.Toxic));
            Assert.That(found, Has.Member(Mechanic.Virus));
            Assert.That(found, Has.No.Member(Mechanic.Sleep), "mèo ngủ cạnh mèo cùng màu thức dậy ngay từ đầu");
        }

        private LevelRun TrapRun(PlayerProgress progress)
        {
            var definition = new LevelDefinition { Id = "trap", Par = 1, Trap = new TutorialStep(Move.Parse("0 1 right"), "bẫy") };
            definition.Rows.AddRange(new[] { "AA.", "##B" });
            definition.Tutorial.Add(new TutorialStep(Move.Parse("1 2 up"), "thắng"));
            return new LevelRun(definition, progress, store, clock);
        }

        [Test]
        public void HintCountsLikeOtherBoosters()
        {
            var progress = PlayerProgress.CreateNew();
            Assert.That(progress.BoosterCount(BoosterKind.Hint), Is.EqualTo(PlayerProgress.StartingBoosters));
            for (var used = 0; used < PlayerProgress.StartingBoosters; used++) Assert.That(progress.TrySpendBooster(BoosterKind.Hint), Is.True);
            Assert.That(progress.TrySpendBooster(BoosterKind.Hint), Is.False, "hết lượt thì không dùng được nữa");
            progress.RefillBoosters();
            Assert.That(progress.BoosterCount(BoosterKind.Hint), Is.EqualTo(PlayerProgress.StartingBoosters));
        }

        [Test]
        public void OldSaveWithoutHintGetsTheStartingCount()
        {
            var progress = PlayerProgress.Restore(3, null, 0, new System.Collections.Generic.Dictionary<BoosterKind, int>(),
                new System.Collections.Generic.Dictionary<string, int>(), new string[0]);
            Assert.That(progress.BoosterCount(BoosterKind.Hint), Is.EqualTo(PlayerProgress.StartingBoosters));
        }

        [Test]
        public void RestoringCorruptResourceCountsClampsThemToValidRanges()
        {
            var progress = PlayerProgress.Restore(-5, null, -10,
                new System.Collections.Generic.Dictionary<BoosterKind, int> { [BoosterKind.Hint] = -3 },
                new System.Collections.Generic.Dictionary<string, int> { ["negative"] = -1, ["too-many"] = 9 },
                Array.Empty<string>());
            Assert.That(progress.Hearts, Is.Zero);
            Assert.That(progress.Coins, Is.Zero);
            Assert.That(progress.BoosterCount(BoosterKind.Hint), Is.Zero);
            Assert.That(progress.TrySpendBooster(BoosterKind.Hint), Is.False);
            Assert.That(progress.BestStarsFor("negative"), Is.Zero);
            Assert.That(progress.BestStarsFor("too-many"), Is.EqualTo(3));
        }

        [Test]
        public void MissingRecoveryTimerIsRepairedAndThenRecoversNormally()
        {
            var progress = PlayerProgress.Restore(1, null, 0,
                new System.Collections.Generic.Dictionary<BoosterKind, int>(),
                new System.Collections.Generic.Dictionary<string, int>(), Array.Empty<string>());
            var now = clock.UtcNow;
            Assert.That(progress.RecoverHearts(now), Is.True);
            Assert.That(progress.Hearts, Is.EqualTo(1));
            Assert.That(progress.NextHeartAt, Is.EqualTo(now + PlayerProgress.HeartRecoveryTime));
            Assert.That(progress.RecoverHearts(now), Is.False);
            Assert.That(progress.RecoverHearts(now + PlayerProgress.HeartRecoveryTime + PlayerProgress.HeartRecoveryTime), Is.True);
            Assert.That(progress.Hearts, Is.EqualTo(PlayerProgress.MaximumHearts));
            Assert.That(progress.NextHeartAt, Is.Null);
        }

        [Test]
        public void FullHeartsDiscardAStaleRecoveryTimer()
        {
            var progress = PlayerProgress.Restore(PlayerProgress.MaximumHearts, clock.UtcNow, 0,
                new System.Collections.Generic.Dictionary<BoosterKind, int>(),
                new System.Collections.Generic.Dictionary<string, int>(), Array.Empty<string>());
            Assert.That(progress.RecoverHearts(clock.UtcNow), Is.True);
            Assert.That(progress.NextHeartAt, Is.Null);
            Assert.That(progress.RecoverHearts(clock.UtcNow), Is.False);
        }

        [Test]
        public void TrapLosesWithoutCostingAHeartThenRestartsForRealPlay()
        {
            var run = TrapRun(PlayerProgress.CreateNew());
            Assert.That(run.GuidedStep.Text, Is.EqualTo("bẫy"));

            Assert.That(run.TryPlay(Swipe(run, "0 1 right"), out var outcome), Is.True);
            Assert.That(outcome.SprungTrap, Is.True);
            Assert.That(outcome.LostHeart, Is.False);
            Assert.That(run.Progress.Hearts, Is.EqualTo(PlayerProgress.MaximumHearts));
            Assert.That(run.GuidedStep, Is.Null, "đang ở bàn thua thì chưa có bước kế");

            run.FinishTrap();
            Assert.That(run.Session.Status, Is.EqualTo(SessionStatus.Playing));
            Assert.That(run.Session.MovesMade, Is.EqualTo(0));
            Assert.That(run.GuidedStep.Text, Is.EqualTo("thắng"));
            Assert.That(run.Progress.HasSeenMechanic(nameof(Mechanic.Clear)), Is.True);

            Assert.That(run.TryPlay(Swipe(run, "1 2 up"), out var win), Is.True);
            Assert.That(win.Stars, Is.EqualTo(3));
        }

        [Test]
        public void TrapStillRunsWhenTheCardMarksTheMechanicSeenAfterOpening()
        {
            var progress = PlayerProgress.CreateNew();
            var run = TrapRun(progress);
            progress.MarkMechanicSeen(nameof(Mechanic.Clear));   // thẻ cơ chế hiện ngay sau khi mở level
            Assert.That(run.IsTrapActive, Is.True);
            Assert.That(run.GuidedStep.Text, Is.EqualTo("bẫy"));
        }

        [Test]
        public void KnownMechanicSkipsTheTrapAndLosingCostsAHeartAgain()
        {
            var progress = PlayerProgress.CreateNew();
            progress.MarkMechanicSeen(nameof(Mechanic.Clear));
            var run = TrapRun(progress);
            Assert.That(run.IsTrapActive, Is.False);
            Assert.That(run.GuidedStep.Text, Is.EqualTo("thắng"));

            Assert.That(run.TryPlay(Swipe(run, "0 1 right"), out var outcome), Is.True);
            Assert.That(outcome.SprungTrap, Is.False);
            Assert.That(outcome.LostHeart, Is.True);
        }

        [Test]
        public void LossReportNamesTheClearedColor()
        {
            var run = Run(2, "AA.", "B..");
            Assert.That(run.TryPlay(Swipe(run, "0 1 right"), out var outcome), Is.True);
            var report = LossReport.Of(outcome.Move);
            Assert.That(report.Color, Is.EqualTo(Species.Red));
            Assert.That(report.Cause, Is.EqualTo(LossCause.Cleared));
            Assert.That(report.Count, Is.EqualTo(3));
        }

        [Test]
        public void LossReportNamesTheReplacedColor()
        {
            var run = Run(2, "AA.", "#B#");
            Assert.That(run.TryPlay(Swipe(run, "0 1 down"), out var outcome), Is.True);
            var report = LossReport.Of(outcome.Move);
            Assert.That(report.Color, Is.EqualTo(Species.Blue));
            Assert.That(report.Cause, Is.EqualTo(LossCause.Replaced));
        }

        [Test]
        public void LossReportIsEmptyWhenNoColorIsLost()
        {
            var run = Run(1, "A.");
            Assert.That(run.TryPlay(Swipe(run, "0 0 right"), out var outcome), Is.True);
            Assert.That(LossReport.Of(outcome.Move), Is.Null);
        }

        [Test]
        public void EachMechanicIsShownOnlyOnce()
        {
            var progress = PlayerProgress.CreateNew();
            Assert.That(progress.MarkMechanicSeen(nameof(Mechanic.Virus)), Is.True);
            Assert.That(progress.MarkMechanicSeen(nameof(Mechanic.Virus)), Is.False);
        }

        [Test]
        public void HintFollowsTheStoredSolutionAndPointsBackAfterAMistake()
        {
            var definition = TestCatalog.Levels[1];
            var run = new LevelRun(definition, PlayerProgress.CreateNew(), store, clock);
            var advisor = new HintAdvisor(Rules.Swipes, Rules.Solver);

            var hint = advisor.Suggest(run.Session, HintAdvisor.DefaultTimeLimit);
            Assert.That(hint.Kind, Is.EqualTo(HintKind.Move));
            Assert.That(hint.MovesLeft, Is.EqualTo(definition.Par));
            Assert.That(hint.Move.ToString(), Is.EqualTo(definition.Solution[0].ToString()));
        }
    }
}
