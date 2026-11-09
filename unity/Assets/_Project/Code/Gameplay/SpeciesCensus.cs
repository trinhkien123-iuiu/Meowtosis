using System.Collections.Generic;
using System.Linq;
using Meowtosis.Domain;

namespace Meowtosis.Gameplay
{
    public readonly struct SpeciesCount
    {
        public SpeciesCount(Species species, int count, bool allAsleep = false)
        {
            Species = species;
            Count = count;
            AllAsleep = allAsleep;
        }

        public Species Species { get; }

        public int Count { get; }

        // Còn mèo màu này nhưng con nào cũng đang ngủ: màu này không vuốt được, cũng không thể mất.
        public bool AllAsleep { get; }
    }

    // Số mèo còn lại của từng màu có trong level (kể cả mèo ngủ), cho hàng đếm dưới bàn: về 0 là thua.
    public static class SpeciesCensus
    {
        public static IReadOnlyList<SpeciesCount> Of(Board board) =>
            board.Level.StartingColors.Select(color =>
            {
                var count = board.Count(piece => piece.Species == color);
                var awake = board.Count(piece => piece.Species == color && !piece.IsAsleep);
                return new SpeciesCount(color, count, count > 0 && awake == 0);
            }).ToList();
    }
}
