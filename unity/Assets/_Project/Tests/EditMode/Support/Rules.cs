using Meowtosis.Domain;

namespace Meowtosis.Tests
{
    // Bộ luật chuẩn, nối giống hệt cách game nối.
    internal static class Rules
    {
        public static readonly TurnResolver Turns = TurnResolver.CreateStandard();
        public static readonly SwipeRule Swipes = new SwipeRule(Turns);
        public static readonly BoosterRules Boosters = new BoosterRules(Turns);
        public static readonly Solver Solver = new Solver(Swipes);
    }
}
