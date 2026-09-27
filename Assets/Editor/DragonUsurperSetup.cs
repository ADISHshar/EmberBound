using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Emberbound.Editor
{
    public static class DragonUsurperSetup
    {
        const string Pack = "Assets/FourEvilDragonsPBR/";
        [MenuItem("Emberbound/Configure DragonUsurper Teams")]
        public static void Configure()
        {
            Directory.CreateDirectory("Assets/Resources"); AssetDatabase.Refresh();
            ConfigureTeam("PlayerDragonSkin", "Blue");
            ConfigureTeam("EnemyDragonSkin", "Red");
            AssetDatabase.SaveAssets();
            DuelSceneSetup.EnsureScene();
            DuelSceneSetup.RefreshDragonModels();
            Debug.Log("EMBERBOUND_USURPER_READY: Blue player, Red opponent; both skins and preview saved.");
        }
        static AnimationClip Clip(string file)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(Pack + "Animations/DragonUsurper/" + file + ".fbx")
                .OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null) throw new Exception("Missing DragonUsurper clip: " + file);
            return clip;
        }
        static void ConfigureTeam(string resource, string color)
        {
            var path = "Assets/Resources/" + resource + ".asset";
            var skin = AssetDatabase.LoadAssetAtPath<DragonSkin>(path);
            if (skin == null) { skin = ScriptableObject.CreateInstance<DragonSkin>(); AssetDatabase.CreateAsset(skin, path); }
            skin.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "Prefab/DragonUsurper/" + color + ".prefab");
            if (skin.Prefab == null) throw new Exception("Missing DragonUsurper prefab: " + color);
            skin.Idle = Clip("idle01"); skin.Walk = Clip("Walk"); skin.Fire = Clip("attackFlame");
            skin.Tail = Clip("attackHand"); skin.Flight = Clip("FlyFlame"); skin.Death = Clip("Die");
            var sample = UnityEngine.Object.Instantiate(skin.Prefab);
            try
            {
                sample.transform.position = Vector3.zero; sample.transform.rotation = Quaternion.identity; sample.transform.localScale = Vector3.one;
                skin.Idle.SampleAnimation(sample, 0);
                var renderers = sample.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) throw new Exception("Dragon prefab contains no renderer.");
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                skin.Scale = Mathf.Min(5.2f / Mathf.Max(bounds.size.x, 0.01f), 4.5f / Mathf.Max(bounds.size.z, 0.01f));
                skin.Offset = new Vector3(0, -bounds.min.y * skin.Scale, 0); skin.Rotation = Vector3.zero;
                Debug.Log("USURPER " + color + " bounds=" + bounds + " scale=" + skin.Scale + " offset=" + skin.Offset);
            }
            finally { UnityEngine.Object.DestroyImmediate(sample); }
            EditorUtility.SetDirty(skin);
        }
        public static void ConfigureAndVerify()
        {
            Configure();
            DuelSceneSetup.VerifyPreview();
            BuildProject.BuildWindows();
        }
    }
}
