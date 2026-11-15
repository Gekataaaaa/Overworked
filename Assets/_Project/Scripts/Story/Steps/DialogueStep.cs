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

        [Tooltip("Не показывать, если стоит этот флаг GameState (Floor_03: «нужна карта Макса» — пока нет MaxPass). Пусто — всегда")]
        public string UnlessFlag;

        public override string Describe() =>
            $"Dialogue: {NameOf(Dialogue)}{(string.IsNullOrEmpty(UnlessFlag) ? "" : $" (если нет флага {UnlessFlag})")}";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (Dialogue == null || DialogueManager.Instance == null) yield break;
            if (!string.IsNullOrEmpty(UnlessFlag) && InteractableBase.HasFlag(UnlessFlag)) yield break;
            // Реплики сценария сменяются сами: Дэнни заблокирован сценарием — ожидание нажатия выглядело бы как зависание.
            if (Wait) yield return ScriptedSequence.WaitCallback(cb => DialogueManager.Instance.Play(Dialogue, cb, false));
            else DialogueManager.Instance.Play(Dialogue, null, false);
        }
    }
}
