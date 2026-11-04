using System;
using System.Collections.Generic;
using System.Linq;
using Meowtosis.Domain;
using Meowtosis.Gameplay;
using NUnit.Framework;

namespace Meowtosis.Tests
{
    // Danh sách level xuất từ prototype là "đáp án vàng" cho luật C#: mỗi lời giải đã lưu phải
    // thắng đúng par nước, và bộ giải C# phải tìm ra cùng par.
    public class LevelCatalogTests
    {
        private static IEnumerable<string> LevelIds() => TestCatalog.Levels.Select(level => level.Id);

        private static IEnumerable<string> TutorialLevelIds() =>
            TestCatalog.Levels.Where(level => level.Tutorial.Count > 0).Select(level => level.Id);

        [Test]
        public void CatalogHasLevelsWithUniqueIds()
        {
            var ids = LevelIds().ToList();
            Assert.That(ids, Is.Not.Empty);
            Assert.That(ids, Is.Unique);
        }

        [TestCaseSource(nameof(LevelIds))]
        public void StartBoardIsPlayable(string levelId)
        {
            var board = new Level(TestCatalog.Find(levelId)).CreateStartBoard();
            Assert.That(GameOutcome.IsLost(board), Is.False, "thua trước cả nước đầu");
            Assert.That(GameOutcome.IsComplete(board), Is.False, "thắng trước cả nước đầu");
            Assert.That(ClearRunsStep.FindRuns(board), Is.Empty, "có sẵn dãy 3 trên bàn");
        }

        [TestCaseSource(nameof(LevelIds))]
        public void StoredSolutionWinsInParMoves(string levelId)
        {
            var definition = TestCatalog.Find(levelId);
            var board = new Level(definition).CreateStartBoard();
            Assert.That(definition.Solution.Count, Is.EqualTo(definition.Par));
            foreach (var move in definition.Solution)
            {
                Assert.That(Rules.Swipes.TrySwipe(board, move, out var result), Is.True, $"nước {move} bị từ chối");
                board = result.After;
                Assert.That(GameOutcome.IsLost(board), Is.False, $"nước {move} làm thua");
            }
            Assert.That(GameOutcome.IsWon(board), Is.True);
        }

        [TestCaseSource(nameof(LevelIds))]
        public void SolverFindsPar(string levelId)
        {
            var definition = TestCatalog.Find(levelId);
            var result = Rules.Solver.Solve(new Level(definition).CreateStartBoard());
            Assert.That(result.Status, Is.EqualTo(SolveStatus.Solved));
            Assert.That(result.Moves.Count, Is.EqualTo(definition.Par));
        }

        // Mọi bước hướng dẫn phải đi được và không làm thua. Hướng dẫn trọn cả level thì thắng đúng par nước.
        [TestCaseSource(nameof(TutorialLevelIds))]
        public void TutorialStepsArePlayable(string levelId)
        {
            var definition = TestCatalog.Find(levelId);
            var board = new Level(definition).CreateStartBoard();
            foreach (var step in definition.Tutorial)
            {
                Assert.That(Rules.Swipes.TrySwipe(board, step.Move, out var result), Is.True, $"bước {step.Move} bị từ chối");
                board = result.After;
                Assert.That(GameOutcome.IsLost(board), Is.False, $"bước {step.Move} làm thua");
            }
            Assert.That(GameOutcome.IsWon(board), Is.EqualTo(definition.Tutorial.Count == definition.Par));
        }

        // Thẻ riêng của level phải là một cơ chế có thật, và đúng 3 thử thách đang khai.
        [Test]
        public void LevelCardsNameKnownMechanics()
        {
            var cards = TestCatalog.Levels.Where(level => level.Card.Length > 0).ToList();
            Assert.That(cards.Select(level => level.Card), Is.EquivalentTo(new[] { "ClearForRoom", "ChainWake", "GermClear" }));
            foreach (var level in cards)
            {
                Assert.That(Enum.TryParse<Mechanic>(level.Card, out var card), Is.True, level.Id);
                Assert.That(MechanicDetector.In(level, 0), Has.Member(card), level.Id);
            }
        }

        // Bẫy phải thật sự làm thua, và thua vì khử 3: đó là luật nó dạy.
        [Test]
        public void TrapLevelsLoseByClearing()
        {
            var trapLevels = TestCatalog.Levels.Where(level => level.Trap != null).ToList();
            Assert.That(trapLevels, Is.Not.Empty);
            foreach (var definition in trapLevels)
            {
                var board = new Level(definition).CreateStartBoard();
                Assert.That(Rules.Swipes.TrySwipe(board, definition.Trap.Move, out var result), Is.True, definition.Id);
                Assert.That(GameOutcome.IsLost(result.After), Is.True, definition.Id);
                Assert.That(result.Stages.SelectMany(stage => stage.Events).Any(turnEvent => turnEvent.Kind == TurnEventKind.Cleared), Is.True, definition.Id);
            }
        }
    }
}
