using System.Linq;
using Meowtosis.Domain;
using NUnit.Framework;

namespace Meowtosis.Tests
{
    // Mọi ví dụ trong docs/mechanics.md, chạy bằng luật C#. Description là mã ví dụ trong tài liệu
    // (S1, V4…), trùng mã với tools/test-doc-examples.js. Sửa luật mà ví dụ nào sai thì sửa cả tài liệu.
    public class MechanicsExampleTests
    {
        private static string[] Rows(params string[] rows) => rows;

        // Vuốt cơ bản

        [Test, Description("S1")]
        public void CopyGoesIntoEmptyNeighbour() =>
            Assert.That(TestLevel.FromRows("A.").Play("0 0 right").Rows, Is.EqualTo(Rows("AA")));

        [Test, Description("S2")]
        public void CopyPushesTheRowToTheNearestEmptyCell() =>
            Assert.That(TestLevel.FromRows("AB.").Play("0 0 right").Rows, Is.EqualTo(Rows("AAB")));

        [Test, Description("S3")]
        public void CopyReplacesNeighbourWhenThereIsNoRoom()
        {
            var play = TestLevel.FromRows("AB", "B.").Play("0 0 right");
            Assert.That(play.Rows, Is.EqualTo(Rows("AA", "B.")));
            Assert.That(play.LastResult.Replaced.HasValue, Is.True);
        }

        [Test, Description("S4")]
        public void CopyNeverReplacesItsOwnColour() =>
            Assert.That(TestLevel.FromRows("AA").Play("0 0 right").LastMoveRefused, Is.True);

        [Test, Description("S5")]
        public void CannotSwipeOffTheBoard() =>
            Assert.That(TestLevel.FromRows("A.").Play("0 0 left").LastMoveRefused, Is.True);

        // Khử 3

        [Test, Description("K1")]
        public void ThreeInARowDisappear() =>
            Assert.That(TestLevel.FromRows("AA.", "A..").Play("0 1 right").Rows, Is.EqualTo(Rows("...", "A..")));

        [Test, Description("K2")]
        public void SleepingCatsCountInARunAndClearBeforeWaking() =>
            Assert.That(TestLevel.FromRows("A.a", "B..").Play("0 0 right").Rows, Is.EqualTo(Rows("...", "B..")));

        [Test, Description("K3")]
        public void ClearingTheLastCatsOfAColourLoses() =>
            Assert.That(TestLevel.FromRows("AA.", "B..").Play("0 1 right").IsLost, Is.True);

        // Thắng / thua

        [Test, Description("W1")]
        public void FullBoardWithAnExtinctColourIsLost()
        {
            var play = TestLevel.FromRows("AB").Play("0 0 right");
            Assert.That(play.Rows, Is.EqualTo(Rows("AA")));
            Assert.That(play.IsLost, Is.True);
            Assert.That(play.IsWon, Is.False);
        }

        [Test, Description("W2")]
        public void SleepingCatsStillCountAsAlive() =>
            Assert.That(GameOutcome.IsLost(TestLevel.FromRows("A.", ".b").Start()), Is.False);

        // Hộp mục tiêu

        [Test, Description("H1")]
        public void SleepingCatOfTheRightColourFillsItsBox() =>
            Assert.That(new TargetBoxGoal().IsMet(TestLevel.FromRows("Ab").WithGoal(".B").Start()), Is.True);

        [Test, Description("H2")]
        public void VirusOnABoxDoesNotFillIt() =>
            Assert.That(new TargetBoxGoal().IsMet(TestLevel.FromRows("A>").WithGoal(".B").Start()), Is.False);

        // Màng một chiều

        [Test, Description("M1")]
        public void CannotSwipeAgainstAMembrane() =>
            Assert.That(TestLevel.FromRows("A.").WithMembrane(0, 1, Direction.Left).Play("0 0 right").LastMoveRefused, Is.True);

        [Test, Description("M2")]
        public void CanSwipeAlongAMembrane() =>
            Assert.That(TestLevel.FromRows(".A").WithMembrane(0, 1, Direction.Left).Play("0 1 left").Rows, Is.EqualTo(Rows("AA")));

        [Test, Description("M3")]
        public void MembraneBlockingThePushTurnsItIntoAReplace()
        {
            var play = TestLevel.FromRows("AB.", "B..").WithMembrane(0, 2, Direction.Left).Play("0 0 right");
            Assert.That(play.Rows, Is.EqualTo(Rows("AA.", "B..")));
            Assert.That(play.LastResult.Replaced.HasValue, Is.True);
        }

        [Test, Description("M4")]
        public void WakingUpIgnoresMembranes() =>
            Assert.That(TestLevel.FromRows(".a.", "A..").WithMembrane(0, 1, Direction.Down).Play("1 0 right").Rows,
                Is.EqualTo(Rows(".A.", "AA.")));

