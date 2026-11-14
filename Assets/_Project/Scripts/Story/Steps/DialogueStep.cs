using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>Реплики Дэнни / описание / системный текст субтитрами (DialogueManager).</summary>
    [Serializable]
    public class DialogueStep : SequenceStep
    {
        [Tooltip("Строки (ассет из Data/Dialogues или Data/Inspectables)")]
        public DialogueData Dialogue;

        [Tooltip("Ждать, пока все строки покажутся")]
        public bool Wait = true;

        public override string Describe() => $"Dialogue: {NameOf(Dialogue)}";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (Dialogue == null || DialogueManager.Instance == null) yield break;
            if (Wait) yield return ScriptedSequence.WaitCallback(cb => DialogueManager.Instance.Play(Dialogue, cb));
            else DialogueManager.Instance.Play(Dialogue);
        }
    }
}
