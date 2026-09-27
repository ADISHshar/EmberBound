using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Emberbound
{
    // Plays explicitly mapped imported clips without depending on vendor Animator parameter names.
    public sealed class DragonAnimationAdapter : MonoBehaviour
    {
        DragonActor actor;
        DragonSkin skin;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        readonly AnimationClip[] clips = new AnimationClip[6];
        readonly AnimationClipPlayable[] players = new AnimationClipPlayable[6];
        int current = -1;
        public void Initialize(DragonActor owner, DragonSkin config)
        {
            actor = owner; skin = config;
            var animator = GetComponentInChildren<Animator>();
            if (animator == null) animator = gameObject.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = null;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var legacy = GetComponentInChildren<Animation>(); if (legacy != null) legacy.enabled = false;
            graph = PlayableGraph.Create("Dragon clip transitions");
            mixer = AnimationMixerPlayable.Create(graph, 6);
            var output = AnimationPlayableOutput.Create(graph, "Dragon", animator); output.SetSourcePlayable(mixer);
            clips[0] = skin.Idle; clips[1] = skin.Walk; clips[2] = skin.Fire; clips[3] = skin.Tail; clips[4] = skin.Flight; clips[5] = skin.Death;
            for (int i = 0; i < clips.Length; i++) if (clips[i] != null)
            {
                players[i] = AnimationClipPlayable.Create(graph, clips[i]); graph.Connect(players[i], 0, mixer, i);
            }
            graph.Play();
        }
        void Update()
        {
            if (!graph.IsValid()) return;
            int next = !actor.Combat.Alive ? 5 : actor.Busy ? 2 + (int)actor.ActiveAbility : actor.Moving > 0.1f ? 1 : 0;
            if (clips[next] == null) next = 0;
            if (next != current) { current = next; if (players[next].IsValid()) players[next].SetTime(0); }
            if (actor.Busy && current >= 2 && current <= 4 && players[current].IsValid())
            {
                // The supplied clips have different lengths; align their full motion with gameplay timing.
                players[current].SetSpeed(0);
                players[current].SetTime(actor.AnimationPhase * clips[current].length);
            }
            for (int i = 0; i < 6; i++)
            {
                mixer.SetInputWeight(i, Mathf.MoveTowards(mixer.GetInputWeight(i), i == current ? 1 : 0, Time.deltaTime * 9));
                if (i < 2 && players[i].IsValid() && players[i].GetTime() >= clips[i].length) players[i].SetTime(0);
                if (i == 5 && players[i].IsValid() && players[i].GetTime() >= clips[i].length)
                { players[i].SetTime(clips[i].length); players[i].SetSpeed(0); }
            }
        }
        void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }
    }
}
