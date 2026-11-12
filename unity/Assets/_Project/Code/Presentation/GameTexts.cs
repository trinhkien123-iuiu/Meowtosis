using System;
using Meowtosis.Domain;
using Meowtosis.Gameplay;

namespace Meowtosis.Presentation
{
    // Mọi chữ do code ghép ra cho người chơi, gom một chỗ để sau này dịch. Chữ cố định (tiêu đề popup, tên nút)
    // nằm thẳng trong scene.
    public static class GameTexts
    {
        public const string HeartsFull = "Full";

        public static string LevelBadge(int levelNumber) => $"Lv{levelNumber}";

        // Dạng ngắn cho ô level trên HUD màn chơi, để ô nhỏ gọn không lấn phần chơi.
        public static string LevelShort(int levelNumber) => $"Màn {levelNumber}";

        // Số trên viên mục tiêu: "đã xong/tổng".
        public static string Count(int done, int total) => $"{done}/{total}";

        public static string CoinReward(int coins) => $"+{coins}";

        public static string Cleared(int count) => $"Khử {count}!";

        // Tiêu đề thua ngắn gọn, đập ngay vào mắt người chơi
        public static string Lost(LossReport report)
        {
            if (report == null) return "<b>HẾT SẠCH MÈO!</b>";
            var color = ColorName(report.Color).ToUpper();
            return $"<b>HẾT SẠCH MÈO {color}!</b>";
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

        // Lời giới thiệu trong popup mua booster.
        public static string BoosterInfo(BoosterKind kind) => kind switch
        {
            BoosterKind.Mutate => "Chạm một chú mèo rồi chọn màu mới: phép thuật biến mèo sang màu đó ngay.",
            BoosterKind.Transplant => "Chạm một chú mèo rồi chạm ô trống bất kỳ: bản sao bay tới và mọc ra ở đó.",
            BoosterKind.Hint => "Mèo mách chỉ cho bạn nước vuốt tốt nhất lúc này.",
            _ => "",
        };

        public static string CoinsOwned(int coins) => $"Bạn đang có {coins} xu";
        public static string NotEnoughCoins(int coins) => $"Chưa đủ xu! Bạn mới có {coins} xu";
        public static string BoughtPack(BoosterKind kind, int amount) => $"+{amount} {BoosterName(kind)}!";

        public const string MutatePickCat = "Chạm con mèo muốn đổi màu";
        public const string MutatePickColor = "Chọn màu mới cho mèo";
        public const string TransplantPickSource = "Chạm con mèo để nhân bản";
        public const string TransplantPickTarget = "Chạm ô trống để cấy bản sao";
        public const string NoOtherColor = "Không có màu khác để đổi";
        public const string NotOnVirus = "Booster không dùng được lên virus";
        public const string DefaultAvatar = "Mặc định";
        public const string AvatarTitle = "AVATAR";
        public const string AvatarSave = "Lưu";
        public const string AvatarHint = "Chọn chú mèo đại diện cho bạn";
        public const string NotOnSleeping = "Mèo đang ngủ, booster không dùng được";
        public const string NotOnToxic = "Không cấy vào ô độc được";
        public const string Revived = "Hồi sinh: đã quay lại trước nước thua và trả lại tim";
        public const string FollowTheHand = "Làm theo ngón tay trước nhé";
        public const string SwipeLikeTheHand = "Vuốt theo hướng ngón tay chỉ nhé";
        public const string TutorialDone = ":check: Giỏi lắm! Giờ tự làm tiếp nhé";
        public const string TrapWarnTrietTieu = "Làm vậy sẽ triệt tiêu hết mèo đỏ";
        public const string TrapWarnTrietTieuVang = "Làm vậy sẽ triệt tiêu hết mèo vàng";
        public const string TrapWarnTrietTieuXanh = "Làm vậy sẽ triệt tiêu hết mèo xanh";
        // Bẫy dạy luật "không để mất hết một màu" (level 7). :ten: là hình minh hoạ (Pictograms).
        public const string TrapLost = ":red: :arrow: :cross:|Hết mèo của một màu là thua! Thử cách khác xem nhé";
        public const string TrapKeepOne = ":red: :check:|Mỗi màu phải còn ít nhất 1 con mèo!";
        public const string TrapFillSafely = "Lấp kín mà không để mất màu nào nhé";
        public const string TrapSeen = "Thấy chưa? Hết sạch một màu là thua!";
        public const string NothingChanges = "Không có gì thay đổi";
        public const string Understood = "Đã hiểu!";
        public const string StartPlaying = "Bắt đầu chơi";
        public const string OnboardingDone = "Xuất sắc! Bạn đã nắm được luật!";

        public static string CheatChosenLevel(int level, int levelCount) => $"Lv{level} / {levelCount}";

        public const string CheatNoMove = "Không tìm được nước đi đúng";

        public static string CheatCoins(int total) => $"Đã cộng xu: {total}";

        public const string CheatUnlocked = "Đã mở khoá mọi level";

        public static string BackgroundButton(string lookName) => $"Nền: {lookName}";

        public static string LastOfColor(Species color) => $"Không thể đổi con mèo {ColorName(color)} cuối cùng";

        public static string NeedCoins(int price) => $"Hết lượt. Cần {price} xu để mua thêm";

        public static string Bought(BoosterKind kind) => $"Đã mua thêm 1 lượt {BoosterName(kind)}";

        public static string Refunded(BoosterKind kind) => $"Đã trả lại booster {BoosterName(kind)}";

        // Chữ nổi khi mèo hai màu đổi màu.

        public static string ColorName(Species species) => species switch
        {
            Species.Red => "đỏ",
            Species.Blue => "xanh",
            Species.Yellow => "vàng",
            Species.Pink => "hồng",
            Species.Purple => "tím",
            Species.Green => "xanh lá",
            Species.Lime => "xanh chanh",
            Species.Orange => "cam",
            Species.Teal => "xanh ngọc",
            Species.BabyBlue => "xanh da trời",
            Species.ElectricBlue => "xanh điện",
            Species.WarmPink => "hồng phấn",
            _ => "virus",
        };
    }
}
