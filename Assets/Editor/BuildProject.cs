using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Emberbound.Editor
{
    public static class BuildProject
    {
        [MenuItem("Emberbound/Build Windows")]
        public static void BuildWindows()
        {
            Prepare();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Duel.unity" },
                locationPathName = "Builds/Windows/Emberbound.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed: " + report.summary.result);
            Debug.Log("EMBERBOUND_BUILD_OK: " + report.summary.totalSize + " bytes");
        }
        [MenuItem("Emberbound/Prepare Scene")]
        public static void Prepare()
        {
            DuelSceneSetup.EnsureScene();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Duel.unity", true) };
            PlayerSettings.companyName = "Emberbound"; PlayerSettings.productName = "Emberbound";
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 800;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            // Runtime-created materials still need their shaders included in the player.
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders = settings.FindProperty("m_AlwaysIncludedShaders");
            // Unity owns GUI/Text Shader internally; forcing it into this list crashes shader stripping.
            for (int i = shaders.arraySize - 1; i >= 0; i--)
            {
                var existing = shaders.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if (existing != null && existing.name == "GUI/Text Shader")
                {
                    shaders.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    shaders.DeleteArrayElementAtIndex(i);
                }
            }
            foreach (string name in new[] { "Standard", "Particles/Standard Unlit" })
            {
                Shader shader = Shader.Find(name); bool found = false;
                for (int i = 0; i < shaders.arraySize; i++) if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) found = true;
                if (!found && shader != null) { shaders.InsertArrayElementAtIndex(shaders.arraySize); shaders.GetArrayElementAtIndex(shaders.arraySize - 1).objectReferenceValue = shader; }
            }
            settings.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets();
        }
    }
}