        [Test, Description("M5")]
        public void RunsAcrossAMembraneStillClear() =>
            Assert.That(TestLevel.FromRows("A.A", "B..").WithMembrane(0, 1, Direction.Left).Play("0 2 left").Rows,
                Is.EqualTo(Rows("...", "B..")));

        // Ô độc

        [Test, Description("T1")]
        public void CopyPlacedOnToxicCellDiesAtOnce() =>
            Assert.That(TestLevel.FromRows("A~", "B.").Play("0 0 right").Rows, Is.EqualTo(Rows("A.", "B.")));

        [Test, Description("T2")]
        public void CatPushedOntoToxicCellDies() =>
            Assert.That(TestLevel.FromRows("AB~", "B..").Play("0 0 right").Rows, Is.EqualTo(Rows("AA.", "B..")));

        [Test, Description("T3")]
        public void ToxicCellsNeedNoFilling() =>
            Assert.That(TestLevel.FromRows("A.~").Play("0 0 right").IsWon, Is.True);

        // Mèo ngủ

        [Test, Description("N1")]
        public void CannotSwipeASleepingCat() =>
            Assert.That(TestLevel.FromRows("a.", "B.").Play("0 0 right").LastMoveRefused, Is.True);

        [Test, Description("N2")]
        public void SleepingCatNextToItsColourWakesAtTheStart() =>
            Assert.That(TestLevel.FromRows("Aa").Start().ToRows(), Is.EqualTo(Rows("AA")));

        [Test, Description("N3")]
        public void WakingRunsAlongAChain() =>
            Assert.That(TestLevel.FromRows("B..", ".bb", "..b").Play("0 0 right").Rows, Is.EqualTo(Rows("BB.", ".BB", "..B")));

        [Test, Description("N4")]
        public void SleepingCatIsNeitherPushedNorReplaced()
        {
            Assert.That(TestLevel.FromRows("Ab.", "B..").Play("0 0 right").LastMoveRefused, Is.True);
            Assert.That(TestLevel.FromRows("Ab", "B.").Play("0 0 right").LastMoveRefused, Is.True);
        }

        [Test, Description("N5")]
        public void ThreeSleepingInARowClearBeforeWaking() =>
            Assert.That(TestLevel.FromRows("A..", ".bb", "B..").Play("2 0 up").Rows, Is.EqualTo(Rows("A..", "...", "B..")));

        [Test, Description("N6")]
        public void PushedRowStopsAtASleepingCatLikeAWall()
        {
            var play = TestLevel.FromRows("ABc", "B..").Play("0 0 right");
            Assert.That(play.Rows, Is.EqualTo(Rows("AAc", "B..")));
            Assert.That(play.LastResult.Replaced.Value.Species, Is.EqualTo(Species.Blue));
        }

        // Virus

        [Test, Description("V1")]
        public void VirusGrowsOneCellPerTurnAndOnlyTheHeadOfALineGrows()
        {
            var line = TestLevel.FromRows("A.B.A", ">....");
            Assert.That(line.Play("0 0 right").Rows, Is.EqualTo(Rows("AAB.A", ">>...")));
            var twoTurns = line.Play("0 0 right", "0 2 right");
            Assert.That(twoTurns.Rows, Is.EqualTo(Rows("AABBA", ">>>..")));
            Assert.That(twoTurns.VirusCount, Is.EqualTo(3));
        }

        [Test, Description("V2")]
        public void TwoSourcesGrowTwoCellsAndNeverGrowIntoEachOther()
        {
            var board = TestLevel.FromRows("AB.C", ">..<");
            Assert.That(board.Play("0 1 right").Rows, Is.EqualTo(Rows("ABBC", ">><<")));
            Assert.That(board.Play("0 1 right", "0 3 left").VirusCount, Is.EqualTo(4));
        }

        [Test, Description("V3")]
        public void VirusAtTheEdgeStopsGrowing() =>
            Assert.That(TestLevel.FromRows("A.", ".>").Play("0 0 right").VirusCount, Is.EqualTo(1));

        [Test, Description("V4")]
        public void KillingTheHeadLetsTheNextVirusEatTheSameTurn()
        {
            var play = TestLevel.FromRows("AB", ">>").Play("0 1 down");
            Assert.That(play.LastResult.Replaced.Value.IsVirus, Is.True);
            Assert.That(play.Rows, Is.EqualTo(Rows("AB", ">>")));
            Assert.That(play.LastEvents(TurnEventKind.Eaten).First().Piece.Species, Is.EqualTo(Species.Blue));
        }

        [Test, Description("V5")]
        public void KillingFromTailToHeadClearsTheVirus()
        {
            var play = TestLevel.FromRows("AB", ">>").Play("0 0 down", "0 1 down");
            Assert.That(play.Rows, Is.EqualTo(Rows("AB", "AB")));
            Assert.That(play.IsWon, Is.True);
        }

