using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>Тост: «Received: …», «This doesn't look like the kitchen.»</summary>
    [Serializable]
    public class ToastStep : SequenceStep
    {
        [Tooltip("Текст тоста (английский, поддерживает {name})")]
        public string Text;

        public override string Describe() => $"Toast: {Text}";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowToast(Text);
            yield break;
        }
    }
}
