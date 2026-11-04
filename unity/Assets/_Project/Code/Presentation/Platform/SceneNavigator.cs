using UnityEngine.SceneManagement;

namespace Meowtosis.Presentation
{
    // Mở scene theo tên trong Build Settings.
    public sealed class SceneNavigator : ISceneNavigator
    {
        private const string HomeScene = "Home";
        private const string GameScene = "Game";

        // Giữ qua lần đổi scene vì màn Home sinh presenter mới.
        private static bool isMapRequested;

        public void OpenHome() => SceneManager.LoadScene(HomeScene);

        public void OpenMap()
        {
            isMapRequested = true;
            OpenHome();
        }

        public bool TakeMapRequest()
        {
            var wasRequested = isMapRequested;
            isMapRequested = false;
            return wasRequested;
        }

        public void OpenGame() => SceneManager.LoadScene(GameScene);
    }
}
