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
        public static IReadOnlyList<SpeciesCount> Of(Board board) =>
            board.Level.StartingColors.Select(color => new SpeciesCount(color, board.Count(piece => piece.Species == color))).ToList();
    }
}
