using Meowtosis.Domain;

namespace Meowtosis.Gameplay
{
    // Bộ luật chuẩn nối sẵn, dùng chung cho game và test.
    public sealed class GameRules
    {
        private GameRules(TurnResolver turns)
        {
            Swipes = new SwipeRule(turns);
            Boosters = new BoosterRules(turns);
            Solver = new Solver(Swipes);
            Hints = new HintAdvisor(Swipes, Solver);
        }

        public SwipeRule Swipes { get; }

        public BoosterRules Boosters { get; }

        public Solver Solver { get; }

        public HintAdvisor Hints { get; }

        public static GameRules CreateStandard() => new GameRules(TurnResolver.CreateStandard());
    }
}
