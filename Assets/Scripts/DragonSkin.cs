using UnityEngine;

namespace Emberbound
{
    [CreateAssetMenu(menuName = "Emberbound/Dragon Skin")]
    public sealed class DragonSkin : ScriptableObject
    {
        public GameObject Prefab;
        public float Scale = 1;
        public Vector3 Offset;
        public Vector3 Rotation;
        public AnimationClip Idle, Walk, Fire, Tail, Flight, Death;
        public static DragonSkin ForTeam(bool player)
        {
            var team = Resources.Load<DragonSkin>(player ? "PlayerDragonSkin" : "EnemyDragonSkin");
            return team != null ? team : Resources.Load<DragonSkin>("DragonSkin");
        }
    }
}
