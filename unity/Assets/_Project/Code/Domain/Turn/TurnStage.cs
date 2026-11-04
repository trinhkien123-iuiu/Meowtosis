using System;
using System.Collections.Generic;

namespace Meowtosis.Domain
{
    // Kết quả một bước cuối lượt: bàn sau bước đó và những việc đã xảy ra, theo thứ tự.
    public sealed class TurnStage
    {
        public TurnStage(Board board, IReadOnlyList<TurnEvent> events)
        {
            Board = board;
            Events = events;
        }

        public Board Board { get; }

        public IReadOnlyList<TurnEvent> Events { get; }

        public static TurnStage Unchanged(Board board) => new TurnStage(board, Array.Empty<TurnEvent>());
    }
}
