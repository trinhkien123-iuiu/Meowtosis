using UnityEngine;

namespace Meowtosis.Bootstrap
{
    // Android mặc định giới hạn 30 khung hình mỗi giây, hoạt ảnh sẽ kém mượt: đặt 60 ngay khi game khởi động, trước mọi scene.
    // Tắt vSync để `targetFrameRate` có hiệu lực: bật vSync (các mức chất lượng Medium trở lên) thì máy 90 / 120 Hz chạy
    // 90 / 120 khung hình, game 2D nhẹ này không cần, chỉ làm máy nóng và tốn pin.
    public static class FrameRateSetup
    {
        private const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
            // Game không cần đèn, bóng đổ hay khử răng cưa: bỏ khỏi mọi mức chất lượng cho đỡ tốn GPU.
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.antiAliasing = 0;
            QualitySettings.softParticles = false;
            QualitySettings.realtimeReflectionProbes = false;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
