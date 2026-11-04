using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Meowtosis.Domain
{
    // Tìm chuỗi vuốt thắng ngắn nhất bằng A*. Dùng cho Gợi ý và để kiểm tra level.
    // Ước lượng là số ô còn phải lấp. Mỗi nước chỉ thêm tối đa một mèo hoặc vi khuẩn,
    // nên ước lượng không bao giờ vượt số nước còn lại và lời giải tìm thấy đầu tiên là ngắn nhất.
    // Các thế cờ cùng tổng ước lượng được lấy cái mới nhất trước, y như prototype,
    // nên hai bên ra cùng lời giải trong cùng giới hạn.
    public sealed class Solver
    {
        public const int DefaultMaxStates = 80000;

        private readonly SwipeRule swipeRule;

        public Solver(SwipeRule swipeRule)
        {
            this.swipeRule = swipeRule;
        }

        public SolveResult Solve(Board start, int maxStates = DefaultMaxStates, TimeSpan? timeLimit = null)
        {
            if (GameOutcome.IsLost(start)) return SolveResult.Unsolvable;
            if (GameOutcome.IsComplete(start)) return SolveResult.Solved(Array.Empty<Move>());

            var clock = Stopwatch.StartNew();
            var level = start.Level;
            var startKey = start.ToKey();
            var nodes = new Dictionary<string, SearchNode> { [startKey] = new SearchNode(start, 0, null, default) };
            var buckets = new List<Stack<string>>();   // buckets[f]: các thế cờ có số nước đã đi cộng ước lượng bằng f
            AddToBucket(buckets, Estimate(start), startKey);
            var expanded = 0;
            var limitReached = false;

            for (var total = Estimate(start); total < buckets.Count; total++)
            {
                var bucket = buckets[total];
                while (bucket != null && bucket.Count > 0)
                {
                    var key = bucket.Pop();
                    var node = nodes[key];
                    if (node.IsExpanded || node.Cost + Estimate(node.Board) != total) continue;   // đã xét, hoặc đã tới được bằng đường ngắn hơn
                    node.IsExpanded = true;

                    if (++expanded % 128 == 0 && timeLimit.HasValue && clock.Elapsed > timeLimit.Value) return SolveResult.LimitReached;
                    if (GameOutcome.IsComplete(node.Board)) return SolveResult.Solved(PathTo(nodes, key));

                    foreach (var (cell, direction) in SwipeRule.Candidates(node.Board))
                    {
                        if (!swipeRule.TrySwipe(node.Board, cell, direction, out var result)) continue;
                        var child = result.After;
                        if (GameOutcome.IsLost(child)) continue;

                        var childKey = child.ToKey();
                        var childCost = node.Cost + 1;
                        var known = nodes.TryGetValue(childKey, out var existing);
                        if (!known && nodes.Count >= maxStates)
                        {
                            limitReached = true;
                            continue;
                        }
                        if (known && existing.Cost <= childCost) continue;

                        var move = new Move(level.RowOf(cell), level.ColumnOf(cell), direction);
                        nodes[childKey] = new SearchNode(child, childCost, key, move);
                        AddToBucket(buckets, childCost + Estimate(child), childKey);
                    }
                }
            }
            return limitReached ? SolveResult.LimitReached : SolveResult.Unsolvable;
        }

        private static int Estimate(Board board)
        {
            var kill = board.Level.KillSpecies;
            return board.Level.CellsToFill - board.Count(piece => !piece.IsVirus && piece.Species != kill);
        }

        private static void AddToBucket(List<Stack<string>> buckets, int total, string key)
        {
            while (buckets.Count <= total) buckets.Add(null);
            if (buckets[total] == null) buckets[total] = new Stack<string>();
            buckets[total].Push(key);
        }

        private static IReadOnlyList<Move> PathTo(Dictionary<string, SearchNode> nodes, string key)
        {
            var moves = new List<Move>();
            for (var node = nodes[key]; node.ParentKey != null; node = nodes[node.ParentKey]) moves.Add(node.Move);
            moves.Reverse();
            return moves;
        }

        private sealed class SearchNode
        {
            public SearchNode(Board board, int cost, string parentKey, Move move)
            {
                Board = board;
                Cost = cost;
                ParentKey = parentKey;
                Move = move;
            }

            public Board Board { get; }

            // Số nước đã đi từ đầu để tới bàn này.
            public int Cost { get; }

            // Bàn đi tới bàn này (null nếu là bàn đầu) và nước đã đi.
            public string ParentKey { get; }

            public Move Move { get; }

            public bool IsExpanded { get; set; }
        }
    }
}
