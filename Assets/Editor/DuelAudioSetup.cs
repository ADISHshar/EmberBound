using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emberbound.Editor
{
    public static class DuelAudioSetup
    {
        [MenuItem("Emberbound/Configure Collected Audio")]
        public static void Configure()
        {
            Directory.CreateDirectory("Assets/Resources"); AssetDatabase.Refresh();
            const string path = "Assets/Resources/DuelAudioSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<DuelAudioSettings>(path);
            if (settings == null) { settings = ScriptableObject.CreateInstance<DuelAudioSettings>(); AssetDatabase.CreateAsset(settings, path); }
            settings.Fire = Load("FireAttack", false); settings.Tail = Load("TailAttack", false);
            settings.Flight = Load("FlyAttackSound", false); settings.Music = Load("LoopingBGmusic", true);
            EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
            Debug.Log("EMBERBOUND_AUDIO_READY: All four collected clips assigned.");
        }
        static AudioClip Load(string name, bool music)
        {
            string path = "Assets/Audio/" + name + ".mp3";
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) throw new Exception("Audio file missing: " + path);
            var sample = importer.defaultSampleSettings;
            sample.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            sample.preloadAudioData = !music;
            importer.defaultSampleSettings = sample; importer.SaveAndReimport();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            Debug.Log("AUDIO " + name + " length=" + clip.length.ToString("0.00") + "s");
            return clip;
        }
        public static void ConfigureAndBuild() { Configure(); BuildProject.BuildWindows(); }
    }
}
