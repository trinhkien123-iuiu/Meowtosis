using System;
using System.Collections.Generic;

namespace Meowtosis.Domain
{
    // Quản lý bảng màu mèo cho các level. Cứ 2-3 level đổi màu/bộ mèo một lần từ 12 loài mèo có sẵn trong Art:
    // Red, Blue, Yellow, Pink, Purple, Green, Lime, Orange, Teal, BabyBlue, ElectricBlue, WarmPink.
    public static class LevelPalettes
    {
        public static readonly IReadOnlyList<Species> Classic = new[]
        {
            Species.Red, Species.Blue, Species.Yellow, Species.Pink, Species.Purple,
        };

        // Các bộ màu luân phiên phối hợp tương phản và bắt mắt:
        // Cứ 2-3 level áp dụng một bộ màu mới để người chơi luôn có cảm giác mới mẻ.
        public static readonly Species[][] Rotations = new[]
        {
            // Nhóm 1: Xanh lá & Cam
            new[] { Species.Green, Species.Orange, Species.Yellow, Species.Purple, Species.Pink },
            // Nhóm 2: Xanh ngọc & Hồng phấn
            new[] { Species.Teal, Species.WarmPink, Species.Yellow, Species.Blue, Species.Lime },
            // Nhóm 3: Xanh chanh & Tím đậm
            new[] { Species.Lime, Species.Purple, Species.Orange, Species.BabyBlue, Species.Red },
            // Nhóm 4: Xanh da trời pastel & Cam
            new[] { Species.BabyBlue, Species.Orange, Species.Green, Species.WarmPink, Species.Yellow },
            // Nhóm 5: Xanh điện quang & Vàng tươi
            new[] { Species.ElectricBlue, Species.Yellow, Species.WarmPink, Species.Green, Species.Purple },
            // Nhóm 6: Xanh lá & Hồng phấn
            new[] { Species.Green, Species.WarmPink, Species.Teal, Species.Orange, Species.BabyBlue },
            // Nhóm 7: Xanh ngọc & Vàng & Tím (3 màu)
            new[] { Species.Teal, Species.Yellow, Species.Purple, Species.Lime, Species.WarmPink },
            // Nhóm 8: Xanh chanh & Cam & Xanh da trời
            new[] { Species.Lime, Species.Orange, Species.BabyBlue, Species.Purple, Species.Red },
            // Nhóm 9: Xanh điện quang & Hồng phấn & Xanh ngọc
            new[] { Species.ElectricBlue, Species.WarmPink, Species.Teal, Species.Yellow, Species.Green },
            // Nhóm 10: Xanh lá & Tím & Cam
            new[] { Species.Green, Species.Purple, Species.Orange, Species.BabyBlue, Species.WarmPink },
            // Nhóm 11: Xanh da trời & Hồng phấn & Vàng
            new[] { Species.BabyBlue, Species.WarmPink, Species.Yellow, Species.Lime, Species.ElectricBlue },
            // Nhóm 12: Xanh chanh & Xanh điện quang & Cam
            new[] { Species.Lime, Species.ElectricBlue, Species.Orange, Species.Purple, Species.Green },
        };
    }
}
