using System;
using System.Collections.Generic;

namespace Meowtosis.Domain
{
    public enum SolveStatus
    {
        Solved,
        Unsolvable,     // không có đường thắng (hoặc bàn đã thua sẵn)
        LimitReached,   // dừng vì chạm giới hạn trạng thái hoặc thời gian: bàn có thể vẫn thắng được
    }

    public sealed class SolveResult
    {
        public static readonly SolveResult Unsolvable = new SolveResult(SolveStatus.Unsolvable, Array.Empty<Move>());
        public static readonly SolveResult LimitReached = new SolveResult(SolveStatus.LimitReached, Array.Empty<Move>());

        private SolveResult(SolveStatus status, IReadOnlyList<Move> moves)
        {
            Status = status;
            Moves = moves;
        }

        public SolveStatus Status { get; }

        // Chuỗi nước thắng ngắn nhất khi giải được; nếu không thì rỗng.
        public IReadOnlyList<Move> Moves { get; }

        public bool IsSolved => Status == SolveStatus.Solved;

        public static SolveResult Solved(IReadOnlyList<Move> moves) => new SolveResult(SolveStatus.Solved, moves);
    }
}
