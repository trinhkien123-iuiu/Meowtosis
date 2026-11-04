using UnityEngine;

namespace Meowtosis.Bootstrap
{
    // Android mặc định giới hạn 30 khung hình mỗi giây, hoạt ảnh sẽ kém mượt: đặt 60 ngay khi game khởi động, trước mọi scene.
    public static class FrameRateSetup
    {
        private const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply() => Application.targetFrameRate = TargetFrameRate;
    }
}
