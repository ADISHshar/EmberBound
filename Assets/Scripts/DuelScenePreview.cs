using UnityEngine;

namespace Emberbound
{
    // Serialized edit-mode scenery. Runtime creates fresh actors and removes this preview.
    public sealed class DuelScenePreview : MonoBehaviour
    {
        public static string PlayerTitle { get; private set; } = "AZURE";
        public static string EnemyTitle { get; private set; } = "CINDER";
        public static void ResetTitles() { PlayerTitle = "AZURE"; EnemyTitle = "CINDER"; }
        void Awake()
        {
            foreach (var actor in GetComponentsInChildren<DragonActor>(true))
            {
                if (actor.IsPlayer) PlayerTitle = actor.Title;
                else EnemyTitle = actor.Title;
            }
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
