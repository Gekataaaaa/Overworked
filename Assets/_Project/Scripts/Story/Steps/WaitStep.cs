using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>Пауза. Время игровое: на паузе меню сценарий тоже стоит.</summary>
    [Serializable]
    public class WaitStep : SequenceStep
    {
        [Tooltip("Секунды")]
        public float Seconds = 1f;

        public override string Describe() => $"Wait {Seconds:0.##}s";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (Seconds > 0f) yield return new WaitForSeconds(Seconds);
        }
    }
}
