using System;

namespace Emberbound
{
    public enum Ability { Fire, Tail, Flight }

    // Shared, engine-independent rules: AI cannot bypass player cooldowns.
    public static class CombatRules
    {
        public const float MaxHealth = 200;
        public const float ArenaRadius = 10;
        public static float Damage(Ability ability) => ability == Ability.Fire ? 24 : ability == Ability.Tail ? 32 : 42;
        public static float Cooldown(Ability ability) => ability == Ability.Fire ? 3.5f : ability == Ability.Tail ? 5 : 9;
        public static float Range(Ability ability) => ability == Ability.Fire ? 6.5f : ability == Ability.Tail ? 3 : 7;
        public static bool InCone(float distance, float forwardDot) => distance <= Range(Ability.Fire) && forwardDot >= 0.78f;
    }

    public sealed class CombatState
    {
        readonly float[] readyAt = new float[3];
        public float Health { get; private set; } = CombatRules.MaxHealth;
        public bool Alive => Health > 0;
        public float Remaining(Ability ability, float now) => Math.Max(0, readyAt[(int)ability] - now);
        public bool TryCast(Ability ability, float now)
        {
            if (!Alive || Remaining(ability, now) > 0) return false;
            readyAt[(int)ability] = now + CombatRules.Cooldown(ability);
            return true;
        }
        public float Hurt(float amount)
        {
            float applied = Math.Min(Health, Math.Max(0, amount));
            Health -= applied;
            return applied;
        }
    }
}
