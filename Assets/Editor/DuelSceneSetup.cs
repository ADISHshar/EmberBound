using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Emberbound.Editor
{
    [InitializeOnLoad]
    public static class DuelSceneSetup
    {
        public const string ScenePath = "Assets/Scenes/Duel.unity";
        static DuelSceneSetup()
        {
            EditorSceneManager.sceneOpened += OnOpened;
            EditorApplication.delayCall += InitializeEmptyScene;
        }
        static void OnOpened(Scene scene, OpenSceneMode mode) => InitializeEmptyScene();
        static void InitializeEmptyScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            var scene = SceneManager.GetActiveScene();
            if (scene.path == ScenePath && scene.rootCount == 0) Populate(scene);
        }
        [MenuItem("Emberbound/Open Duel Scene")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            Frame();
        }
        public static void EnsureScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Scene scene;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == ScenePath)
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            else if (File.Exists(ScenePath)) scene = EditorSceneManager.OpenScene(ScenePath);
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            if (scene.rootCount == 0) Populate(scene);
        }
        static void Populate(Scene scene)
        {
            var root = new GameObject("DUEL PREVIEW | Press Play to fight");
            root.AddComponent<DuelScenePreview>();
            ArenaBuilder.Build().SetParent(root.transform);
            PreviewDragon(root.transform, true, new Vector3(-4, 0, -2), new Color(0.15f, 0.72f, 0.71f), "AZURE");
            PreviewDragon(root.transform, false, new Vector3(4, 0, 2), new Color(0.8f, 0.22f, 0.15f), "CINDER");
            var camera = new GameObject("Tactical camera | Editor preview").AddComponent<Camera>();
            camera.transform.SetParent(root.transform); camera.transform.position = new Vector3(0, 22, -19);
            camera.transform.rotation = Quaternion.Euler(49, 0, 0); camera.orthographic = true; camera.orthographicSize = 13.7f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.035f, 0.055f, 0.08f);
            // Persist generated meshes/materials: the scene must survive closing and reopening Unity.
            Directory.CreateDirectory("Assets/Generated"); AssetDatabase.Refresh();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                foreach (var material in renderer.sharedMaterials)
                    if (material != null && !AssetDatabase.Contains(material))
                        AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath("Assets/Generated/ArenaMaterial.mat"));
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
                if (filter.sharedMesh != null && filter.sharedMesh.name == "Original dragon wing" && !AssetDatabase.Contains(filter.sharedMesh))
                    AssetDatabase.CreateAsset(filter.sharedMesh, AssetDatabase.GenerateUniqueAssetPath("Assets/Generated/DragonWing.asset"));
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Frame();
            Debug.Log("EMBERBOUND_SCENE_READY: Saved arena, dragons, lights and preview camera.");
        }
        static void PreviewDragon(Transform parent, bool player, Vector3 position, Color tint, string name)
        {
            var go = new GameObject(name + " | Model preview"); go.transform.SetParent(parent); go.transform.position = position;
            go.transform.rotation = Quaternion.LookRotation(-position);
            var actor = go.AddComponent<DragonActor>(); actor.enabled = false; actor.IsPlayer = player; actor.Tint = tint; actor.Title = name;
            var visual = go.AddComponent<DragonVisual>(); visual.Build(actor); visual.enabled = false;
        }
        public static void RefreshDragonModels()
        {
            var preview = Object.FindFirstObjectByType<DuelScenePreview>();
            if (preview == null) return;
            foreach (var actor in preview.GetComponentsInChildren<DragonActor>())
            {
                var oldVisual = actor.GetComponent<DragonVisual>();
                if (oldVisual != null) Object.DestroyImmediate(oldVisual);
                // Replace only the generated model/ring children, preserving the arena and actor positions.
                for (int i = actor.transform.childCount - 1; i >= 0; i--)
                {
                    var child = actor.transform.GetChild(i);
                    if (child.name == "Dragon model" || child.name == "Ability ring") Object.DestroyImmediate(child.gameObject);
                }
                var visual = actor.gameObject.AddComponent<DragonVisual>(); visual.Build(actor); visual.enabled = false;
            }
            foreach (var renderer in preview.GetComponentsInChildren<Renderer>())
                foreach (var material in renderer.sharedMaterials)
                    if (material != null && !AssetDatabase.Contains(material))
                        AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath("Assets/Generated/TeamRing.mat"));
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            EditorSceneManager.SaveScene(preview.gameObject.scene);
        }
        static void Frame()
        {
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(Vector3.zero, Quaternion.Euler(49, 0, 0), 19, true);
        }
        public static void VerifyPreview()
        {
            EnsureScene();
            var preview = Object.FindFirstObjectByType<DuelScenePreview>();
            if (preview == null || preview.GetComponentsInChildren<Renderer>().Length < 50)
                throw new System.Exception("Scene preview is missing geometry.");
            var camera = preview.GetComponentInChildren<Camera>();
            var target = new RenderTexture(1280, 800, 24);
            var texture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0); texture.Apply();
                Directory.CreateDirectory("output"); File.WriteAllBytes("output/editor-preview.png", texture.EncodeToPNG());
                Debug.Log("EMBERBOUND_PREVIEW_VERIFIED");
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                Object.DestroyImmediate(texture); Object.DestroyImmediate(target);
            }
        }
    }
}
