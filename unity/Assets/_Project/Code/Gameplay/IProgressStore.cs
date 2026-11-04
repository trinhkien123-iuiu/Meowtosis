namespace Meowtosis.Gameplay
{
    // Nơi lưu tiến độ người chơi (PlayerPrefs trong game, bộ nhớ trong test).
    public interface IProgressStore
    {
        PlayerProgress Load();

        void Save(PlayerProgress progress);
    }
}
