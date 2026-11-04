using System;
using System.Collections.Generic;
using Meowtosis.Domain;
using Meowtosis.Gameplay;

namespace Meowtosis.Presentation
{
    // Mọi chữ do code ghép ra cho người chơi, gom một chỗ để sau này dịch. Chữ cố định (tiêu đề popup, tên nút)
    // nằm thẳng trong scene.
    public static class GameTexts
    {
        public const string HeartsFull = "Đầy";

        public static string LevelBadge(int levelNumber) => $"Level {levelNumber}";

        // Dạng ngắn cho ô level trên HUD màn chơi, để ô nhỏ gọn không lấn phần chơi.
        public static string LevelShort(int levelNumber) => $"Lv{levelNumber}";

        public static string Moves(int movesMade, int par) => $"{movesMade} nước · tối ưu {par}";

        public static string Goal(GoalProgress progress)
        {
            var parts = new List<string> { $"Lấp kín {progress.FilledCells}/{progress.CellsToFill}" };
            if (progress.BoxCount > 0) parts.Add($"Hộp {progress.BoxesDone}/{progress.BoxCount}");
            if (progress.KillRemaining > 0) parts.Add($"Vi khuẩn còn {progress.KillRemaining}");
            return string.Join("  ·  ", parts);
        }

        public static string CoinReward(int coins) => $"+{coins}";

        public static string Cleared(int count) => $"Khử {count}!";

        // Nói rõ vì sao thua, để lần sau người chơi biết tránh.
        public static string Lost(LossReport report)
        {
            var color = ColorName(report.Color);
            return report.Cause switch
            {
                LossCause.Cleared => $"{report.Count} mèo {color} thẳng hàng nên bị khử!",
                LossCause.Replaced => $"Mèo {color} cuối cùng đã bị thay thế!",
                LossCause.Poisoned => $"Mèo {color} cuối cùng đã rơi vào ô độc!",
                LossCause.Eaten => $"Virus đã ăn mất mèo {color} cuối cùng!",
                _ => $"Hết sạch mèo {color}!",
            } + "\nMàu nào cũng phải còn ít nhất 1 con.";
        }

        // "m:ss" tới tim kế tiếp, rỗng khi tim đầy.
        public static string HeartTimer(TimeSpan? timeLeft)
        {
            if (!timeLeft.HasValue) return "";
            var seconds = Math.Max(0, (int)Math.Ceiling(timeLeft.Value.TotalSeconds));
            return $"{seconds / 60}:{seconds % 60:00}";
        }

        public static string NextHeart(string timer) => $"Tim mới sau {timer}";

        public static string Hint(Hint hint) => hint.Kind switch
        {
            HintKind.Move => $"Thử vuốt theo mũi tên · còn ít nhất {hint.MovesLeft} lần vuốt",
            HintKind.Undo => "Đi lệch đường thắng rồi, bấm Cài đặt rồi Chơi lại nhé.",
            HintKind.NotWinnable => "Từ đây không thắng được nữa, hãy chơi lại.",
            _ => "Thế cờ này rối quá, chưa tìm ra gợi ý. Thử đi một nước khác rồi bấm lại nhé.",
        };

        public static string BoosterName(BoosterKind kind) => kind switch
        {
            BoosterKind.Mutate => "Hoán đổi",
            BoosterKind.Transplant => "Nhân bản",
            BoosterKind.Revive => "Hồi sinh",
            BoosterKind.Hint => "Mèo mách",
            _ => "",
        };

        public const string MutatePickCat = "Chạm con mèo muốn đổi màu";
        public const string MutatePickColor = "Chọn màu mới cho mèo";
        public const string TransplantPickSource = "Chạm con mèo để nhân bản";
        public const string TransplantPickTarget = "Chạm ô trống để cấy bản sao";
        public const string NoOtherColor = "Không có màu khác để đổi";
        public const string NotOnVirus = "Booster không dùng được lên virus";
        public const string NotOnToxic = "Không cấy vào ô độc được";
        public const string Revived = "Hồi sinh: đã quay lại trước nước thua và trả lại tim";
        public const string FollowTheHand = "Làm theo ngón tay trước nhé";
        public const string SwipeLikeTheHand = "Vuốt theo hướng ngón tay chỉ nhé";
        public const string TutorialDone = "Giỏi lắm! Giờ tự làm tiếp nhé";
        public const string TrapSeen = "Thấy chưa? Hết sạch một màu là thua!";
        public const string NothingChanges = "Không có gì thay đổi";

        public static string LastOfColor(Species color) => $"Không thể đổi con mèo {ColorName(color)} cuối cùng";

        public static string NeedCoins(int price) => $"Hết lượt. Cần {price} xu để mua thêm";

        public static string Bought(BoosterKind kind) => $"Đã mua thêm 1 lượt {BoosterName(kind)}";

        public static string Refunded(BoosterKind kind) => $"Đã trả lại booster {BoosterName(kind)}";

        public static string ColorName(Species species) => species switch
        {
            Species.Red => "đỏ",
            Species.Blue => "xanh",
            Species.Yellow => "vàng",
            Species.Pink => "hồng",
            Species.Purple => "tím",
            Species.Germ => "vi khuẩn",
            _ => "virus",
        };
    }
}
