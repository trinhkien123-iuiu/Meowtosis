using System;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Bộ âm thanh của game (Data/GameSounds): kéo clip và chỉnh âm lượng từng tiếng trong Inspector.
    [CreateAssetMenu(fileName = "GameSounds", menuName = "Meowtosis/Game Sounds")]
    public sealed class GameSounds : ScriptableObject
    {
        [SerializeField] private SoundEntry[] sounds = Array.Empty<SoundEntry>();
        [SerializeField] private AudioClip[] meows = Array.Empty<AudioClip>();
        [SerializeField, Range(0f, 1f)] private float meowVolume = 0.75f;
        [Tooltip("Cao độ tiếng mèo theo loài: đỏ, xanh, vàng, hồng, tím, vi khuẩn.")]
        [SerializeField] private float[] voicePitches = { 1f, 0.86f, 1.16f, 1.26f, 0.78f, 1.08f };
        [SerializeField] private AudioClip music;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.3f;

        public AudioClip[] Meows => meows;

        public float MeowVolume => meowVolume;

        public AudioClip Music => music;

        public float MusicVolume => musicVolume;

        public float VoicePitch(int speciesIndex) => speciesIndex < voicePitches.Length ? voicePitches[speciesIndex] : 1f;

        public SoundEntry Find(SoundId id) => Array.Find(sounds, entry => entry.Id == id);

        [Serializable]
        public sealed class SoundEntry
        {
            [SerializeField] private SoundId id;
            [SerializeField] private AudioClip clip;
            [SerializeField, Range(0f, 1f)] private float volume = 0.7f;

            public SoundId Id => id;

            public AudioClip Clip => clip;

            public float Volume => volume;
        }
    }
}
