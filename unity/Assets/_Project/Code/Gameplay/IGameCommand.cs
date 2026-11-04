using Meowtosis.Domain;

namespace Meowtosis.Gameplay
{
    public enum BoosterKind
    {
        None,
        Mutate,
        Transplant,
        Revive,
        Hint,
    }

    // Một nước đi người chơi gửi vào phiên chơi: vuốt hoặc dùng booster.
    // Mọi thay đổi bàn đều đi qua lệnh, nên GameSession hoàn tác được mọi loại nước như nhau.
    public interface IGameCommand
    {
        // Booster mà nước này tiêu (None với một lần vuốt thường).
        BoosterKind Booster { get; }

        bool TryApply(Board board, out MoveResult result);
    }
}
