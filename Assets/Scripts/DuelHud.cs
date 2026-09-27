using System;
using UnityEngine;
using UnityEngine.UI;

namespace Emberbound
{
    // Layout/style live in the saved Canvas. Only changing game data is updated here.
    [ExecuteAlways]
    public sealed class DuelHud : MonoBehaviour
    {
        public enum PreviewScreen { Gameplay, Pause, Winner }
        [Serializable] public sealed class HealthWidgets { public Image Fill; public Text Value, Name; }
        [Serializable] public sealed class AbilityWidgets
        {
            public Button Button;
            public Image CooldownFill;
            public GameObject CooldownOverlay;
            public Text Countdown, Status;
        }
        [Header("Editor preview (does not change gameplay)")]
        public PreviewScreen EditorPreview;
        [Header("HUD bindings")]
        public HealthWidgets PlayerHealth, EnemyHealth;
        public Text MatchTimer, EnemyState;
        public GameObject OpeningHint;
        public AbilityWidgets[] Abilities = new AbilityWidgets[3];
        [Header("Panels and buttons")]
        public GameObject PausePanel, WinnerPanel;
        public Button ResumeButton, RestartButton;
        public Text WinnerTitle, WinnerMessage;
        [Min(0), Tooltip("Seconds to let the final hit/death animation play before showing the win or lose panel. Zero shows it immediately.")]
        public float ResultDelaySeconds = 1.5f;
        [Header("Editable runtime text")]
        public string ReadyText = "READY";
        public string EnemyStatePrefix = "RIVAL / ";
        public string WinnerTitleFormat = "{0} WINS";
        [TextArea] public string VictoryMessage = "The sanctuary is yours.";
        [TextArea] public string DefeatMessage = "Rise from the ashes. Try again.";
        void Update()
        {
            if (!Application.isPlaying)
            {
                Show(PausePanel, EditorPreview == PreviewScreen.Pause);
                Show(WinnerPanel, EditorPreview == PreviewScreen.Winner);
                return;
            }
            var game = DuelGame.Instance;
            if (game == null || game.Player == null || game.Enemy == null) return;
            Refresh(game);
        }
        public void Refresh(DuelGame game)
        {
            // Resolve bindings for existing saved Canvases without rebuilding user layouts.
            if (PlayerHealth.Name == null) PlayerHealth.Name = transform.Find("Player Health/Dragon Name")?.GetComponent<Text>();
            if (EnemyHealth.Name == null) EnemyHealth.Name = transform.Find("Opponent Health/Dragon Name")?.GetComponent<Text>();
            Health(PlayerHealth, game.Player); Health(EnemyHealth, game.Enemy);
            if (MatchTimer != null) MatchTimer.text = Mathf.FloorToInt(game.MatchTime / 60).ToString("00") + ":" + Mathf.FloorToInt(game.MatchTime % 60).ToString("00");
            if (EnemyState != null) EnemyState.text = EnemyStatePrefix + game.Enemy.StateLabel.ToUpperInvariant();
            Show(OpeningHint, game.MatchTime < 5 && !game.Finished && !game.Paused);
            for (int i = 0; i < Abilities.Length && i < 3; i++)
            {
                var view = Abilities[i]; if (view == null) continue;
                float cooldown = game.Player.Combat.Remaining((Ability)i, Time.time);
                if (view.Button != null) view.Button.interactable = !game.Finished && !game.Paused && !game.Player.Busy && cooldown <= 0;
                if (view.CooldownFill != null) view.CooldownFill.fillAmount = cooldown / CombatRules.Cooldown((Ability)i);
                Show(view.CooldownOverlay, cooldown > 0);
                if (view.Countdown != null) view.Countdown.text = cooldown.ToString("0.0");
                if (view.Status != null) view.Status.text = cooldown > 0 ? cooldown.ToString("0.0") + "s" : ReadyText;
            }
            Show(PausePanel, game.Paused && !game.Finished);
            Show(WinnerPanel, game.Finished && game.SecondsSinceFinished >= Mathf.Max(0, ResultDelaySeconds));
            if (game.Finished && game.Winner != null)
            {
                if (WinnerTitle != null) WinnerTitle.text = WinnerTitleFormat.Replace("{0}", game.Winner.Title);
                if (WinnerMessage != null) WinnerMessage.text = game.Winner.IsPlayer ? VictoryMessage : DefeatMessage;
            }
        }
        static void Show(GameObject value, bool show) { if (value != null && value.activeSelf != show) value.SetActive(show); }
        static void Health(HealthWidgets view, DragonActor actor)
        {
            if (view == null) return;
            if (view.Name != null) view.Name.text = actor.Title;
            if (view.Fill != null) view.Fill.fillAmount = actor.Combat.Health / CombatRules.MaxHealth;
            if (view.Value != null) view.Value.text = actor.Combat.Health.ToString("0") + " / " + CombatRules.MaxHealth.ToString("0");
        }
        public void UseAbility(int index)
        {
            if (Application.isPlaying && index >= 0 && index < 3 && DuelGame.Instance != null) DuelGame.Instance.Player.Cast((Ability)index);
        }
        public void Resume()
        {
            if (Application.isPlaying && DuelGame.Instance != null && DuelGame.Instance.Paused) DuelGame.Instance.TogglePause();
        }
        public void Restart()
        {
            if (Application.isPlaying && DuelGame.Instance != null) DuelGame.Instance.Restart();
        }
    }
}
