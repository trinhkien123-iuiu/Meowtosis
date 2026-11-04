using System;
using System.IO;

namespace UnityEngine
{
    // Thay cho UnityEngine.Application để test EditMode chạy được bằng .NET thường.
    public static class Application
    {
        public static string dataPath => FindAssetsFolder();

        private static string FindAssetsFolder()
        {
            for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
            {
                var assets = Path.Combine(folder.FullName, "unity", "Assets");
                if (Directory.Exists(assets)) return assets;
            }
            throw new DirectoryNotFoundException("Không tìm thấy unity/Assets phía trên " + AppContext.BaseDirectory);
        }
    }
}
