using System;
using System.Collections.Generic;
using System.Linq;

namespace Meowtosis.Domain
{
    // Phần cố định của một level: khung bàn, ô độc, hộp mục tiêu, màng và điều kiện thắng.
    // Ô đánh số theo từng hàng từ góc trên trái: cell = row * Width + column.
    public sealed class Level
    {
        public const int NoCell = -1;

        private const int DirectionCount = 4;

        private readonly bool[] isSlot;
        private readonly bool[] isToxic;
        private readonly bool[] isIce;
        private readonly Species?[] targets;
        private readonly int[] nextCells;        // [cell * 4 + direction]: ô mà quân đi tới, hoặc NoCell
        private readonly int[][] neighbours;     // các ô kề trên bàn, bỏ qua màng
        private readonly Piece?[] startPieces;
        private readonly int firstFreePieceId;

        public Level(LevelDefinition definition)
        {
            Id = definition.Id;
            Height = definition.Rows.Count;
            Width = Height == 0 ? 0 : definition.Rows[0].Length;
            CheckGridSize(definition.Rows, "row");
            if (definition.Goal.Count > 0) CheckGridSize(definition.Goal, "goal");

            isSlot = new bool[CellCount];
            isToxic = new bool[CellCount];
            isIce = new bool[CellCount];
            targets = new Species?[CellCount];
            startPieces = new Piece?[CellCount];
            for (var cell = 0; cell < CellCount; cell++)
            {
                var symbol = definition.Rows[RowOf(cell)][ColumnOf(cell)];
                isSlot[cell] = symbol != LevelSymbols.Hole;
                isToxic[cell] = symbol == LevelSymbols.Toxic;
                isIce[cell] = symbol == LevelSymbols.Ice;
                if (LevelSymbols.TryReadPiece(symbol, firstFreePieceId, out var piece))
                {
                    startPieces[cell] = piece;
                    firstFreePieceId++;
                }
                if (definition.Goal.Count > 0 && LevelSymbols.TryReadSpecies(definition.Goal[RowOf(cell)][ColumnOf(cell)], out var target))
                    targets[cell] = target;
            }

            Membranes = definition.Membranes.ToList();
            nextCells = CreateNextCells(Membranes);
            neighbours = CreateNeighbours();
            StartingColors = startPieces
                .Where(piece => piece.HasValue && !piece.Value.IsVirus)
                .Select(piece => piece.Value.Species)
                .Distinct()
                .ToList();
            CellsToFill = Enumerable.Range(0, CellCount).Count(cell => isSlot[cell] && !isToxic[cell]);
            HasTargets = targets.Any(target => target.HasValue);
            Goals = CreateGoals();
        }

        public string Id { get; }

        public int Width { get; }

        public int Height { get; }

        public int CellCount => Width * Height;

        public IReadOnlyList<Membrane> Membranes { get; }

        // Các màu mèo có trên bàn lúc đầu. Mất hết mèo của một màu là thua.
        public IReadOnlyList<Species> StartingColors { get; }

        // Số ô phải lấp kín để thắng (mọi ô trừ ô độc).
        public int CellsToFill { get; }

        public bool HasTargets { get; }

        public IReadOnlyList<ILevelGoal> Goals { get; }

        public int IndexOf(int row, int column) => row * Width + column;

        public int RowOf(int cell) => cell / Width;

        public int ColumnOf(int cell) => cell % Width;

        public bool Contains(int row, int column) => row >= 0 && column >= 0 && row < Height && column < Width;

        // False với lỗ: ô không thuộc bàn.
        public bool IsSlot(int cell) => isSlot[cell];

        public bool IsToxic(int cell) => isToxic[cell];

        // Ô băng trơn: bản sao vào ô này khi nó trống thì trượt tiếp (SwipeRule).
        public bool IsIce(int cell) => isIce[cell];

        // Màu hộp mục tiêu ở ô này, hoặc null.
        public Species? TargetAt(int cell) => targets[cell];

        // Ô mà quân tới được khi đi một bước, hoặc NoCell nếu ra ngoài bàn,
        // rơi vào lỗ hoặc đi ngược chiều màng.
        public int Step(int cell, Direction direction) => nextCells[cell * DirectionCount + (int)direction];

        public IReadOnlyList<int> Neighbours(int cell) => neighbours[cell];

        // Bàn lúc bắt đầu level. Mèo ngủ đang chạm mèo thức cùng màu thì thức dậy luôn.
        public Board CreateStartBoard() => WakeStep.WakeAll(new Board(this, (Piece?[])startPieces.Clone(), firstFreePieceId), null);

        private void CheckGridSize(List<string> rows, string name)
        {
            if (rows.Count == 0) throw new FormatException($"Level '{Id}' không có dòng {name} nào");
            if (rows.Count != Height || rows.Any(row => row.Length != Width))
                throw new FormatException($"Level '{Id}': phải có {Height} dòng {name}, mỗi dòng rộng {Width} ô");
        }

        private int[] CreateNextCells(IReadOnlyList<Membrane> membranes)
        {
            // Màng chỉ cho đi qua theo hướng của nó, nên từ ô bên kia đi ngược lại bị chặn.
            var closedExits = new bool[CellCount * DirectionCount];
            foreach (var membrane in membranes)
            {
                var row = membrane.Row + membrane.Direction.RowOffset();
                var column = membrane.Column + membrane.Direction.ColumnOffset();
                if (!Contains(membrane.Row, membrane.Column) || !Contains(row, column))
                    throw new FormatException($"Level '{Id}': màng ở {membrane.Row} {membrane.Column} chĩa ra ngoài bàn");
                closedExits[IndexOf(row, column) * DirectionCount + (int)membrane.Direction.Opposite()] = true;
            }

            var result = new int[CellCount * DirectionCount];
            for (var cell = 0; cell < CellCount; cell++)
            {
                foreach (var direction in Directions.All)
                {
                    var exit = cell * DirectionCount + (int)direction;
                    var next = AdjacentSlot(cell, direction);
                    result[exit] = closedExits[exit] ? NoCell : next;
                }
            }
            return result;
        }

        private int[][] CreateNeighbours()
        {
            var result = new int[CellCount][];
            for (var cell = 0; cell < CellCount; cell++)
            {
                result[cell] = Directions.All
                    .Select(direction => AdjacentSlot(cell, direction))
                    .Where(next => next != NoCell)
                    .ToArray();
            }
            return result;
        }

        private int AdjacentSlot(int cell, Direction direction)
        {
            var row = RowOf(cell) + direction.RowOffset();
            var column = ColumnOf(cell) + direction.ColumnOffset();
            return Contains(row, column) && isSlot[IndexOf(row, column)] ? IndexOf(row, column) : NoCell;
        }

        private IReadOnlyList<ILevelGoal> CreateGoals()
        {
            var goals = new List<ILevelGoal> { new FillGoal() };
            if (HasTargets) goals.Add(new TargetBoxGoal());
            return goals;
        }
    }
}
