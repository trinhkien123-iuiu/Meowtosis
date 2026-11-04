namespace Meowtosis.Domain
{
    // Một luật chạy cuối mỗi lượt, sau khi đặt mèo mới.
    // TurnResolver chạy các bước theo thứ tự cố định; cơ chế mới thường là thêm một bước.
    public interface ITurnStep
    {
        TurnStage Apply(Board board);
    }
}
