using Meowtosis.Domain;

namespace Meowtosis.Presentation
{
    public enum SoundId
    {
        ButtonTap,
        PopupOpen,
        PopupClose,
        BoxDone,
        Clear,
        Sparkle,
        Win,
        Lose,
        Push,
        Replace,
        Toxic,
        Eat,
        Grumble,
        Squish,
        Booster,
        Revive,
        Coin,
        Cashout,
        AngryMeow,
        LevelSelect,
        LevelLocked,
        Portal,
        ColorSwitch,
        MutateMagic,
        CloneMagic,
        StarLost,
    }

    // Phát âm thanh, nhìn từ phía presenter và phần diễn hoạt.
    public interface ISoundPlayer
    {
        void Play(SoundId sound);

        // Tiếng xu chạm ô đếm: cao dần theo số xu đã về.
        void PlayCoin(int coinsArrived);

        // Tiếng ngôi sao đáp xuống: sao sau cao hơn sao trước.
        void PlayStar(int starIndex);

        // Tiếng mèo ngẫu nhiên, mỗi màu một giọng; loudness và pitchUp nhân thêm vào mức thường.
        void PlayMeow(Species species, float loudness = 1f, float pitchUp = 1f);

        void SetSoundOn(bool on);

        void SetMusicOn(bool on);
    }
}
