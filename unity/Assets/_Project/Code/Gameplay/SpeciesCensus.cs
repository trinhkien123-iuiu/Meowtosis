using System.Collections.Generic;
using System.Linq;
using Meowtosis.Domain;

namespace Meowtosis.Gameplay
{
    public readonly struct SpeciesCount
    {
        public SpeciesCount(Species species, int count)
        {
            Species = species;
            Count = count;
        }

        public Species Species { get; }

        public int Count { get; }
    }

    // Số mèo còn lại của từng màu có trong level (kể cả mèo ngủ), cho hàng đếm dưới bàn: về 0 là thua.
    public static class SpeciesCensus
    {
        public static IReadOnlyList<SpeciesCount> Of(Board board)
        {
            var colors = new List<Species>(board.Level.StartingColors);
            for (var cell = 0; cell < board.CellCount; cell++)
            {
                if (board[cell] is Piece piece && piece.IsCat)
                {
                    if (!colors.Contains(piece.Species))
                        colors.Add(piece.Species);
                    if (piece.IsTwoColor && piece.OtherColor.HasValue && !colors.Contains(piece.OtherColor.Value))
                        colors.Add(piece.OtherColor.Value);
                }
                var target = board.Level.TargetAt(cell);
                if (target.HasValue && !colors.Contains(target.Value))
                    colors.Add(target.Value);
            }
            foreach (var extinct in GameOutcome.ExtinctColors(board))
            {
                if (!colors.Contains(extinct))
                    colors.Add(extinct);
            }
            return colors.Select(color => new SpeciesCount(color, board.Count(piece => piece.Species == color))).ToList();
        }
    }
}
