namespace Meowtosis.Domain
{
    // Một bước hướng dẫn: bàn tay chỉ vào Move, game chỉ nhận đúng nước đó và hiện Text trong bong bóng lời thoại.
    public sealed class TutorialStep
    {
        public TutorialStep(Move move, string text)
        {
            Move = move;
            Text = text;
        }

        public Move Move { get; }

        public string Text { get; }
    }
}
