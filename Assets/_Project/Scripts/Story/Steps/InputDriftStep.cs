using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>Сбой ввода (Floor_02): несколько секунд Дэнни идёт дальше после отпускания клавиши.</summary>
    [Serializable]
    public class InputDriftStep : SequenceStep
    {
        [Tooltip("Длительность сбоя, секунды")]
        public float Seconds = 3f;

        [Tooltip("Ждать, пока сбой закончится")]
        public bool Wait = true;

        public override string Describe() => $"Input drift {Seconds:0.##}s";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (PlayerController.Instance != null) PlayerController.Instance.StartInputDrift(Seconds);
            if (Wait && Seconds > 0f) yield return new WaitForSeconds(Seconds);
        }
    }
}
