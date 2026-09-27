using UnityEngine;

namespace Emberbound
{
    public static class ArenaBuilder
    {
        public static Transform Build()
        {
            var root = new GameObject("Arena | Obsidian sanctuary").transform;
            var stone = DuelEffects.Material(new Color(0.14f, 0.19f, 0.22f));
            var rim = DuelEffects.Material(new Color(0.24f, 0.3f, 0.32f));
            var dark = DuelEffects.Material(new Color(0.055f, 0.085f, 0.11f));
            var gold = DuelEffects.Material(new Color(0.65f, 0.4f, 0.14f));
            Piece("Floating foundation", PrimitiveType.Cylinder, new Vector3(0, -0.7f, 0), new Vector3(24, 0.6f, 24), dark, root);
            Piece("Arena floor", PrimitiveType.Cylinder, new Vector3(0, -0.12f, 0), new Vector3(22, 0.12f, 22), stone, root);
            for (int i = 0; i < 48; i++)
            {
                float angle = i * Mathf.PI * 2 / 48;
                Vector3 position = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                var wall = Piece("Boundary stone", PrimitiveType.Cube, position * 11 + Vector3.up * 0.35f, new Vector3(1.38f, 0.7f, 0.55f), rim, root);
                wall.transform.rotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg + 90, 0);
                if (i % 6 == 0)
                {
                    Piece("Obelisk", PrimitiveType.Cube, position * 11.8f + Vector3.up * 1.2f, new Vector3(0.7f, 2.8f, 0.7f), dark, root);
                    Piece("Crown", PrimitiveType.Sphere, position * 11.8f + Vector3.up * 2.75f, Vector3.one * 0.35f, DuelEffects.Material(new Color(1, 0.48f, 0.08f), true), root);
                    var lamp = new GameObject("Warm brazier light").AddComponent<Light>(); lamp.transform.SetParent(root); lamp.transform.position = position * 11.4f + Vector3.up * 3; lamp.type = LightType.Point; lamp.color = new Color(1, 0.5f, 0.15f); lamp.intensity = 2.5f; lamp.range = 6;
                }
            }
            DuelEffects.Ring(Vector3.zero, 8.7f, new Color(0.34f, 0.38f, 0.34f), -1, root);
            DuelEffects.Ring(Vector3.zero, 3.5f, new Color(0.44f, 0.33f, 0.16f), -1, root);
            DuelEffects.Ring(Vector3.zero, 3.8f, new Color(0.3f, 0.27f, 0.18f), -1, root);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30;
                var mark = Piece("Floor inlay", PrimitiveType.Cube, Quaternion.Euler(0, angle, 0) * new Vector3(0, 0.02f, 6.2f), new Vector3(0.045f, 0.018f, 4.5f), gold, root);
                mark.transform.rotation = Quaternion.Euler(0, angle, 0); Remove(mark.GetComponent<Collider>());
            }
            RenderSettings.ambientLight = new Color(0.38f, 0.44f, 0.52f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.035f, 0.055f, 0.08f); RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 35; RenderSettings.fogEndDistance = 75;
            var sun = new GameObject("Moonlight").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.5f; sun.color = new Color(0.7f, 0.83f, 1); sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(48, -35, 0);
            sun.transform.SetParent(root);
            return root;
        }
        public static void Remove(Object value)
        {
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }
        static GameObject Piece(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent); go.transform.position = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material;
            if (type == PrimitiveType.Cylinder)
            {
                // Unity's primitive cylinder uses a capsule collider. Flattening it creates
                // an oversized rounded collision surface that pushes actors off the floor.
                var primitiveCollider = go.GetComponent<Collider>();
                primitiveCollider.enabled = false;
                Remove(primitiveCollider);
                go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            }
            return go;
        }
    }
}
