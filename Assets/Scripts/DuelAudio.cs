using System.Collections;
using UnityEngine;

namespace Emberbound
{
    public sealed class DuelAudio : MonoBehaviour
    {
        DuelAudioSettings settings;
        public bool MusicPlaying => music != null && music.isPlaying;
        AudioSource music;
        void Awake()
        {
            settings = Resources.Load<DuelAudioSettings>("DuelAudioSettings");
            if (settings == null || settings.Music == null) return;
            music = gameObject.AddComponent<AudioSource>();
            music.clip = settings.Music; music.loop = true; music.playOnAwake = false;
            music.spatialBlend = 0; music.volume = settings.MusicVolume; music.priority = 180;
            music.Play();
        }
        public void PlayAbility(Ability ability)
        {
            var clip = settings == null ? null : ability == Ability.Fire ? settings.Fire : ability == Ability.Tail ? settings.Tail : settings.Flight;
            if (clip == null) { DuelEffects.Sound(ability == Ability.Fire ? 170 : ability == Ability.Tail ? 90 : 330, 0.5f); return; }
            StartCoroutine(PlayEffect(clip, ability == Ability.Flight ? 1.65f : ability == Ability.Fire ? 0.9f : 0.7f));
        }
        IEnumerator PlayEffect(AudioClip clip, float maxDuration)
        {
            var voice = new GameObject("Audio | " + clip.name); voice.transform.SetParent(transform, false);
            var source = voice.AddComponent<AudioSource>(); source.clip = clip; source.playOnAwake = false;
            source.spatialBlend = 0; source.volume = settings.EffectsVolume; source.priority = 60; source.Play();
            // Bound long recordings to the action and fade their tails instead of cutting abruptly.
            float duration = Mathf.Min(clip.length, maxDuration);
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                source.volume = settings.EffectsVolume * Mathf.Clamp01((duration - elapsed) / 0.12f);
                yield return null;
            }
            Destroy(voice);
        }
    }
}
