using System;

namespace Meowtosis.Presentation
{
    // Thứ tự trùng với các trang và các tab trong scene: Home nằm giữa.
    public enum HomeTab
    {
        Shop,
        Home,
        Settings,
    }

    // Màn Home nhìn từ phía presenter: hiện số liệu được đưa, báo lại nút bấm.
    public interface IHomeView
    {
        event Action PlayClicked;

        event Action<HomeTab> TabClicked;

        event Action SoundToggled;

        event Action MusicToggled;

        void ShowCoins(int coins);

        // `status` là thời gian tới tim kế tiếp, hoặc chữ báo tim đã đầy.
        void ShowHearts(int hearts, string status);

        void ShowLevel(int levelNumber);

        void ShowTab(HomeTab tab, bool animate);

        // Tắt vuốt chuyển trang khi có màn khác phủ lên (bản đồ level).
        void SetSwipeEnabled(bool isEnabled);

        void ShowAudio(bool soundOn, bool musicOn);
    }
}
