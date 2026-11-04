using System;
using System.Collections.Generic;
using System.Linq;
using Meowtosis.Domain;

namespace Meowtosis.Gameplay
{
    // Một nút trên bản đồ level.
    public sealed class LevelMapEntry
    {
        public LevelMapEntry(int index, int stars, bool isUnlocked, bool isCurrent, bool introducesMechanic)
        {
            Index = index;
            Stars = stars;
            IsUnlocked = isUnlocked;
            IsCurrent = isCurrent;
            IntroducesMechanic = introducesMechanic;
        }

        public int Index { get; }

        // Số sao cao nhất đã đạt, 0 nếu chưa thắng.
        public int Stars { get; }

        public bool IsUnlocked { get; }

        // Level mở xa nhất, nơi người chơi đang tiến tới.
        public bool IsCurrent { get; }

        // Level đầu tiên có một cơ chế mới.
        public bool IntroducesMechanic { get; }
    }

    // Tính bản đồ từ danh sách level và tiến độ: thắng level nào thì mở level kế tiếp, mọi level đã mở đều chơi lại được.
    public static class LevelMap
    {
        public static IReadOnlyList<LevelMapEntry> Build(IReadOnlyList<LevelDefinition> levels, PlayerProgress progress)
        {
            var current = CurrentIndex(levels, progress);
            var seenMechanics = new HashSet<Mechanic>();
            var entries = new List<LevelMapEntry>(levels.Count);
            for (var index = 0; index < levels.Count; index++)
            {
                var mechanics = MechanicDetector.In(levels[index], index);
                var isNew = mechanics.Count(seenMechanics.Add) > 0;
                entries.Add(new LevelMapEntry(index, progress.BestStarsFor(levels[index].Id), index <= current, index == current, isNew));
            }
            return entries;
        }

        // Level ngay sau level đã thắng xa nhất; thắng hết rồi thì là level cuối.
        public static int CurrentIndex(IReadOnlyList<LevelDefinition> levels, PlayerProgress progress)
        {
            var furthestWon = -1;
            for (var index = 0; index < levels.Count; index++)
            {
                if (progress.BestStarsFor(levels[index].Id) > 0) furthestWon = index;
            }
            return Math.Min(furthestWon + 1, levels.Count - 1);
        }
    }
}
