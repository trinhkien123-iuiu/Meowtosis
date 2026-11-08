using System;
using System.Collections.Generic;
using Meowtosis.Domain;

namespace Meowtosis.Gameplay
{
    public enum OnboardingStage
    {
        // Làm theo bàn tay: chỉ nhận đúng nước được chỉ.
        Guided,

        // Vừa đi xong một bước có câu luật: hiện câu luật, chờ bấm "Đã hiểu!".
        ShowingRule,

        // Tự lấp nốt ổ: nhận mọi nước còn đường thắng.
        Free,

        // Đã kín ổ.
        Finished,
    }

    // Màn hướng dẫn đầu game (Levels/Onboarding.txt): một bàn duy nhất dạy hết luật cơ bản.
    // Người chơi làm theo lần lượt các bước `tutorial`; bước nào có `rule` thì đi xong hiện câu luật đó.
    // Hết các bước thì tự chơi nốt tới khi kín ổ (lời dặn là dòng `tip`). Phần tự chơi từ chối nước làm mất đường thắng,
    // nên người mới không thể thua ở đây.
    public sealed class Onboarding
    {
        // Các cơ chế màn hướng dẫn dạy, theo đúng thứ tự (OnboardingTests kiểm tra). Xong hướng dẫn thì coi như đã biết.
        public static readonly IReadOnlyList<Mechanic> TaughtMechanics = new[] { Mechanic.Divide, Mechanic.Push, Mechanic.Replace, Mechanic.Clear };

        private static readonly TimeSpan SolveTimeLimit = TimeSpan.FromMilliseconds(300);

        private readonly SwipeRule swipeRule;
        private readonly Solver solver;
        private int stepIndex;
        private int ruleIndex;   // câu luật đang hiện của bước hiện tại (một bước có thể có nhiều câu luật nối nhau)

        public Onboarding(LevelDefinition definition, SwipeRule swipeRule, Solver solver)
        {
            if (definition.Tutorial.Count == 0)
                throw new ArgumentException("Màn hướng dẫn cần ít nhất một bước 'tutorial'", nameof(definition));
            this.swipeRule = swipeRule;
            this.solver = solver;
            Session = new GameSession(definition);
        }

        public GameSession Session { get; }

        public OnboardingStage Stage { get; private set; } = OnboardingStage.Guided;

        // Bước đang làm theo (Guided) hoặc vừa làm xong (ShowingRule); null khi đã qua hết các bước.
        public TutorialStep CurrentStep => stepIndex < Steps.Count ? Steps[stepIndex] : null;

        // Chữ trong bong bóng: lời dẫn của bước, câu luật vừa thấy, hoặc lời dặn lúc tự chơi.
        public string Text => Stage switch
        {
            OnboardingStage.Guided => CurrentStep.Text,
            OnboardingStage.ShowingRule => CurrentStep.Rules[ruleIndex],
            OnboardingStage.Free => Session.Definition.Tip,
            _ => "",
        };

        private IReadOnlyList<TutorialStep> Steps => Session.Definition.Tutorial;

        // Các ô nước hướng dẫn hiện tại sẽ chạm tới: ô mèo được vuốt và mọi ô đổi khác trong nước đó (bản sao, dãy bị đẩy,
        // mèo bị thay, dãy 3 bị khử). Màn hình làm tối phần còn lại. Rỗng nếu không ở bước hướng dẫn.
        public IReadOnlyList<int> FocusCells()
        {
            if (Stage != OnboardingStage.Guided || !swipeRule.TrySwipe(Session.Board, CurrentStep.Move, out var preview))
                return Array.Empty<int>();
            return MoveFocus.CellsOf(preview, Session.Level);
        }

        // Nước nên đi lúc này: nước của bước hướng dẫn, hoặc lúc tự chơi là nước đầu của một lời giải từ thế cờ hiện tại.
        public bool TrySuggest(out Move move)
        {
            move = default;
            if (Stage == OnboardingStage.Guided)
            {
                move = CurrentStep.Move;
                return true;
            }
            if (Stage != OnboardingStage.Free) return false;
            var solved = solver.Solve(Session.Board, Solver.DefaultMaxStates, SolveTimeLimit);
            if (!solved.IsSolved || solved.Moves.Count == 0) return false;
            move = solved.Moves[0];
            return true;
        }

        // Đi một nước vuốt. Từ chối khi: sai nước hướng dẫn, đang hiện câu luật, nước làm mất đường thắng, hoặc đã xong.
        public bool TryPlay(int cell, Direction direction, out MoveResult result)
        {
            result = null;
            switch (Stage)
            {
                case OnboardingStage.Guided:
                    var expected = CurrentStep.Move;
                    if (Session.Level.IndexOf(expected.Row, expected.Column) != cell || expected.Direction != direction) return false;
                    if (!Session.TryPlay(new SwipeCommand(swipeRule, cell, direction), out result)) return false;
                    if (CurrentStep.HasRule) Stage = OnboardingStage.ShowingRule;
                    else NextStep();
                    return true;

                case OnboardingStage.Free:
                    if (!swipeRule.TrySwipe(Session.Board, cell, direction, out var preview) || !KeepsWinPossible(preview.After)) return false;
                    if (!Session.TryPlay(new SwipeCommand(swipeRule, cell, direction), out result)) return false;
                    if (Session.Status == SessionStatus.Won) Stage = OnboardingStage.Finished;
                    return true;

                default:
                    return false;
            }
        }

        // Người chơi bấm "Đã hiểu!": sang câu luật kế của bước, hết câu thì sang bước kế tiếp.
        public void AcknowledgeRule()
        {
            if (Stage != OnboardingStage.ShowingRule) return;
            if (ruleIndex + 1 < CurrentStep.Rules.Count) ruleIndex++;
            else NextStep();
        }

        private void NextStep()
        {
            ruleIndex = 0;
            stepIndex++;
            if (Session.Status == SessionStatus.Won) Stage = OnboardingStage.Finished;
            else Stage = stepIndex < Steps.Count ? OnboardingStage.Guided : OnboardingStage.Free;
        }

        private bool KeepsWinPossible(Board board)
        {
            if (GameOutcome.IsLost(board)) return false;
            if (GameOutcome.IsWon(board)) return true;
            return solver.Solve(board, Solver.DefaultMaxStates, SolveTimeLimit).IsSolved;
        }
    }
}
