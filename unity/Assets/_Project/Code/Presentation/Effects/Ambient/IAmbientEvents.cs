using Meowtosis.Domain;

namespace Meowtosis.Presentation
{
    // Các sự kiện vui trong màn chơi, nhìn từ phía presenter.
    public interface IAmbientEvents
    {
        // Gọi mỗi khung hình. `isIdle` là người chơi đang rảnh; hết rảnh thì sự kiện đang diễn dừng ngay.
        void Tick(float deltaSeconds, bool isIdle, Board board);
    }
}
