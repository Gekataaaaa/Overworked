using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>Онбординг HUD Floor_03: элементы по очереди, затем подсказка управления; ждёт её закрытия.</summary>
    [Serializable]
    public class HudOnboardingStep : SequenceStep
    {
        [Tooltip("Компонент HudOnboarding в сцене (группа Story)")]
        public HudOnboarding Onboarding;

        public override string Describe() => $"HUD onboarding: {NameOf(Onboarding)}";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (Onboarding == null)
            {
                Debug.LogWarning($"[Story] {sequence.name}: в HudOnboardingStep не задан HudOnboarding");
                yield break;
            }
            yield return ScriptedSequence.WaitCallback(cb => Onboarding.Play(cb));
        }
    }
}
