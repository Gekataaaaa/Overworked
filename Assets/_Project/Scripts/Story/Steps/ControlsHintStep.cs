using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>Подсказка управления; сценарий ждёт, пока игрок её закроет.</summary>
    [Serializable]
    public class ControlsHintStep : SequenceStep
    {
        public override string Describe() => "Controls hint (ждать закрытия)";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (UIManager.Instance == null) yield break;
            yield return ScriptedSequence.WaitCallback(cb => UIManager.Instance.ShowControlsHint(cb));
        }
    }
}
