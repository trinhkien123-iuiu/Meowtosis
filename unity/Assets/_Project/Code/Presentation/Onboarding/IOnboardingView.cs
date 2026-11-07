using System;

namespace Meowtosis.Presentation
{
    // Phần giao diện riêng của màn hướng dẫn đầu game, nhìn từ phía presenter.
    public interface IOnboardingView
    {
        // Người chơi bấm nút dưới bàn ("Đã hiểu!" hoặc "Bắt đầu chơi").
        event Action ButtonClicked;

        // Vào hướng dẫn: ẩn các phần HUD chưa cần (tim, sao, nhiệm vụ, booster...), chỉ chừa bàn chơi và bong bóng lời dẫn.
        void Enter();

        // Xong hướng dẫn: trả HUD về như cũ, ẩn nút.
        void Exit();

        void ShowButton(string label);

        void HideButton();
    }
}
