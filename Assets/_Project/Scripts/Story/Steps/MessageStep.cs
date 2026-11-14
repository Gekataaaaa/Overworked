using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Сообщение в телефон. С Wait сценарий ждёт, пока игрок откроет чат и закроет телефон
    /// (тогда же выдаётся LinkedObjective). Цепочка «Макс, следом Босс» — два шага: первый без Wait.
    /// </summary>
    [Serializable]
    public class MessageStep : SequenceStep
    {
        [Tooltip("Сообщение (ассет из Data/Messages)")]
        public MessageData Message;

        [Tooltip("Ждать, пока игрок прочитает и закроет телефон")]
        public bool WaitUntilRead = true;

        public override string Describe() => $"Message: {NameOf(Message)}{(WaitUntilRead ? " (ждать прочтения)" : "")}";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (Message == null || MessageManager.Instance == null) yield break;
            if (WaitUntilRead) yield return ScriptedSequence.WaitCallback(cb => MessageManager.Instance.Deliver(Message, cb));
            else MessageManager.Instance.Deliver(Message);
        }
    }
}
