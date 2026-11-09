using UnityEngine;

namespace Meowtosis.Presentation
{
    // Chế độ giảm chuyển động (cho người dễ chóng mặt): tắt rung bàn, dừng hình, làm chậm thời gian và rung thanh sao.
    // Hiệu ứng hạt, nảy, co giãn của quân vẫn giữ nguyên. Lưu riêng bằng PlayerPrefs vì chỉ là tuỳ chọn hiển thị của máy này.
    public static class MotionSettings
    {
        private const string Key = "meowtosis.reducedMotion";
        private static int cached = -1;

        public static bool Reduced
        {
            get
            {
                if (cached < 0) cached = Read();
                return cached == 1;
            }
            set
            {
                cached = value ? 1 : 0;
                try
                {
                    PlayerPrefs.SetInt(Key, cached);
                    PlayerPrefs.Save();
                }
                catch (System.Exception)
                {
                    // Không lưu được thì chỉ có hiệu lực trong lần chạy này.
                }
            }
        }

        private static int Read()
        {
            try { return PlayerPrefs.GetInt(Key, 0) == 1 ? 1 : 0; }
            catch (System.Exception) { return 0; }
        }
    }
}
