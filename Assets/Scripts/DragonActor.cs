using System.Collections;
using UnityEngine;

namespace Emberbound
{
    public sealed class DragonActor : MonoBehaviour
    {
        public const float LandingPhase = 0.82f;
        public CombatState Combat { get; private set; } = new CombatState();
        public DragonActor Opponent;
        public bool IsPlayer;
        public bool Automated;
        public Color Tint;
        public string Title;
        public string StateLabel { get; private set; } = "Ready";
        public bool Busy { get; private set; }
        public bool Airborne { get; private set; }
        public float AnimationPhase { get; private set; }
        public Ability ActiveAbility { get; private set; }
        public float Moving { get; private set; }
        public DragonVisual Visual { get; private set; }
        Vector3 impulse;
        float nextDecision = 1.5f;
        Rigidbody body;

        public void Initialize(bool player, Color tint, string title)
        {
            IsPlayer = player; Tint = tint; Title = title;
            body = gameObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var collider = gameObject.AddComponent<SphereCollider>(); collider.radius = 0.7f; collider.center = Vector3.up * 0.7f;
            Visual = gameObject.AddComponent<DragonVisual>(); Visual.Build(this);
        }

        void Update()
        {
            if (DuelGame.Instance.Finished || DuelGame.Instance.Paused || !Combat.Alive) return;
            if (Opponent != null) Face(Opponent.transform.position - transform.position);
            if (IsPlayer && !Automated)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Q)) Cast(Ability.Fire);
                if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.E)) Cast(Ability.Tail);
                if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.R)) Cast(Ability.Flight);
            }
            else Think();
        }

        void FixedUpdate()
        {
            if (!Combat.Alive || DuelGame.Instance.Finished) { body.linearVelocity = Vector3.zero; return; }
            Vector3 movement = Vector3.zero;
            if (!Busy && !DuelGame.Instance.Paused)
            {
                if (IsPlayer && !Automated)
                {
                    movement = new Vector3((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0), 0,
                        (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0)).normalized;
                }
                else
                {
                    Vector3 delta = Opponent.transform.position - transform.position;
                    float distance = delta.magnitude;
                    if (distance > 4.2f) movement = delta.normalized;
                    else if (distance < 2 && Combat.Remaining(Ability.Tail, Time.time) > 0) movement = -delta.normalized;
                    else movement = Vector3.Cross(delta.normalized, Vector3.up) * 0.6f;
                    StateLabel = distance > 4.2f ? "Chase" : "Circle";
                }
            }
            Moving = movement.magnitude;
            impulse = Vector3.MoveTowards(impulse, Vector3.zero, Time.fixedDeltaTime * 10);
            Vector3 velocity = movement * 4.1f + impulse;
            Vector3 next = body.position + velocity * Time.fixedDeltaTime;
            Vector3 flat = new Vector3(next.x, 0, next.z);
            if (flat.magnitude > CombatRules.ArenaRadius)
                velocity = (Vector3.ClampMagnitude(flat, CombatRules.ArenaRadius) - body.position) / Time.fixedDeltaTime;
            body.linearVelocity = velocity;
        }

        void Face(Vector3 direction)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion rotation = Quaternion.LookRotation(direction);
                body.rotation = rotation;
                transform.rotation = rotation;
            }
        }

        void Think()
        {
            if (Busy || Time.time < nextDecision) return;
            nextDecision = Time.time + 0.28f;
            float distance = Vector3.Distance(transform.position, Opponent.transform.position);
            if (distance < 2.9f && Cast(Ability.Tail)) return;
            if (distance > 3 && distance < 7 && Cast(Ability.Flight)) return;
            if (distance < 6.5f) Cast(Ability.Fire);
        }

        public bool Cast(Ability ability)
        {
            if (Busy || DuelGame.Instance.Finished || DuelGame.Instance.Paused || !Combat.TryCast(ability, Time.time)) return false;
            StartCoroutine(Attack(ability));
            return true;
        }

        IEnumerator Attack(Ability ability)
        {
            Busy = true; ActiveAbility = ability; StateLabel = ability.ToString();
            float duration = ability == Ability.Flight ? 1.65f : ability == Ability.Fire ? 0.85f : 0.65f;
            Vector3 origin = transform.position;
            Vector3 target = origin + Vector3.ClampMagnitude(Opponent.transform.position - origin, CombatRules.Range(ability));
            target = Vector3.ClampMagnitude(target, CombatRules.ArenaRadius);
            bool hit = false;
            Airborne = ability == Ability.Flight;
            if (Airborne) DuelEffects.Ring(target, 2.8f, new Color(1, 0.35f, 0.2f), duration);
            if (Airborne) DuelGame.Instance.Audio.PlayAbility(ability);
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                if (!Combat.Alive || DuelGame.Instance.Finished) break;
                Face(Opponent.transform.position - transform.position);
                AnimationPhase = elapsed / duration;
                if (Airborne)
                {
                    body.position = Vector3.Lerp(origin, target, Mathf.SmoothStep(0, 1, Mathf.Clamp01(AnimationPhase / LandingPhase)));
                    body.linearVelocity = Vector3.zero;
                }
                if (!hit && AnimationPhase >= (ability == Ability.Flight ? LandingPhase : 0.35f))
                {
                    hit = true;
                    Vector3 delta = Opponent.transform.position - transform.position;
                    bool connects;
                    if (ability == Ability.Fire)
                    {
                        Vector3 direction = transform.forward;
                        DuelGame.Instance.Audio.PlayAbility(ability);
                        DuelEffects.Flame(transform.position + Vector3.up * 1.1f + direction, direction);
                        connects = CombatRules.InCone(delta.magnitude, Vector3.Dot(direction, delta.normalized));
                    }
                    else if (ability == Ability.Tail)
                    {
                        DuelGame.Instance.Audio.PlayAbility(ability);
                        DuelEffects.Ring(transform.position, 3, Tint, 0.45f);
                        connects = delta.magnitude <= CombatRules.Range(ability);
                    }
                    else
                    {
                        Airborne = false;
                        body.position = target;
                        DuelGame.Instance.TriggerShake(0.48f, 0.42f);
                        DuelEffects.Sound(55, 0.32f);
                        DuelEffects.Burst(target + Vector3.up * 0.2f, new Color(1, 0.65f, 0.18f), 55);
                        DuelEffects.Ring(target, 2.8f, Color.yellow, 0.45f);
                        connects = Vector3.Distance(Opponent.transform.position, target) <= 2.8f;
                    }
                    if (connects) Opponent.TakeHit(CombatRules.Damage(ability), delta.normalized * (ability == Ability.Tail ? 7 : 4));
                }
                yield return null;
            }
            Busy = false; Airborne = false; AnimationPhase = 0; StateLabel = "Ready";
        }

        public void TakeHit(float damage, Vector3 push)
        {
            if (!Combat.Alive || DuelGame.Instance.Finished || Airborne) return;
            float applied = Combat.Hurt(damage);
            impulse += push;
            Visual.Flash();
            DuelEffects.Burst(transform.position + Vector3.up, Tint, 18);
            DuelEffects.Popup(transform.position + Vector3.up * 2.5f, applied.ToString("0"), Color.white);
            DuelEffects.Sound(70, 0.15f);
            DuelGame.Instance.TriggerShake(0.12f, 0.15f);
            if (!Combat.Alive) DuelGame.Instance.End(Opponent);
        }
    }
}
