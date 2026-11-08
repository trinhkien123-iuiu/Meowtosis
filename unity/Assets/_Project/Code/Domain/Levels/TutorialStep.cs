using System.Collections.Generic;

namespace Meowtosis.Domain
{
    // Một bước hướng dẫn: bàn tay chỉ vào Move, game chỉ nhận đúng nước đó và hiện Text trong bong bóng lời thoại.
    // Rules (có thể rỗng) là các câu luật hiện lần lượt sau khi người chơi đi xong nước này; mỗi câu chờ bấm "Đã hiểu!" mới sang câu kế
    // hoặc bước kế.
    public sealed class TutorialStep
    {
        private static readonly string[] NoRules = new string[0];

        public TutorialStep(Move move, string text, IReadOnlyList<string> rules = null)
        {
            Move = move;
            Text = text;
            Rules = rules ?? NoRules;
        }

        public Move Move { get; }

        public string Text { get; }

        public IReadOnlyList<string> Rules { get; }

        public bool HasRule => Rules.Count > 0;

        // Thêm một câu luật nữa vào cuối (nhiều dòng 'rule' liên tiếp thành nhiều câu).
        public TutorialStep WithRule(string rule)
        {
            var all = new List<string>(Rules) { rule };
            return new TutorialStep(Move, Text, all);
        }
    }
}
