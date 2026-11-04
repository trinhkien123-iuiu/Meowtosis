using System;
using System.Collections.Generic;
using System.Linq;
using Meowtosis.Domain;

namespace Meowtosis.Gameplay
{
    // Các cơ chế được giới thiệu bằng thẻ hướng dẫn. Số của từng giá trị đã lưu trong Data/Mechanics/*.asset: không đổi thứ tự.
    public enum Mechanic
    {
        Divide,
        Push,
        Replace,
        Clear,
        TargetBox,
        Membrane,
        Toxic,
        Sleep,
        Virus,
        Kill,
        ClearForRoom,
        ChainWake,
        GermClear,
    }

    // Level nào có cơ chế gì: bốn cơ chế cơ bản theo thứ tự level, đúng level đầu tiên thật sự dùng đến chúng;
    // còn lại theo nội dung level.
    public static class MechanicDetector
    {
        private const int FirstClearLevel = 1;
        private const int FirstPushLevel = 3;
        private const int FirstReplaceLevel = 5;

        // Như In(Level, int), thêm thẻ riêng mà level khai trong danh sách level.
        public static IReadOnlyList<Mechanic> In(LevelDefinition definition, int levelIndex)
        {
            var found = new List<Mechanic>(In(new Level(definition), levelIndex));
            if (definition.Card.Length == 0) return found;
            if (!Enum.TryParse<Mechanic>(definition.Card, out var card)) throw new FormatException($"Không rõ thẻ cơ chế '{definition.Card}'");
            found.Add(card);
            return found;
        }

        public static IReadOnlyList<Mechanic> In(Level level, int levelIndex)
        {
            var start = level.CreateStartBoard();
            var found = new List<Mechanic> { Mechanic.Divide };
            if (levelIndex >= FirstClearLevel) found.Add(Mechanic.Clear);
            if (levelIndex >= FirstPushLevel) found.Add(Mechanic.Push);
            if (levelIndex >= FirstReplaceLevel) found.Add(Mechanic.Replace);
            if (level.HasTargets) found.Add(Mechanic.TargetBox);
            if (level.Membranes.Count > 0) found.Add(Mechanic.Membrane);
            if (Enumerable.Range(0, level.CellCount).Any(level.IsToxic)) found.Add(Mechanic.Toxic);
            if (start.Count(piece => piece.IsAsleep) > 0) found.Add(Mechanic.Sleep);
            if (start.Count(piece => piece.IsVirus) > 0) found.Add(Mechanic.Virus);
            if (level.KillSpecies.HasValue) found.Add(Mechanic.Kill);
            return found;
        }
    }
}
