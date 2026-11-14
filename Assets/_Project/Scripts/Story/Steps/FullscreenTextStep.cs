using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>Текст на весь экран (системное сообщение); сценарий ждёт закрытия.</summary>
    [Serializable]
    public class FullscreenTextStep : SequenceStep
    {
        [Tooltip("Текст (английский, поддерживает {name})")]
        [TextArea(2, 6)] public string Text;

        public override string Describe() => "Fullscreen text";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (UIManager.Instance == null) yield break;
            yield return ScriptedSequence.WaitCallback(cb => UIManager.Instance.ShowFullscreenText(Text, cb));
        }
    }
}
