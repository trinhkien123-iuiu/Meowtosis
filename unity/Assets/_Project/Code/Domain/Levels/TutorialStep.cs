namespace Meowtosis.Domain
{
    // Một bước hướng dẫn: bàn tay chỉ vào Move, game chỉ nhận đúng nước đó và hiện Text trong bong bóng lời thoại.
    // Rule (có thể rỗng) là câu luật hiện sau khi người chơi đi xong nước này, chờ bấm "Đã hiểu!" mới đi tiếp.
    public sealed class TutorialStep
    {
        public TutorialStep(Move move, string text, string rule = "")
        {
            Move = move;
            Text = text;
            Rule = rule;
        }

        public Move Move { get; }

        public string Text { get; }

        public string Rule { get; }

        public bool HasRule => Rule.Length > 0;

        public TutorialStep WithRule(string rule) => new TutorialStep(Move, Text, rule);
    }
}
