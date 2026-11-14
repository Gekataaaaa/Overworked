using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>Включить/выключить объект сцены (силуэт, свет, триггер, предмет).</summary>
    [Serializable]
    public class SetActiveStep : SequenceStep
    {
        [Tooltip("Объект сцены")]
        public GameObject Target;

        [Tooltip("Включить (true) или выключить (false)")]
        public bool Active = true;

        public override string Describe() => $"{(Active ? "Enable" : "Disable")}: {NameOf(Target)}";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (Target != null) Target.SetActive(Active);
            yield break;
        }
    }
}
