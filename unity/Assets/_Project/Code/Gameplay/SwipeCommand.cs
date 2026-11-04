using Meowtosis.Domain;

namespace Meowtosis.Gameplay
{
    // Vuốt con mèo ở `cell` theo `direction`.
    public sealed class SwipeCommand : IGameCommand
    {
        private readonly SwipeRule swipeRule;
        private readonly int cell;
        private readonly Direction direction;

        public SwipeCommand(SwipeRule swipeRule, int cell, Direction direction)
        {
            this.swipeRule = swipeRule;
            this.cell = cell;
            this.direction = direction;
        }

        public BoosterKind Booster => BoosterKind.None;

        public bool TryApply(Board board, out MoveResult result) => swipeRule.TrySwipe(board, cell, direction, out result);
    }

    // Booster Hoán đổi: đổi con ở `cell` sang màu `color`.
    public sealed class MutateCommand : IGameCommand
    {
        private readonly BoosterRules boosterRules;
        private readonly int cell;
        private readonly Species color;

        public MutateCommand(BoosterRules boosterRules, int cell, Species color)
        {
            this.boosterRules = boosterRules;
            this.cell = cell;
            this.color = color;
        }

        public BoosterKind Booster => BoosterKind.Mutate;

        public bool TryApply(Board board, out MoveResult result) => boosterRules.TryMutate(board, cell, color, out result);
    }

    // Booster Nhân bản: bản sao của con ở `from` mọc ở ô trống `to`.
    public sealed class TransplantCommand : IGameCommand
    {
        private readonly BoosterRules boosterRules;
        private readonly int from;
        private readonly int to;

        public TransplantCommand(BoosterRules boosterRules, int from, int to)
        {
            this.boosterRules = boosterRules;
            this.from = from;
            this.to = to;
        }

        public BoosterKind Booster => BoosterKind.Transplant;

        public bool TryApply(Board board, out MoveResult result) => boosterRules.TryTransplant(board, from, to, out result);
    }
}
