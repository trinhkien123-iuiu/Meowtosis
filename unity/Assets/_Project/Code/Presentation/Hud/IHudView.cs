using System;
using System.Collections.Generic;
using Meowtosis.Domain;
using Meowtosis.Gameplay;

namespace Meowtosis.Presentation
{
    // HUD của màn chơi nhìn từ phía presenter: hiện số liệu được đưa, báo lại nút bấm.
    public interface IHudView
    {
        event Action SettingsClicked;

        // Nút chơi lại màn trên thanh trên.
        event Action RestartClicked;

        event Action HintClicked;

        event Action<BoosterKind> BoosterClicked;

        event Action<Species> ColorChosen;

        // Chạm nhanh 3 lần vào ô "Lv": mở bảng cheat của bản thử.
        event Action LevelBadgeTripleTapped;

        void ShowLevel(int levelNumber);

        // `timer` là thời gian tới tim kế tiếp, rỗng khi tim đầy.
        void ShowHearts(int hearts, string timer);

        void ShowStars(StarMeter meter, bool instantly);

        // Viên gỗ mục tiêu dưới thanh sao (số ô đã lấp, số hộp đúng màu).
        void ShowGoal(GoalProgress progress, bool instantly);

        // Bong bóng lời thoại: mẹo, hướng dẫn, gợi ý; chuỗi rỗng thì ẩn.
        void ShowTip(string text);

        // Bố trí của level có bước hướng dẫn (như màn hướng dẫn đầu game): ẩn thanh trên và thanh sao,
        // đưa bong bóng lời thoại lên sát đỉnh để không đè lên bàn chơi.
        void SetGuidedLayout(bool guided);

        void ShowSpecies(IReadOnlyList<SpeciesCount> counts);

        // Thanh "đang dùng booster" dưới bàn (icon, tên, lời nhắn, nút huỷ), thay cho hàng booster; None thì ẩn.
        void ShowBoosterPrompt(BoosterKind kind, string prompt);

        // Bấm nút huỷ trên thanh booster.
        event Action BoosterCancelClicked;

        // Thanh sao vừa mất sao (số sao mất); presenter phát tiếng.
        event Action<int> StarLost;

        event Action<int> StarGained;

        void ShowBooster(BoosterKind kind, int count, bool selected);

        void SetHintEnabled(bool enabled);

        // Thông báo ngắn tự tắt.
        void ShowToast(string text);

        // Bảng chọn màu cho Hoán đổi; danh sách rỗng thì ẩn.
        void ShowColorChoices(IReadOnlyList<Species> colors, UnityEngine.Vector2 screenPoint);
    }
}
