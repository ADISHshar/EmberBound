using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emberbound.Editor
{
    public static class DragonSkinSetup
    {
        [MenuItem("Emberbound/Use Selected Dragon Prefab")]
        public static void Configure()
        {
            var prefab = Selection.activeObject as GameObject;
            if (prefab == null || !AssetDatabase.Contains(prefab))
            {
                EditorUtility.DisplayDialog("Select a dragon", "Select the imported dragon prefab or FBX in the Project window first.", "OK"); return;
            }
            Directory.CreateDirectory("Assets/Resources");
            var config = AssetDatabase.LoadAssetAtPath<DragonSkin>("Assets/Resources/DragonSkin.asset");
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<DragonSkin>();
                AssetDatabase.CreateAsset(config, "Assets/Resources/DragonSkin.asset");
            }
            config.Prefab = prefab;
            EditorUtility.SetDirty(config); AssetDatabase.SaveAssets();
            Selection.activeObject = config;
            EditorUtility.DisplayDialog("Dragon skin created", "Set Scale, Offset and Rotation so the dragon is about 3 units long and faces +Z. Assign the six animation clips in the Inspector. Clip names are intentionally not guessed.", "OK");
        }
    }
}
