using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Emberbound
{
    public sealed class DuelGame : MonoBehaviour
    {
        public static DuelGame Instance { get; private set; }
        public DragonActor Player { get; private set; }
        public DragonActor Enemy { get; private set; }
        public bool Finished { get; private set; }
        public bool Paused { get; private set; }
        public DragonActor Winner { get; private set; }
        public float Shake { get; private set; }
        float shakeDuration, shakeRemaining, shakeStrength;
        public DuelAudio Audio { get; private set; }
        public float MatchTime { get; private set; }
        Camera view;
        bool demo;
        float finishedAt;
        float finishedAtRealtime;
        public float SecondsSinceFinished => Finished ? Time.unscaledTime - finishedAtRealtime : 0;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            DuelScenePreview.ResetTitles();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (FindFirstObjectByType<DuelGame>() == null) new GameObject("Emberbound | Match controller").AddComponent<DuelGame>();
        }
        void Awake()
        {
            Instance = this; Time.timeScale = 1; AudioListener.pause = false;
            Application.targetFrameRate = 60;
            QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowDistance = 65; QualitySettings.antiAliasing = 4;
            demo = Array.IndexOf(Environment.GetCommandLineArgs(), "-demo") >= 0;
            ArenaBuilder.Build();
            view = new GameObject("Tactical camera").AddComponent<Camera>(); view.tag = "MainCamera";
            view.orthographic = true; view.orthographicSize = 13.7f; view.transform.position = new Vector3(0, 22, -19); view.transform.rotation = Quaternion.Euler(49, 0, 0); view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = new Color(0.035f, 0.055f, 0.08f); view.farClipPlane = 100;
            view.gameObject.AddComponent<AudioListener>();
            Audio = gameObject.AddComponent<DuelAudio>();
            Player = Spawn(true, new Vector3(-4, 0, -2), new Color(0.15f, 0.72f, 0.71f), DuelScenePreview.PlayerTitle);
            Enemy = Spawn(false, new Vector3(4, 0, 2), new Color(0.8f, 0.22f, 0.15f), DuelScenePreview.EnemyTitle);
            Player.Opponent = Enemy; Enemy.Opponent = Player; Player.Automated = demo;
            Player.transform.LookAt(Enemy.transform); Enemy.transform.LookAt(Player.transform);
            if (FindFirstObjectByType<DuelHud>() == null)
                Debug.LogError("Duel scene needs its saved Duel UI Canvas. Use Emberbound > Create Editable HUD.");
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-smoke") >= 0) gameObject.AddComponent<RuntimeSmokeTest>();
        }
        DragonActor Spawn(bool player, Vector3 position, Color tint, string title)
        {
            var actor = new GameObject(title + (player ? " | Player" : " | AI")).AddComponent<DragonActor>();
            actor.transform.position = position; actor.Initialize(player, tint, title); return actor;
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !Finished) TogglePause();
            if (Input.GetKeyDown(KeyCode.M)) AudioListener.volume = AudioListener.volume > 0 ? 0 : 1;
            if (!Finished) MatchTime += Time.deltaTime;
            if (demo && Finished && Time.time - finishedAt > 6) Application.Quit();
        }
        void LateUpdate()
        {
            // Fixed arena framing prevents either combatant from escaping the view; account for portrait/narrow windows.
            view.orthographicSize = Mathf.Max(13.7f, 15 / Mathf.Max(0.5f, view.aspect));
            shakeRemaining = Mathf.Max(0, shakeRemaining - Time.deltaTime);
            float fade = shakeDuration > 0 ? shakeRemaining / shakeDuration : 0;
            Shake = shakeStrength * fade * fade;
            float tick = Time.time * 32;
            Vector3 offset = view.transform.right * ((Mathf.PerlinNoise(tick, 3.1f) * 2 - 1) * Shake)
                + view.transform.up * ((Mathf.PerlinNoise(8.7f, tick) * 2 - 1) * Shake);
            view.transform.position = new Vector3(0, 22, -19) + offset;
        }
        public void TriggerShake(float strength, float duration)
        {
            if (strength <= 0 || duration <= 0) return;
            shakeStrength = Mathf.Max(Shake, strength);
            shakeRemaining = Mathf.Max(shakeRemaining, duration);
            shakeDuration = shakeRemaining;
            Shake = shakeStrength;
        }
        public void End(DragonActor winner)
        {
            if (Finished) return;
            Finished = true; Winner = winner; finishedAt = Time.time; finishedAtRealtime = Time.unscaledTime;
        }
        public void TogglePause() { Paused = !Paused; Time.timeScale = Paused ? 0 : 1; AudioListener.pause = Paused; }
        public void Restart() { Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    }
}
