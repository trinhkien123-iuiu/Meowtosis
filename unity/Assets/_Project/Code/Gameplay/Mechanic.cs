using System;
using System.Collections.Generic;
using System.Linq;
using Meowtosis.Domain;

namespace Meowtosis.Gameplay
{
    // Các cơ chế được giới thiệu bằng thẻ hướng dẫn. Số của từng giá trị đã lưu trong Data/Mechanics/*.asset: không đổi số.
    // Số 9 (Kill) và 12 (GermClear) từng là vi khuẩn, đã bỏ ngày 2026-11-07: đừng dùng lại.
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
        Virus = 8,
        ClearForRoom = 10,
        ChainWake = 11,
        Ice = 13,
        Portal = 14,
        TwoColor = 15,
    }

    // Level nào có cơ chế gì, theo nội dung level. Bốn luật cơ bản (nhân đôi lấp kín, khử 3, đẩy, thay thế) không dò ra từ
    // bàn: chúng được dạy lần lượt ở 15 level đầu, level mở đầu mỗi luật khai thẻ của luật đó (card) để bản đồ gắn nhãn "mới".
    public static class MechanicDetector
    {
        // Như In(Level), thêm thẻ riêng mà level khai trong danh sách level.
        public static IReadOnlyList<Mechanic> In(LevelDefinition definition)
        {
            var found = new List<Mechanic>(In(new Level(definition)));
            if (definition.Card.Length == 0) return found;
            if (!Enum.TryParse<Mechanic>(definition.Card, out var card)) throw new FormatException($"Không rõ thẻ cơ chế '{definition.Card}'");
            found.Add(card);
            return found;
        }

        public static IReadOnlyList<Mechanic> In(Level level)
        {
            var start = level.CreateStartBoard();
            var found = new List<Mechanic>();
            if (level.HasTargets) found.Add(Mechanic.TargetBox);
            if (level.Membranes.Count > 0) found.Add(Mechanic.Membrane);
            if (Enumerable.Range(0, level.CellCount).Any(level.IsToxic)) found.Add(Mechanic.Toxic);
            if (start.Count(piece => piece.IsAsleep) > 0) found.Add(Mechanic.Sleep);
            if (start.Count(piece => piece.IsVirus) > 0) found.Add(Mechanic.Virus);
            if (Enumerable.Range(0, level.CellCount).Any(level.IsIce)) found.Add(Mechanic.Ice);
            if (level.Portals.Count > 0) found.Add(Mechanic.Portal);
            if (level.ColorSwitches.Count > 0) found.Add(Mechanic.TwoColor);
            return found;
        }
    }
}
