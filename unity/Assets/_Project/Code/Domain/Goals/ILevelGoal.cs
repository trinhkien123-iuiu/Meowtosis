namespace Meowtosis.Domain
{
    // Một điều kiện để thắng level. Level nào cũng có FillGoal; mỗi cơ chế thêm điều kiện riêng
    // (xem Level.CreateGoals), nên thêm loại điều kiện mới không phải sửa các loại cũ.
    public interface ILevelGoal
    {
        bool IsMet(Board board);
    }
}
