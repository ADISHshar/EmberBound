using System.Collections.Generic;
using UnityEngine;

namespace Emberbound
{
    public sealed class DuelEffects : MonoBehaviour
    {
        static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        static Material particleMaterial;
        static readonly Dictionary<int, AudioClip> sounds = new Dictionary<int, AudioClip>();
        float remaining, lifetime;
        Vector3 velocity;
        LineRenderer ring;
        TextMesh text;
        public static Material Material(Color color, bool glow = false)
        {
            if (materials.TryGetValue(color, out var existing) && existing != null) return existing;
            var material = new Material(Shader.Find("Standard"));
            material.color = color; material.SetFloat("_Glossiness", 0.25f);
            material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", glow ? color * 1.7f : Color.black);
            materials[color] = material; return material;
        }
        static Material ParticleMaterial()
        {
            if (particleMaterial == null) particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
            return particleMaterial;
        }
        public static void Ring(Vector3 position, float radius, Color color, float duration, Transform follow = null)
        {
            var go = new GameObject("Ability ring"); go.transform.position = position + Vector3.up * 0.08f;
            if (follow != null) go.transform.SetParent(follow, true);
            var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.loop = true; line.positionCount = 64;
            line.startWidth = line.endWidth = 0.055f; line.sharedMaterial = Material(color, true);
            for (int i = 0; i < 64; i++) { float a = i * Mathf.PI * 2 / 64; line.SetPosition(i, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius); }
            if (duration > 0) { var effect = go.AddComponent<DuelEffects>(); effect.remaining = effect.lifetime = duration; effect.ring = line; }
        }
        public static void Burst(Vector3 position, Color color, int count)
        {
            var go = new GameObject("Impact sparks"); go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.duration = 0.4f; main.startLifetime = 0.55f; main.startSpeed = 5; main.startSize = 0.13f; main.startColor = color; main.gravityModifier = 1; main.maxParticles = 100;
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.15f;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMaterial(); ps.Play(); ps.Emit(count); Destroy(go, 1.5f);
        }
        public static void Flame(Vector3 position, Vector3 direction)
        {
            var go = new GameObject("Fire breath"); go.transform.position = position; go.transform.rotation = Quaternion.LookRotation(direction);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.duration = 0.35f; main.startLifetime = 0.48f; main.startSpeed = 12; main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.55f); main.startColor = new ParticleSystem.MinMaxGradient(new Color(1, 0.2f, 0.025f), new Color(1, 0.85f, 0.2f)); main.maxParticles = 300;
            var emission = ps.emission; emission.rateOverTime = 300;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 24; shape.radius = 0.12f;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMaterial(); ps.Play(); Destroy(go, 1.5f);
        }
        public static void Popup(Vector3 position, string value, Color color)
        {
            var go = new GameObject("Damage number"); go.transform.position = position;
            var label = go.AddComponent<TextMesh>(); label.text = value; label.fontSize = 64; label.characterSize = 0.055f; label.anchor = TextAnchor.MiddleCenter; label.color = color;
            var effect = go.AddComponent<DuelEffects>(); effect.text = label; effect.remaining = effect.lifetime = 0.9f; effect.velocity = Vector3.up;
        }
        public static void Sound(int pitch, float duration)
        {
            if (!sounds.TryGetValue(pitch, out var clip) || clip == null)
            {
                int count = Mathf.CeilToInt(22050 * duration); var samples = new float[count];
                var random = new System.Random(pitch);
                for (int i = 0; i < count; i++)
                {
                    float t = i / 22050f, envelope = Mathf.Sin(Mathf.PI * i / count) * (1 - i / (float)count);
                    samples[i] = (Mathf.Sin(2 * Mathf.PI * pitch * t * (1 - t * 0.4f)) * 0.5f + ((float)random.NextDouble() * 2 - 1) * 0.5f) * envelope * 0.24f;
                }
                clip = AudioClip.Create("Original synthesized combat sound", count, 1, 22050, false); clip.SetData(samples, 0); sounds[pitch] = clip;
            }
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, 0.8f);
        }
        void Update()
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0) { Destroy(gameObject); return; }
            if (ring != null) { ring.widthMultiplier = 0.055f * remaining / lifetime; transform.localScale += Vector3.one * Time.deltaTime * 0.3f; }
            if (text != null) { transform.position += velocity * Time.deltaTime; transform.rotation = Camera.main.transform.rotation; text.color = new Color(1, 1, 1, remaining / lifetime); }
        }
    }
}
