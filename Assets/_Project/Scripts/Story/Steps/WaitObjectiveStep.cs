using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>Ждать, пока задание выполнят (игрок подобрал пропуск и т. п.).</summary>
    [Serializable]
    public class WaitObjectiveStep : SequenceStep
    {
        [Tooltip("Какое задание ждём")]
        public ObjectiveData Objective;

        public override string Describe() => $"Wait objective: {NameOf(Objective)}";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (Objective == null) yield break;
            while (ObjectiveManager.Instance == null || !ObjectiveManager.Instance.IsCompleted(Objective))
                yield return null;
        }
    }
}
