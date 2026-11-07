using Meowtosis.Domain;
using Meowtosis.Gameplay;
using NUnit.Framework;

namespace Meowtosis.Tests
{
    public class GameSessionTests
    {
        private static GameSession Session(params string[] rows)
        {
            var definition = new LevelDefinition { Id = "test", Par = 1 };
            definition.Rows.AddRange(rows);
            return new GameSession(definition);
        }

        private static IGameCommand Swipe(GameSession session, string move)
        {
            var parsed = Move.Parse(move);
            return new SwipeCommand(Rules.Swipes, session.Level.IndexOf(parsed.Row, parsed.Column), parsed.Direction);
        }

        [Test]
        public void PlayingAMoveRecordsItAndWinsWhenTheBoardIsFull()
        {
            var session = Session("A.");
            Assert.That(session.TryPlay(Swipe(session, "0 0 right"), out var result), Is.True);
            Assert.That(session.Board, Is.SameAs(result.After));
            Assert.That(session.MovesMade, Is.EqualTo(1));
            Assert.That(session.Status, Is.EqualTo(SessionStatus.Won));
        }

        [Test]
        public void RefusedMoveChangesNothing()
        {
            var session = Session("A.");
            var start = session.Board;
            Assert.That(session.TryPlay(Swipe(session, "0 0 left"), out _), Is.False);
            Assert.That(session.Board, Is.SameAs(start));
            Assert.That(session.CanUndo, Is.False);
        }

        [Test]
        public void NoMoreMovesAfterTheLevelIsLost()
        {
            var session = Session("AA.", "B..");
            Assert.That(session.TryPlay(Swipe(session, "0 1 right"), out _), Is.True);
            Assert.That(session.Status, Is.EqualTo(SessionStatus.Lost));
            Assert.That(session.TryPlay(Swipe(session, "1 0 right"), out _), Is.False);
            Assert.That(session.CanUndo, Is.True);
        }

        [Test]
        public void UndoGoesBackOneMoveAndRestartGoesBackToTheStart()
        {
            var session = Session("A..");
            var start = session.Board;
            Assert.That(session.TryPlay(Swipe(session, "0 0 right"), out var first), Is.True);
            Assert.That(session.TryPlay(Swipe(session, "0 1 right"), out _), Is.True);

            Assert.That(session.TryUndo(out _), Is.True);
            Assert.That(session.Board, Is.SameAs(first.After));
            Assert.That(session.MovesMade, Is.EqualTo(1));

            session.Restart();
            Assert.That(session.Board.ToRows(), Is.EqualTo(start.ToRows()));
            Assert.That(session.CanUndo, Is.False);
            Assert.That(session.TryUndo(out _), Is.False);
        }

        [TestCase(4, 4, 3)]
        [TestCase(3, 4, 3)]
        [TestCase(6, 4, 2)]
        [TestCase(7, 4, 1)]
        public void StarsDependOnMovesOverPar(int movesMade, int par, int stars) =>
            Assert.That(StarRating.For(movesMade, par), Is.EqualTo(stars));

        [Test]
        public void GoalProgressCountsFilledCellsAndBoxes()
        {
            var definition = new LevelDefinition { Id = "test" };
            definition.Rows.AddRange(new[] { "AB~", "B>." });
            definition.Goal.AddRange(new[] { "B..", ".A." });
            var progress = GoalProgress.Of(new Level(definition).CreateStartBoard());

            Assert.That(progress.FilledCells, Is.EqualTo(3));
            Assert.That(progress.CellsToFill, Is.EqualTo(5));
            Assert.That(progress.BoxesDone, Is.EqualTo(0));
            Assert.That(progress.BoxCount, Is.EqualTo(2));
        }
    }
}
