using System.Collections;
using System.IO;
using UnityEngine;

namespace Emberbound
{
    // Opt-in player test. Exercises real coroutines, geometry, death and winner state.
    public sealed class RuntimeSmokeTest : MonoBehaviour
    {
        static bool restartExpected;
        IEnumerator Start()
        {
            var game = DuelGame.Instance;
            game.Player.enabled = false; game.Enemy.enabled = false;
            yield return null;
            var hud = FindFirstObjectByType<DuelHud>();
            if (hud == null || FindObjectsByType<DuelHud>(FindObjectsSortMode.None).Length != 1 || UnityEngine.EventSystems.EventSystem.current == null)
            { Fail("Saved Canvas HUD or EventSystem missing/duplicated"); yield break; }
            hud.Refresh(game);
            if (game.Player.Title != DuelScenePreview.PlayerTitle || game.Enemy.Title != DuelScenePreview.EnemyTitle)
            { Fail("Scene dragon titles were not carried into gameplay"); yield break; }
            game.Player.Title = "TEST AZURE"; game.Enemy.Title = "TEST CINDER";
            hud.Refresh(game);
            if (hud.PlayerHealth.Name.text != game.Player.Title || hud.EnemyHealth.Name.text != game.Enemy.Title)
            { Fail("HUD names did not follow actor titles"); yield break; }
            if (restartExpected)
            {
                if (game.Finished || game.Paused || game.Player.Combat.Health != CombatRules.MaxHealth || hud.PlayerHealth.Fill.fillAmount != 1 || hud.WinnerPanel.activeSelf)
                { Fail("HUD restart did not restore fresh match"); yield break; }
                game.Player.TakeHit(1000, Vector3.zero);
                hud.Refresh(game);
                if (game.Winner != game.Enemy || (hud.ResultDelaySeconds > 0 && hud.WinnerPanel.activeSelf))
                { Fail("Defeat panel appeared before its delay"); yield break; }
                yield return new WaitForSecondsRealtime(Mathf.Max(0, hud.ResultDelaySeconds) + 0.1f);
                hud.Refresh(game);
                if (!hud.WinnerPanel.activeSelf || !hud.WinnerTitle.text.Contains(game.Enemy.Title) || hud.WinnerMessage.text != hud.DefeatMessage)
                { Fail("Delayed defeat panel failed"); yield break; }
                Directory.CreateDirectory("output");
                File.WriteAllText("output/runtime-smoke.txt", "PASS: delayed win and lose panels, immediate combat lockout, editor Canvas, button callbacks, health/cooldowns, pause/resume, restart, team models, audio, combat, auto-facing and landing shake.");
                Debug.Log("EMBERBOUND_RUNTIME_OK"); restartExpected = false; Application.Quit(0); yield break;
            }
            foreach (var actor in new[] { game.Player, game.Enemy })
            {
                var skin = DragonSkin.ForTeam(actor.IsPlayer);
                if (skin == null) continue;
                var model = actor.transform.Find("Dragon model/" + skin.Prefab.name + "(Clone)");
                if (model == null || model.GetComponent<DragonAnimationAdapter>() == null || model.GetComponentsInChildren<SkinnedMeshRenderer>().Length == 0)
                { Fail("Team model or animation adapter missing: " + actor.Title); yield break; }
                Debug.Log("SMOKE TEAM " + actor.Title + " uses " + skin.Prefab.name);
            }
            if (Camera.allCamerasCount != 1 || FindFirstObjectByType<DuelScenePreview>() != null)
            { Fail("Editor preview was not removed on Play"); yield break; }
            Directory.CreateDirectory("output");
            var audio = Resources.Load<DuelAudioSettings>("DuelAudioSettings");
            if (audio == null || audio.Fire == null || audio.Tail == null || audio.Flight == null || audio.Music == null || !game.Audio.MusicPlaying)
            { Fail("Collected audio or looping music missing"); yield break; }
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("output/smoke-arena.png"));
            foreach (Ability ability in new[] { Ability.Fire, Ability.Tail, Ability.Flight })
            {
                Position(game.Player, new Vector3(0, 0, -2));
                Position(game.Enemy, new Vector3(0, 0, ability == Ability.Tail ? 0 : 3));
                game.Player.transform.rotation = Quaternion.identity;
                Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();
                float before = game.Enemy.Combat.Health;
                Debug.Log("SMOKE BEFORE " + ability + " player=" + game.Player.transform.position + " forward=" + game.Player.transform.forward + " enemy=" + game.Enemy.transform.position);
                hud.Abilities[(int)ability].Button.onClick.Invoke();
                if (!game.Player.Busy) { Fail("Canvas ability button did not cast: " + ability); yield break; }
                hud.Refresh(game);
                if (hud.Abilities[(int)ability].CooldownFill.fillAmount <= 0 || !hud.Abilities[(int)ability].CooldownOverlay.activeSelf)
                { Fail("Canvas cooldown did not update"); yield break; }
                if (game.Player.Cast(ability)) { Fail("Busy attack accepted"); yield break; }
                // Move the rival sideways during windup to catch a stale, cast-time fire heading.
                if (ability == Ability.Fire) Position(game.Enemy, new Vector3(4, 0, -2));
                if (ability == Ability.Flight)
                {
                    yield return new WaitForSeconds(0.65f);
                    if (game.Shake > 0.001f) { Fail("Landing shake started during ascent"); yield break; }
                    float deadline = Time.time + 2;
                    while (game.Player.Airborne && Time.time < deadline) yield return null;
                    if (game.Player.Airborne || game.Shake < 0.2f) { Fail("Landing did not trigger camera shake"); yield break; }
                    yield return new WaitForSeconds(0.7f);
                    if (game.Shake > 0.001f || Vector3.Distance(Camera.main.transform.position, new Vector3(0, 22, -19)) > 0.001f)
                    { Fail("Camera did not settle after landing"); yield break; }
                }
                else yield return new WaitForSeconds(2);
                Debug.Log("SMOKE AFTER " + ability + " player=" + game.Player.transform.position + " forward=" + game.Player.transform.forward + " enemy=" + game.Enemy.transform.position + " busy=" + game.Player.Busy + " airborne=" + game.Enemy.Airborne);
                if (ability == Ability.Fire && Vector3.Dot(game.Player.transform.forward, (game.Enemy.transform.position - game.Player.transform.position).normalized) < 0.99f)
                { Fail("Dragon did not face the rival during attack"); yield break; }
                if (Mathf.Abs(before - game.Enemy.Combat.Health - CombatRules.Damage(ability)) > 0.01f) { Fail("Wrong damage: " + ability + "; actual=" + (before - game.Enemy.Combat.Health)); yield break; }
                hud.Refresh(game);
                if (Mathf.Abs(hud.EnemyHealth.Fill.fillAmount - game.Enemy.Combat.Health / CombatRules.MaxHealth) > 0.001f)
                { Fail("Canvas health did not update"); yield break; }
            }
            // Landing is an impact event even when the opponent dodges: no damage required to shake.
            yield return new WaitForSeconds(game.Player.Combat.Remaining(Ability.Flight, Time.time) + 0.05f);
            Position(game.Player, new Vector3(0, 0, -2)); Position(game.Enemy, new Vector3(0, 0, 3));
            Physics.SyncTransforms(); yield return new WaitForFixedUpdate();
            float healthBeforeMiss = game.Enemy.Combat.Health;
            if (!game.Player.Cast(Ability.Flight)) { Fail("Missed flight setup"); yield break; }
            Position(game.Enemy, new Vector3(8, 0, 0));
            float landingDeadline = Time.time + 2;
            while (game.Player.Airborne && Time.time < landingDeadline) yield return null;
            if (game.Shake < 0.2f || game.Enemy.Combat.Health != healthBeforeMiss)
            { Fail("Missed landing must shake without damage"); yield break; }
            game.TogglePause(); float pausedShake = game.Shake;
            yield return new WaitForSecondsRealtime(0.15f);
            if (!AudioListener.pause || Mathf.Abs(game.Shake - pausedShake) > 0.001f) { Fail("Pause did not freeze feedback"); yield break; }
            hud.Refresh(game);
            if (!hud.PausePanel.activeSelf) { Fail("Canvas pause panel missing"); yield break; }
            hud.ResumeButton.onClick.Invoke();
            if (game.Paused || AudioListener.pause) { Fail("Canvas resume button failed"); yield break; }
            yield return new WaitForSeconds(0.7f);
            game.Enemy.TakeHit(1000, Vector3.zero);
            yield return null;
            if (!game.Finished || game.Winner != game.Player) { Fail("Winner state"); yield break; }
            hud.Refresh(game);
            if ((hud.ResultDelaySeconds > 0 && hud.WinnerPanel.activeSelf) || game.Player.Cast(Ability.Fire))
            { Fail("Result delay or immediate combat lockout failed"); yield break; }
            float delayRemaining = Mathf.Max(0, hud.ResultDelaySeconds - game.SecondsSinceFinished);
            if (delayRemaining > 0.1f)
            {
                yield return new WaitForSecondsRealtime(delayRemaining * 0.5f);
                hud.Refresh(game);
                if (hud.WinnerPanel.activeSelf) { Fail("Win panel appeared halfway through delay"); yield break; }
            }
            yield return new WaitForSecondsRealtime(Mathf.Max(0, hud.ResultDelaySeconds - game.SecondsSinceFinished) + 0.1f);
            hud.Refresh(game);
            if (!hud.WinnerPanel.activeSelf || !hud.WinnerTitle.text.Contains(game.Player.Title))
            { Fail("Canvas winner panel failed"); yield break; }
            Directory.CreateDirectory("output");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("output/smoke-winner.png"));
            yield return new WaitForSeconds(1);
            restartExpected = true;
            hud.RestartButton.onClick.Invoke();
            yield return new WaitForSeconds(2);
            Fail("Canvas restart button did not reload the scene");
        }
        static void Position(DragonActor actor, Vector3 position)
        {
            var body = actor.GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.None;
            body.linearVelocity = Vector3.zero;
            body.position = position;
            body.rotation = Quaternion.identity;
            actor.transform.position = position;
            actor.transform.rotation = Quaternion.identity;
        }
        static void Fail(string message) { Debug.LogError("EMBERBOUND_RUNTIME_FAIL: " + message); Application.Quit(1); }
    }
}
