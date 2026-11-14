using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>Выдать или закрыть задание (задания без сообщения: F1_FindKey101, F1_FinishWork).</summary>
    [Serializable]
    public class ObjectiveStep : SequenceStep
    {
        [Tooltip("Задание (ассет из Data/Objectives)")]
        public ObjectiveData Objective;

        [Tooltip("Закрыть задание вместо выдачи")]
        public bool Complete;

        public override string Describe() => $"Objective {(Complete ? "complete" : "start")}: {NameOf(Objective)}";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (Objective == null || ObjectiveManager.Instance == null) yield break;
            if (Complete) ObjectiveManager.Instance.Complete(Objective);
            else ObjectiveManager.Instance.StartObjective(Objective);
        }
    }
}
