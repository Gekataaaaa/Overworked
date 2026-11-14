using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>Мгновенно поставить Дэнни в точку.</summary>
    [Serializable]
    public class TeleportStep : SequenceStep
    {
        [Tooltip("Точка в сцене")]
        public Transform Point;

        public override string Describe() => $"Teleport: {NameOf(Point)}";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            if (Point != null && PlayerController.Instance != null) PlayerController.Instance.Teleport(Point);
            yield break;
        }
    }
}
