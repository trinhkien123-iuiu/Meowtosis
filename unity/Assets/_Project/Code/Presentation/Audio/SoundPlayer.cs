using Meowtosis.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Phát âm thanh qua các AudioSource đặt sẵn trong scene (đối tượng Audio): một nguồn nhạc nền, vài nguồn tiếng để chồng lên nhau.
    public sealed class SoundPlayer : MonoBehaviour, ISoundPlayer
    {
        private const int HighestCoinStep = 14;
        private const float CoinPitchStep = 1.035f;
        private const float StarPitchStep = 1.19f;

        [SerializeField] private GameSounds sounds;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource[] effectSources;

        private int nextSource;
        private int lastMeow = -1;
        private bool soundOn = true;

        public void Play(SoundId sound)
        {
            var entry = sounds.Find(sound);
            if (entry != null) PlayClip(entry.Clip, entry.Volume, 1f);
        }

        public void PlayCoin(int coinsArrived)
        {
            var entry = sounds.Find(SoundId.Coin);
            if (entry != null) PlayClip(entry.Clip, entry.Volume, Mathf.Pow(CoinPitchStep, Mathf.Min(coinsArrived, HighestCoinStep)));
        }

        public void PlayStar(int starIndex)
        {
            var entry = sounds.Find(SoundId.Sparkle);
            if (entry != null) PlayClip(entry.Clip, entry.Volume, Mathf.Pow(StarPitchStep, starIndex));
        }

        // Không lặp tiếng vừa phát; cao độ lệch ngẫu nhiên một chút để không đơn điệu.
        public void PlayMeow(Species species, float loudness = 1f, float pitchUp = 1f)
        {
            var meows = sounds.Meows;
            if (meows.Length == 0) return;
            var index = Random.Range(0, meows.Length);
            if (meows.Length > 1 && index == lastMeow) index = (index + 1) % meows.Length;
            lastMeow = index;
            var pitch = sounds.VoicePitch((int)species) * pitchUp * Random.Range(0.95f, 1.06f);
            PlayClip(meows[index], sounds.MeowVolume * loudness, pitch);
        }

        public void SetSoundOn(bool on) => soundOn = on;

        public void SetMusicOn(bool on)
        {
            if (on && !musicSource.isPlaying)
            {
                musicSource.clip = sounds.Music;
                musicSource.volume = sounds.MusicVolume;
                musicSource.loop = true;
                musicSource.Play();
            }
            else if (!on) musicSource.Stop();
        }

        // Mọi nút trong scene kêu "tách" khi bấm, kể cả nút trong popup đang tắt.
        private void Awake()
        {
            foreach (var button in FindObjectsOfType<Button>(true)) button.onClick.AddListener(() => Play(SoundId.ButtonTap));
        }

        private void PlayClip(AudioClip clip, float volume, float pitch)
        {
            if (!soundOn || clip == null || effectSources.Length == 0) return;
            var source = effectSources[nextSource];
            nextSource = (nextSource + 1) % effectSources.Length;
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
        }
    }
}
