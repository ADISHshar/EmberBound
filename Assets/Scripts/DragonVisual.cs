using UnityEngine;

namespace Emberbound
{
    public sealed class DragonVisual : MonoBehaviour
    {
        DragonActor actor;
        Transform model, leftWing, rightWing, tail;
        Renderer[] renderers;
        MaterialPropertyBlock block;
        float flashUntil;
        bool importedModel;
        public void Build(DragonActor owner)
        {
            actor = owner;
            model = new GameObject("Dragon model").transform; model.SetParent(transform, false);
            var skin = DuelEffects.Material(owner.Tint);
            var horn = DuelEffects.Material(new Color(0.95f, 0.83f, 0.59f));
            var membrane = DuelEffects.Material(owner.Tint * 0.55f);
            Part("Body", model, new Vector3(0, 0.95f, 0), new Vector3(1.05f, 0.85f, 1.8f), skin);
            Part("Chest", model, new Vector3(0, 1.18f, 0.6f), new Vector3(0.9f, 1, 0.9f), skin);
            Part("Head", model, new Vector3(0, 1.62f, 1.1f), new Vector3(0.8f, 0.65f, 0.95f), skin);
            Part("Muzzle", model, new Vector3(0, 1.43f, 1.65f), new Vector3(0.68f, 0.34f, 0.65f), skin);
            for (int side = -1; side <= 1; side += 2)
            {
                Part("Eye", model, new Vector3(side * 0.38f, 1.72f, 1.4f), Vector3.one * 0.16f, DuelEffects.Material(new Color(1, 0.9f, 0.25f), true));
                var h = Part("Horn", model, new Vector3(side * 0.33f, 2.02f, 0.9f), new Vector3(0.13f, 0.65f, 0.15f), horn);
                h.localRotation = Quaternion.Euler(-28, 0, side * -20);
                for (int z = -1; z <= 1; z += 2)
                {
                    Part("Leg", model, new Vector3(side * 0.55f, 0.48f, z * 0.6f), new Vector3(0.32f, 0.65f, 0.38f), skin);
                    Part("Claw", model, new Vector3(side * 0.55f, 0.2f, z * 0.6f + 0.18f), new Vector3(0.4f, 0.2f, 0.5f), horn);
                }
                Transform wing = new GameObject(side < 0 ? "Left wing" : "Right wing").transform;
                wing.SetParent(model, false); wing.localPosition = new Vector3(side * 0.45f, 1.35f, 0.15f);
                var mesh = new Mesh { name = "Original dragon wing" };
                mesh.vertices = new[] { Vector3.zero, new Vector3(side * 2.4f, 0.25f, -0.35f), new Vector3(side * 1.9f, 0, -1.15f), new Vector3(side * 0.8f, -0.1f, -0.7f) };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 2, 1, 0, 3, 2, 0 }; mesh.RecalculateNormals();
                wing.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                wing.gameObject.AddComponent<MeshRenderer>().sharedMaterial = membrane;
                if (side < 0) leftWing = wing; else rightWing = wing;
            }
            tail = new GameObject("Tail pivot").transform; tail.SetParent(model, false); tail.localPosition = new Vector3(0, 0.8f, -0.7f);
            for (int i = 0; i < 5; i++)
                Part("Tail segment", tail, new Vector3(0, -i * 0.075f, -i * 0.36f), new Vector3(0.5f - i * 0.08f, 0.4f - i * 0.055f, 0.6f), skin);
            for (int i = 0; i < 5; i++)
                Part("Spine", model, new Vector3(0, 1.46f, 0.45f - i * 0.3f), new Vector3(0.14f, 0.35f, 0.18f), horn);
            // A configured Resources/DragonSkin replaces the temporary original mesh.
            DragonSkin skinConfig = DragonSkin.ForTeam(owner.IsPlayer);
            if (skinConfig != null && skinConfig.Prefab != null)
            {
                var oldChildren = new System.Collections.Generic.List<GameObject>();
                foreach (Transform child in model) oldChildren.Add(child.gameObject);
                foreach (var child in oldChildren) ArenaBuilder.Remove(child);
                var imported = Instantiate(skinConfig.Prefab, model);
                importedModel = true;
                imported.transform.localPosition = skinConfig.Offset;
                imported.transform.localRotation = Quaternion.Euler(skinConfig.Rotation);
                imported.transform.localScale = Vector3.one * skinConfig.Scale;
                foreach (var c in imported.GetComponentsInChildren<Collider>()) c.enabled = false;
                foreach (var rb in imported.GetComponentsInChildren<Rigidbody>()) { rb.isKinematic = true; rb.detectCollisions = false; }
                if (Application.isPlaying)
                {
                    var adapter = imported.AddComponent<DragonAnimationAdapter>(); adapter.Initialize(actor, skinConfig);
                }
                else if (skinConfig.Idle != null)
                {
                    skinConfig.Idle.SampleAnimation(imported, 0);
                    foreach (var animator in imported.GetComponentsInChildren<Animator>()) animator.enabled = false;
                }
                leftWing = rightWing = tail = null;
            }
            renderers = model.GetComponentsInChildren<Renderer>(); block = new MaterialPropertyBlock();
            DuelEffects.Ring(transform.position, 1, owner.Tint, -1, transform);
        }
        static Transform Part(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Sphere); part.name = name;
            ArenaBuilder.Remove(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false); part.transform.localPosition = position; part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part.transform;
        }
        public void Flash() => flashUntil = Time.time + 0.15f;
        void LateUpdate()
        {
            if (model == null) return;
            float phase = actor.AnimationPhase;
            float height = actor.Airborne ? Mathf.Sin(Mathf.Clamp01(phase / DragonActor.LandingPhase) * Mathf.PI) * 3.8f : Mathf.Sin(Time.time * 7) * 0.045f * actor.Moving;
            model.localPosition = Vector3.up * height;
            float roll = actor.Combat.Alive || importedModel ? 0 : 75;
            float spin = actor.Busy && actor.ActiveAbility == Ability.Tail ? phase * 360 : 0;
            model.localRotation = Quaternion.Euler(0, spin, roll);
            if (leftWing != null)
            {
                float flap = Mathf.Sin(Time.time * (actor.Airborne ? 19 : 3)) * (actor.Airborne ? 40 : 9);
                leftWing.localRotation = Quaternion.Euler(0, 0, flap);
                rightWing.localRotation = Quaternion.Euler(0, 0, -flap);
                tail.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 4) * 16, 0);
            }
            block.SetColor("_EmissionColor", Time.time < flashUntil ? Color.white * 1.5f : Color.black);
            foreach (var renderer in renderers) if (renderer != null) renderer.SetPropertyBlock(block);
        }
    }
}
