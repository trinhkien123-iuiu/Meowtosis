using System.Collections.Generic;

namespace Meowtosis.Domain
{
    // Một quân bị cú vuốt đẩy đi một ô theo dãy.
    public readonly struct CellShift
    {
        public CellShift(int from, int to)
        {
            From = from;
            To = to;
        }

        public int From { get; }

        public int To { get; }
    }

    // Mọi việc một nước đi đã làm, theo thứ tự, để giao diện diễn lại:
    // đặt mèo mới (đẩy hoặc thay thế), rồi lần lượt từng chặng cuối lượt.
    public sealed class MoveResult
    {
        public MoveResult(Board before, Board placed, int sourceCell, int placedCell,
            IReadOnlyList<CellShift> pushes, Piece? replaced, IReadOnlyList<TurnStage> stages, Direction? swipeDirection = null)
        {
            Before = before;
            Placed = placed;
            SourceCell = sourceCell;
            PlacedCell = placedCell;
            Pushes = pushes;
            Replaced = replaced;
            Stages = stages;
            SwipeDirection = swipeDirection;
        }

        public Board Before { get; }

        // Bàn ngay sau khi đặt mèo mới, trước mọi bước cuối lượt.
        public Board Placed { get; }

        // Con mèo được nhân đôi (với booster: con được dùng booster).
        public int SourceCell { get; }

        // Ô mèo mới xuất hiện.
        public int PlacedCell { get; }

        // Các quân bị đẩy, con xa nhất trước. Rỗng nếu không đẩy gì.
        public IReadOnlyList<CellShift> Pushes { get; }

        // Quân bị mèo mới thay thế, nếu có.
        public Piece? Replaced { get; }

        public IReadOnlyList<TurnStage> Stages { get; }

        // Hướng vuốt; null nếu nước này là booster.
        public Direction? SwipeDirection { get; }

        public Board After => Stages.Count > 0 ? Stages[Stages.Count - 1].Board : Placed;
    }
}
