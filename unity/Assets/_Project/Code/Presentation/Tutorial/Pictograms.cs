using System;
using System.Text.RegularExpressions;

namespace Meowtosis.Presentation
{
    // Hình minh hoạ chèn trong lời hướng dẫn: trong dữ liệu level viết :ten: (ví dụ ":red::red::red: :arrow: :poof:"),
    // dấu | là xuống dòng (thường: một dòng hình, một dòng chữ),
    // trong game đổi thành ảnh nhỏ của bộ TMP Sprite Asset "Pictograms" (Art/UI/Pictograms) gắn trên chữ của bong bóng.
    // Tên lạ thì giữ nguyên chữ để dễ thấy lỗi gõ.
    public static class Pictograms
    {
        public static readonly string[] Names = { "red", "blue", "yellow", "poof", "arrow", "empty", "check", "cross" };

        private static readonly Regex Token = new Regex(":([a-z]+):");

        public static string Format(string text) =>
            string.IsNullOrEmpty(text) ? text : Token.Replace(text, match =>
                Array.IndexOf(Names, match.Groups[1].Value) >= 0 ? $"<sprite name=\"{match.Groups[1].Value}\">" : match.Value).Replace('|', '\n');

        // Dòng hình (trước dấu | đầu tiên) to hơn chữ cho dễ nhìn.
        public static string FormatWithBigPictures(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var split = text.IndexOf('|');
            return split < 0 ? Format(text) : $"<size=135%>{Format(text.Substring(0, split))}</size>\n{Format(text.Substring(split + 1))}";
        }
    }
}
