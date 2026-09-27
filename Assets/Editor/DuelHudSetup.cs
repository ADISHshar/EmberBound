using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Emberbound.Editor
{
    public static class DuelHudSetup
    {
        static readonly Color Ink = new Color(0.035f, 0.055f, 0.075f, 0.96f);
        static readonly Color Muted = new Color(0.58f, 0.66f, 0.7f);
        static readonly Color Gold = new Color(0.9f, 0.68f, 0.34f);
        static readonly Color Blue = new Color(0.15f, 0.72f, 0.9f);
        static readonly Color Red = new Color(0.9f, 0.28f, 0.18f);
        static Sprite white;
        static Font font;

        [MenuItem("Emberbound/Create Editable HUD")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            DuelSceneSetup.EnsureScene();
            var existing = Object.FindFirstObjectByType<DuelHud>();
            if (existing != null) { Selection.activeGameObject = existing.gameObject; return; }
            Directory.CreateDirectory("Assets/UI/Icons"); AssetDatabase.Refresh();
            white = MakeSprite(-1); font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject("Duel UI (Canvas)", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800); scaler.matchWidthOrHeight = 0;
            var hud = root.AddComponent<DuelHud>();

            var player = Rect("Player Health", root.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(38, -38), new Vector2(320, 87));
            hud.PlayerHealth = Health(player, "AZURE", "PLAYER DRAGON", Blue, out _);
            var enemy = Rect("Opponent Health", root.transform, Vector2.one, Vector2.one, new Vector2(-38, -38), new Vector2(320, 87));
            hud.EnemyHealth = Health(enemy, "CINDER", "RIVAL / READY", Red, out var state); hud.EnemyState = state;
            var header = Rect("Match Header", root.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(440, 110));
            Label("Game Title", header, 0, 0, 440, 42, "E M B E R B O U N D", 24, Gold, TextAnchor.MiddleCenter, true);
            Label("Subtitle", header, 0, 43, 440, 22, "THE OBSIDIAN DUEL", 12, Muted, TextAnchor.MiddleCenter);
            hud.MatchTimer = Label("Match Timer", header, 0, 73, 440, 28, "00:00", 17, Color.white, TextAnchor.MiddleCenter);

            var abilities = Rect("Ability Bar", root.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 23), new Vector2(556, 96));
            for (int i = 0; i < 3; i++) hud.Abilities[i] = Ability(abilities, i, hud);
            var controls = Rect("Controls", root.transform, Vector2.zero, Vector2.zero, new Vector2(35, 29), new Vector2(295, 60));
            Label("Movement Help", controls, 0, 0, 295, 28, "WASD  MOVE    /    AUTO-FACE RIVAL", 12, Color.white);
            Label("Pause and Sound Help", controls, 0, 30, 295, 23, "ESC  PAUSE    /    M  SOUND", 11, Muted);
            var tagline = Rect("Tagline", root.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-35, 34), new Vector2(250, 40));
            Label("Tagline Text", tagline, 0, 0, 250, 40, "OUTPLAY. OUTLAST. ASCEND.", 11, Gold, TextAnchor.MiddleRight);
            var hint = Rect("Opening Hint", root.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 170), new Vector2(600, 36));
            Label("Hint Text", hint, 0, 0, 600, 36, "Watch the landing circle. Dodge the rival's Skyfall.", 16, Color.white, TextAnchor.MiddleCenter);
            hud.OpeningHint = hint.gameObject;

            hud.PausePanel = Modal(root.transform, "Pause Panel", "TAKE A BREATH", "PAUSED", "Your rival can wait.", "RESUME DUEL", out var resume, out _, out _);
            hud.ResumeButton = resume; UnityEventTools.AddPersistentListener(resume.onClick, hud.Resume);
            hud.WinnerPanel = Modal(root.transform, "Winner Panel", "THE DUEL IS DECIDED", "AZURE WINS", "The sanctuary is yours.", "PLAY AGAIN", out var restart, out var winnerTitle, out var winnerMessage);
            hud.RestartButton = restart; hud.WinnerTitle = winnerTitle; hud.WinnerMessage = winnerMessage;
            UnityEventTools.AddPersistentListener(restart.onClick, hud.Restart);
            hud.PausePanel.SetActive(false); hud.WinnerPanel.SetActive(false);
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("UI EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                events.transform.SetParent(root.transform, false);
                events.GetComponent<EventSystem>().sendNavigationEvents = false;
            }
            EditorUtility.SetDirty(hud);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, "Assets/UI/DuelHUD.prefab", InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(root.scene); EditorSceneManager.SaveScene(root.scene);
            AssetDatabase.SaveAssets(); Selection.activeGameObject = root;
            Debug.Log("EMBERBOUND_CANVAS_READY: Saved editable HUD prefab and scene instance.");
        }
        static DuelHud.HealthWidgets Health(RectTransform panel, string name, string subtitle, Color color, out Text state)
        {
            Paint(panel, Ink);
            ImageAt("Team Stripe", panel, 0, 0, 4, 87, color);
            var nameLabel = Label("Dragon Name", panel, 18, 6, 180, 28, name, 22, Color.white, bold: true);
            state = Label("Dragon Role", panel, 18, 34, 170, 19, subtitle, 11, Muted);
            var value = Label("Health Value", panel, 200, 14, 100, 28, "200 / 200", 14, color, TextAnchor.MiddleRight);
            ImageAt("Health Track", panel, 18, 65, 284, 6, new Color(0.18f, 0.23f, 0.26f));
            var fill = ImageAt("Health Fill", panel, 18, 65, 284, 6, color); Filled(fill);
            return new DuelHud.HealthWidgets { Fill = fill, Value = value, Name = nameLabel };
        }
        static DuelHud.AbilityWidgets Ability(RectTransform bar, int index, DuelHud hud)
        {
            string title = index == 0 ? "FIRE BREATH" : index == 1 ? "TAIL SWEEP" : "SKYFALL";
            var panel = At(title + " Button", bar, index * 190, 4, 176, 92); var background = Paint(panel, Ink); background.raycastTarget = true;
            var button = panel.gameObject.AddComponent<Button>(); StyleButton(button, background);
            UnityEventTools.AddIntPersistentListener(button.onClick, hud.UseAbility, index);
            ImageAt("Top Accent", panel, 0, 0, 176, 2, Gold);
            var icon = ImageAt("Ability Icon", panel, 8, 14, 48, 48, Gold); icon.sprite = MakeSprite(index); icon.preserveAspect = true;
            var overlay = ImageAt("Cooldown Overlay", panel, 8, 14, 48, 48, new Color(0.02f, 0.035f, 0.05f, 0.8f));
            var countdown = Label("Countdown", overlay.rectTransform, 0, 0, 48, 48, "3.5", 16, Color.white, TextAnchor.MiddleCenter, true);
            overlay.gameObject.SetActive(false);
            Label("Ability Name", panel, 60, 19, 112, 23, title, 12, Color.white, bold: true);
            var status = Label("Ready Status", panel, 60, 45, 110, 21, "READY", 12, Gold);
            Label("Key and Damage", panel, 10, 70, 160, 18, (index + 1) + " / " + (index == 0 ? "Q" : index == 1 ? "E" : "R") + "     " + CombatRules.Damage((Emberbound.Ability)index) + " DAMAGE", 10, Muted);
            var fill = ImageAt("Cooldown Progress", panel, 0, -4, 176, 3, Gold); Filled(fill); fill.fillAmount = 0;
            return new DuelHud.AbilityWidgets { Button = button, CooldownOverlay = overlay.gameObject, Countdown = countdown, Status = status, CooldownFill = fill };
        }
        static GameObject Modal(Transform parent, string name, string eyebrow, string title, string message, string action, out Button button, out Text heading, out Text description)
        {
            var overlay = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); overlay.transform.SetParent(parent, false);
            var rect = (RectTransform)overlay.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var shade = overlay.GetComponent<Image>(); shade.sprite = white; shade.color = new Color(0.01f, 0.02f, 0.035f, 0.78f); shade.raycastTarget = true;
            var card = Rect("Dialog Card", rect, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(500, 300)); Paint(card, Ink);
            ImageAt("Top Accent", card, 0, 0, 500, 3, Gold);
            Label("Eyebrow", card, 30, 26, 440, 30, eyebrow, 13, Gold, TextAnchor.MiddleCenter);
            heading = Label("Heading", card, 20, 71, 460, 58, title, 38, Color.white, TextAnchor.MiddleCenter, true);
            description = Label("Message", card, 30, 137, 440, 36, message, 17, Muted, TextAnchor.MiddleCenter);
            var actionRect = At("Action Button", card, 100, 207, 300, 48);
            var background = Paint(actionRect, new Color(0.18f, 0.23f, 0.27f)); background.raycastTarget = true;
            button = actionRect.gameObject.AddComponent<Button>(); StyleButton(button, background);
            Label("Button Label", actionRect, 0, 0, 300, 48, action, 18, Color.white, TextAnchor.MiddleCenter, true);
            return overlay;
        }
        static void StyleButton(Button button, Image target)
        {
            button.targetGraphic = target; var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f); colors.disabledColor = new Color(0.7f, 0.7f, 0.7f); button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
        }
        static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        static RectTransform At(string name, Transform parent, float x, float y, float width, float height) => Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height));
        static Image Paint(RectTransform rect, Color color) { var image = rect.gameObject.AddComponent<Image>(); image.sprite = white; image.color = color; image.raycastTarget = false; return image; }
        static Image ImageAt(string name, Transform parent, float x, float y, float width, float height, Color color) => Paint(At(name, parent, x, y, width, height), color);
        static void Filled(Image image) { image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.fillOrigin = 0; image.fillAmount = 1; }
        static Text Label(string name, Transform parent, float x, float y, float width, float height, string value, int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft, bool bold = false)
        {
            var text = At(name, parent, x, y, width, height).gameObject.AddComponent<Text>(); text.font = font; text.text = value; text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; text.color = color; text.alignment = alignment; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; return text;
        }
        static Sprite MakeSprite(int kind)
        {
            string name = kind < 0 ? "WhiteSquare" : kind == 0 ? "Fire" : kind == 1 ? "Tail" : "Skyfall";
            string path = "Assets/UI/Icons/" + name + ".png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false); var pixels = new Color[4096];
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    float u = (x - 32) / 28f, v = (y - 32) / 28f;
                    bool fill = kind < 0 || (kind == 0 ? v > -0.8f && v < 0.95f && Mathf.Abs(u + Mathf.Sin(v * 5) * 0.12f) < (0.95f - v) * 0.4f && u * u + v * v < 0.9f
                        : kind == 1 ? u * u + v * v < 0.78f && u * u + v * v > 0.28f && (u < 0.25f || v < 0)
                        : (Mathf.Abs(v - Mathf.Abs(u) * 0.7f + 0.2f) < 0.18f && Mathf.Abs(u) < 0.95f) || (Mathf.Abs(u) < 0.14f && v > -0.7f && v < 0.2f));
                    pixels[y * 64 + x] = fill ? Color.white : Color.clear;
                }
                texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single; importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        public static void GenerateAndBuild() { Create(); VerifyScreens(); BuildProject.BuildWindows(); }
        public static void VerifyScreens()
        {
            var hud = Object.FindFirstObjectByType<DuelHud>(); var canvas = hud.GetComponent<Canvas>();
            var preview = Object.FindFirstObjectByType<DuelScenePreview>(); var camera = preview.GetComponentInChildren<Camera>();
            Directory.CreateDirectory("output");
            var target = new RenderTexture(1280, 800, 24); var texture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 2;
                for (int state = 0; state < 3; state++)
                {
                    hud.PausePanel.SetActive(state == 1); hud.WinnerPanel.SetActive(state == 2);
                    Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                    texture.ReadPixels(new UnityEngine.Rect(0, 0, 1280, 800), 0, 0); texture.Apply();
                    File.WriteAllBytes("output/ui-" + (state == 0 ? "gameplay" : state == 1 ? "pause" : "winner") + ".png", texture.EncodeToPNG());
                }
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
                hud.PausePanel.SetActive(false); hud.WinnerPanel.SetActive(false);
                Object.DestroyImmediate(texture); Object.DestroyImmediate(target);
            }
            Debug.Log("EMBERBOUND_CANVAS_PREVIEWS_RENDERED");
        }
    }
}
