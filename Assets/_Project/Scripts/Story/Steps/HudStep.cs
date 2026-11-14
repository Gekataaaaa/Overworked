using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>Показать/спрятать часть HUD (онбординг Floor_03, WhiteRoom).</summary>
    [Serializable]
    public class HudStep : SequenceStep
    {
        [Tooltip("Какая часть HUD")]
        public HudPart Part;

        [Tooltip("Показать (true) или спрятать (false)")]
        public bool Visible = true;

        [Tooltip("Применить ко всему HUD сразу, Part не важен")]
        public bool AllParts;

        public override string Describe() => $"HUD: {(AllParts ? "all" : Part.ToString())} {(Visible ? "show" : "hide")}";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (UIManager.Instance == null) yield break;
            if (AllParts) UIManager.Instance.SetHudVisible(Visible);
            else UIManager.Instance.SetHudVisible(Part, Visible);
        }
    }
}
