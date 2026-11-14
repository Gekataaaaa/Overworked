using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>Заблокировать или вернуть управление Дэнни.</summary>
    [Serializable]
    public class LockPlayerStep : SequenceStep
    {
        [Tooltip("true — Дэнни стоит, false — управление возвращается")]
        public bool Locked = true;

        public override string Describe() => Locked ? "Lock player" : "Unlock player";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (PlayerController.Instance != null) PlayerController.Instance.SetInputLocked(Locked);
            yield break;
        }
    }
}
