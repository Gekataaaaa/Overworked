using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>Запустить другой сценарий (общий кусок нескольких сцен).</summary>
    [Serializable]
    public class PlaySequenceStep : SequenceStep
    {
        [Tooltip("Сценарий в сцене")]
        public ScriptedSequence Sequence;

        [Tooltip("Ждать, пока он закончится")]
        public bool Wait = true;

        public override string Describe() => $"Play sequence: {NameOf(Sequence)}";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (Sequence == null || Sequence == sequence) yield break;
            if (Wait) yield return ScriptedSequence.WaitCallback(cb => Sequence.Play(cb));
            else Sequence.Play();
        }
    }
}