        [Test, Description("V6")]
        public void VirusGrowingOntoToxicCellDiesButTheSourceStays() =>
            Assert.That(TestLevel.FromRows("A..", ">~.").Play("0 0 right").Rows, Is.EqualTo(Rows("AA.", ">..")));

        [Test, Description("V7")]
        public void MembraneStopsTheVirus() =>
            Assert.That(TestLevel.FromRows("A..", ">..").WithMembrane(1, 1, Direction.Left).Play("0 0 right").VirusCount, Is.EqualTo(1));

        [Test, Description("V8")]
        public void VirusEatingTheLastCatOfAColourLoses() =>
            Assert.That(TestLevel.FromRows("B.", ">A").Play("0 0 right").IsLost, Is.True);

        [Test, Description("V9")]
        public void VirusEatsGerms() =>
            Assert.That(TestLevel.FromRows("A.", ">G").WithKill(Species.Germ).Play("0 0 right").Rows, Is.EqualTo(Rows("AA", ">>")));

        [Test, Description("V10")]
        public void VirusCannotBeSwipedButIsPushedLikeAnyPiece()
        {
            Assert.That(TestLevel.FromRows(">.", "A.").Play("0 0 right").LastMoveRefused, Is.True);
            Assert.That(TestLevel.FromRows("A>.", "...").Play("0 0 right").Rows, Is.EqualTo(Rows("AA>", "...")));
        }

        [Test, Description("V11")]
        public void VirusesNeitherFormRunsNorFillCells()
        {
            var board = TestLevel.FromRows(">>>", "AAB").Start();
            Assert.That(ClearRunsStep.FindRuns(board), Is.Empty);
            Assert.That(new FillGoal().IsMet(board), Is.False);
        }

        // Vi khuẩn

        [Test, Description("G1")]
        public void GermsCanBeSwipedAndMultiply() =>
            Assert.That(TestLevel.FromRows("G.", "A.").WithKill(Species.Germ).Play("0 0 right").Rows, Is.EqualTo(Rows("GG", "A.")));

        [Test, Description("G2")]
        public void ThreeGermsInARowClear() =>
            Assert.That(TestLevel.FromRows("GG.", "A..").WithKill(Species.Germ).Play("0 1 right").Rows, Is.EqualTo(Rows("...", "A..")));

        [Test, Description("G3")]
        public void CopyCanReplaceAGerm() =>
            Assert.That(TestLevel.FromRows("AG", "A.").WithKill(Species.Germ).Play("0 0 right").Rows, Is.EqualTo(Rows("AA", "A.")));

        [Test, Description("G4")]
        public void GermsAreNotAColourButMustBeGoneToWin()
        {
            var board = TestLevel.FromRows("AG").WithKill(Species.Germ).Start();
            Assert.That(GameOutcome.IsLost(board), Is.False);
            Assert.That(GameOutcome.IsComplete(board), Is.False);
        }

        // Booster

        [Test, Description("B1")]
        public void MutateRefusesVirusesAndTheLastCatOfAColour()
        {
            var board = TestLevel.FromRows("A>", "BB").Start();
            Assert.That(Rules.Boosters.TryMutate(board, 1, Species.Red, out _), Is.False);
            Assert.That(Rules.Boosters.TryMutate(board, 0, Species.Blue, out _), Is.False);
            Assert.That(Rules.Boosters.TryMutate(board, 2, Species.Red, out _), Is.True);
        }

        [Test, Description("B2")]
        public void MutateTurnsGermsIntoCatsAndWakesSleepingCats()
        {
            var germs = TestLevel.FromRows("AG", "Bb").WithKill(Species.Germ).Start();
            Assert.That(Rules.Boosters.TryMutate(germs, 1, Species.Red, out var fromGerm), Is.True);
            Assert.That(fromGerm.After[1].Value.Species, Is.EqualTo(Species.Red));

            var sleeping = TestLevel.FromRows("A.", "Bc", "C.").Start();
            Assert.That(Rules.Boosters.TryMutate(sleeping, 3, Species.Red, out var fromSleeping), Is.True);
            Assert.That(fromSleeping.After[3].Value.IsAsleep, Is.False);
        }

        [Test, Description("B3")]
        public void TransplantGrowsACopyInAnyEmptyCellButNotOnToxic()
        {
            var board = TestLevel.FromRows("A.~", "B..").Start();
            Assert.That(Rules.Boosters.TryTransplant(board, 0, 5, out var result), Is.True);
            Assert.That(result.After.ToRows(), Is.EqualTo(Rows("A..", "B.A")));
            Assert.That(Rules.Boosters.TryTransplant(board, 0, 2, out _), Is.False);
        }

        [Test, Description("B4")]
        public void BoostersEndTheTurnSoVirusesStillGrow()
        {
            var board = TestLevel.FromRows("A..", ">..").Start();
            Assert.That(Rules.Boosters.TryTransplant(board, 0, 2, out var result), Is.True);
            Assert.That(result.After.Count(piece => piece.IsVirus), Is.EqualTo(2));
        }
    }
}
