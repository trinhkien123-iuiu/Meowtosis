namespace Meowtosis.Presentation
{
    // Chuyển giữa các màn của game; presenter không cần biết tên scene.
    public interface ISceneNavigator
    {
        void OpenHome();

        // Về màn Home và mở sẵn bản đồ level.
        void OpenMap();

        // Có yêu cầu mở bản đồ đang chờ không; đọc xong thì xoá.
        bool TakeMapRequest();

        void OpenGame();
    }
}
