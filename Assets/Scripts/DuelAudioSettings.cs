using UnityEngine;

namespace Emberbound
{
    [CreateAssetMenu(menuName = "Emberbound/Audio Settings")]
    public sealed class DuelAudioSettings : ScriptableObject
    {
        public AudioClip Fire, Tail, Flight, Music;
        [Range(0, 1)] public float EffectsVolume = 0.75f;
        [Range(0, 1)] public float MusicVolume = 0.18f;
    }
}
