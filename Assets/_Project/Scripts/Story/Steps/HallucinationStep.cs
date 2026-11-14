using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>Галлюцинация (HallucinationManager, P4): звук, визуал, спайк пульса, новое значение рассудка.</summary>
    [Serializable]
    public class HallucinationStep : SequenceStep
    {
        [Tooltip("Галлюцинация в сцене (папка Hallucinations)")]
        public HallucinationEvent Hallucination;

        [Tooltip("Ждать окончания галлюцинации")]
        public bool Wait = true;

        public override string Describe() => $"Hallucination: {NameOf(Hallucination)}";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (Hallucination == null || HallucinationManager.Instance == null) yield break;
            if (Wait) yield return ScriptedSequence.WaitCallback(cb => HallucinationManager.Instance.Play(Hallucination, cb));
            else HallucinationManager.Instance.Play(Hallucination);
        }
    }
}
