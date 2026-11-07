using System.IO;
using System.Linq;
using Meowtosis.Domain;
using Meowtosis.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Meowtosis.Tests
{
    // Màn hướng dẫn đầu game (Levels/Onboarding.txt): các bước dạy đúng thứ tự luật, không bước nào làm thua,
    // và lớp Onboarding dẫn người chơi đúng như thiết kế.
    public class OnboardingTests
    {
        private static LevelDefinition Script =>
            LevelCatalogParser.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "_Project", "Levels", "Onboarding.txt"))).Single();

        private static Onboarding Start() => new Onboarding(Script, Rules.Swipes, Rules.Solver);

        private static bool Play(Onboarding onboarding, Move move) =>
            onboarding.TryPlay(onboarding.Session.Level.IndexOf(move.Row, move.Column), move.Direction, out _);

        [Test]
        public void EveryStepHasTextAndRule()
        {
            var steps = Script.Tutorial;
            Assert.That(steps, Is.Not.Empty);
            Assert.That(steps.All(step => step.Text.Length > 0 && step.HasRule), Is.True);
            Assert.That(Script.Tip, Is.Not.Empty, "thiếu lời dặn lúc tự chơi");
        }

        [Test]
        public void StepsTeachDivideThenPushThenReplaceThenClear()
        {
            var board = new Level(Script).CreateStartBoard();
            var lessons = Script.Tutorial.Select(step =>
            {
                Assert.That(Rules.Swipes.TrySwipe(board, step.Move, out var result), Is.True, $"bước {step.Move} bị từ chối");
                board = result.After;
                Assert.That(GameOutcome.IsLost(board), Is.False, $"bước {step.Move} làm thua");
                var cleared = result.Stages.SelectMany(stage => stage.Events).Any(turnEvent => turnEvent.Kind == TurnEventKind.Cleared);
                if (cleared) return Mechanic.Clear;
                if (result.Replaced.HasValue) return Mechanic.Replace;
                return result.Pushes.Count > 0 ? Mechanic.Push : Mechanic.Divide;
            }).ToList();
            Assert.That(lessons, Is.EqualTo(new[] { Mechanic.Divide, Mechanic.Push, Mechanic.Replace, Mechanic.Clear }));
            Assert.That(lessons, Is.EqualTo(Onboarding.TaughtMechanics), "TaughtMechanics phải khớp với các bước thật sự dạy");
        }

        [Test]
        public void StoredSolutionFollowsTheStepsAndWins()
        {
            var script = Script;
            var steps = script.Tutorial.Select(step => step.Move.ToString());
            Assert.That(script.Solution.Take(script.Tutorial.Count).Select(move => move.ToString()), Is.EqualTo(steps));

            var onboarding = Start();
            foreach (var move in script.Solution)
            {
                Assert.That(Play(onboarding, move), Is.True, $"nước {move} bị từ chối");
                onboarding.AcknowledgeRule();
            }
            Assert.That(onboarding.Stage, Is.EqualTo(OnboardingStage.Finished));
            Assert.That(GameOutcome.IsWon(onboarding.Session.Board), Is.True);
        }

        [Test]
        public void GuidedStepRefusesOtherMoves()
        {
            var onboarding = Start();
            var step = onboarding.CurrentStep.Move;
            var otherDirection = step.Direction == Direction.Left ? Direction.Right : Direction.Left;
            Assert.That(Play(onboarding, new Move(step.Row, step.Column, otherDirection)), Is.False);
            Assert.That(onboarding.Session.MovesMade, Is.EqualTo(0));
            Assert.That(onboarding.Stage, Is.EqualTo(OnboardingStage.Guided));
        }

        [Test]
        public void RuleWaitsForUnderstoodBeforeNextStep()
        {
            var onboarding = Start();
            var first = onboarding.CurrentStep;
            Assert.That(Play(onboarding, first.Move), Is.True);
            Assert.That(onboarding.Stage, Is.EqualTo(OnboardingStage.ShowingRule));
            Assert.That(onboarding.Text, Is.EqualTo(first.Rule));

            var second = Script.Tutorial[1].Move;
            Assert.That(Play(onboarding, second), Is.False, "đang hiện câu luật mà vẫn nhận nước");
            onboarding.AcknowledgeRule();
            Assert.That(onboarding.Stage, Is.EqualTo(OnboardingStage.Guided));
            Assert.That(onboarding.Text, Is.EqualTo(Script.Tutorial[1].Text));
        }

        [Test]
        public void FocusCellsCoverPushedRow()
        {
            var onboarding = Start();
            Play(onboarding, Script.Tutorial[0].Move);
            onboarding.AcknowledgeRule();
            // Bước 2: mèo xanh (0,0) vuốt phải, bản sao vào (0,1), mèo đỏ ở (0,1) bị đẩy sang (0,2).
            var level = onboarding.Session.Level;
            Assert.That(onboarding.FocusCells(), Is.EquivalentTo(new[] { level.IndexOf(0, 0), level.IndexOf(0, 1), level.IndexOf(0, 2) }));
        }

        [Test]
        public void FreePartRefusesMoveThatLosesAndSuggestsAWinningOne()
        {
            var onboarding = Start();
            foreach (var step in Script.Tutorial)
            {
                Play(onboarding, step.Move);
                onboarding.AcknowledgeRule();
            }
            Assert.That(onboarding.Stage, Is.EqualTo(OnboardingStage.Free));
            Assert.That(onboarding.Text, Is.EqualTo(Script.Tip));

            // Bàn lúc này: BAA / ... Mèo đỏ (0,1) vuốt trái thì bản sao thay mất bé xanh cuối cùng: hết màu xanh.
            Assert.That(Play(onboarding, Move.Parse("0 1 left")), Is.False, "nhận nước làm hết màu xanh");
            Assert.That(onboarding.Session.MovesMade, Is.EqualTo(Script.Tutorial.Count));

            Assert.That(onboarding.TrySuggest(out var suggestion), Is.True);
            Assert.That(Play(onboarding, suggestion), Is.True);
        }
    }
}
