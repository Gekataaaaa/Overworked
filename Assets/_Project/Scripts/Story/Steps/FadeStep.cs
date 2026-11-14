using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>Затемнение экрана или выход из него (FadeController).</summary>
    [Serializable]
    public class FadeStep : SequenceStep
    {
        [Tooltip("true — в чёрное, false — из чёрного")]
        public bool ToBlack = true;

        [Tooltip("Длительность, секунды")]
        public float Seconds = 0.5f;

        public override string Describe() => $"Fade {(ToBlack ? "out" : "in")} {Seconds:0.##}s";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            var fade = FadeController.Instance;
            if (fade == null) yield break;
            if (ToBlack) yield return ScriptedSequence.WaitCallback(cb => fade.FadeOut(Seconds, cb));
            else yield return ScriptedSequence.WaitCallback(cb => fade.FadeIn(Seconds, cb));
        }
    }
}
