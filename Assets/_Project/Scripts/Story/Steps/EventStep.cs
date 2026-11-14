using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Overworked
{
    /// <summary>
    /// Произвольные вызовы через UnityEvent — для всего, чего нет в шагах:
    /// открыть дверь (DoorInteractable.SetDoorLocked), включить свет, вызвать метод P4.
    /// </summary>
    [Serializable]
    public class EventStep : SequenceStep
    {
        [Tooltip("Подпись шага в инспекторе")]
        public string Note;

        public UnityEvent OnRun = new UnityEvent();

        public override string Describe() => string.IsNullOrEmpty(Note) ? "Event" : $"Event: {Note}";

        public override IEnumerator Run(ScriptedSequence sequence)
        {
            OnRun?.Invoke();
            yield break;
        }
    }
}
