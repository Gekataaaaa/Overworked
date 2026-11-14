using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>Анимация Дэнни: LookAtScreen, Fall, LieDown, StandUp (PlayerController.PlayAnim).</summary>
    [Serializable]
    public class PlayAnimStep : SequenceStep
    {
        [Tooltip("Имя состояния в Animator Дэнни")]
        public string Anim = "LookAtScreen";

        [Tooltip("Сколько секунд ждать после запуска (длина анимации)")]
        public float WaitSeconds;

        public override string Describe() => $"Anim: {Anim}";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (PlayerController.Instance != null) PlayerController.Instance.PlayAnim(Anim);
            if (WaitSeconds > 0f) yield return new WaitForSeconds(WaitSeconds);
        }
    }
}
