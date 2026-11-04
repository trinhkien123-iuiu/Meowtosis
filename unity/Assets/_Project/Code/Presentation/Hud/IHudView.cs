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

        event Action HintClicked;

        event Action<BoosterKind> BoosterClicked;

        event Action<Species> ColorChosen;

        void ShowLevel(int levelNumber);

        // `timer` là thời gian tới tim kế tiếp, rỗng khi tim đầy.
        void ShowHearts(int hearts, string timer);

        void ShowStars(StarMeter meter, string movesText, bool instantly);

        void ShowGoal(string text);

        // Bong bóng lời thoại: mẹo, hướng dẫn, gợi ý; chuỗi rỗng thì ẩn.
        void ShowTip(string text);

        void ShowSpecies(IReadOnlyList<SpeciesCount> counts);

        void ShowBooster(BoosterKind kind, int count, bool selected);

        void SetHintEnabled(bool enabled);

        // Thông báo ngắn tự tắt.
        void ShowToast(string text);

        // Bảng chọn màu cho Hoán đổi; danh sách rỗng thì ẩn.
        void ShowColorChoices(IReadOnlyList<Species> colors);
    }
}
