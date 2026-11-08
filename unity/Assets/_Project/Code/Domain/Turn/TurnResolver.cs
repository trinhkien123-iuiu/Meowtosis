using System.Collections.Generic;

namespace Meowtosis.Domain
{
    // Kết thúc lượt bằng cách chạy lần lượt mọi bước cuối lượt.
    public sealed class TurnResolver
    {
        private readonly IReadOnlyList<ITurnStep> steps;

        public TurnResolver(IReadOnlyList<ITurnStep> steps)
        {
            this.steps = steps;
        }

        // Thứ tự này là một phần của luật (docs/mechanics.md). Đổi thứ tự là đổi lời giải của các level đang có.
        public static TurnResolver CreateStandard() => new TurnResolver(new ITurnStep[]
        {
            new PoisonStep(),
            new ColorSwitchStep(),
            new ClearRunsStep(),
            new VirusSpreadStep(),
            new PoisonStep(),
            new WakeStep(),
        });

        public IReadOnlyList<TurnStage> Resolve(Board placed)
        {
            var stages = new List<TurnStage>(steps.Count);
            var board = placed;
            foreach (var step in steps)
            {
                var stage = step.Apply(board);
                stages.Add(stage);
                board = stage.Board;
            }
            return stages;
        }
    }
}
