using System;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Текущее задание: выдаёт, закрывает, сообщает остальным о выполнении.
    /// ЗАГЛУШКА — заполняет P2.
    /// </summary>
    public class ObjectiveManager : SingletonBehaviour<ObjectiveManager>
    {
        /// <summary>Задание выполнено (UI ставит галочку, сценарий идёт дальше).</summary>
        public event Action<ObjectiveData> OnCompleted;

        /// <summary>Текущее задание или null.</summary>
        public ObjectiveData Current { get; private set; }

        /// <summary>Выдаёт новое задание.</summary>
        public void StartObjective(ObjectiveData o)
        {
            Current = o;
            Debug.Log($"[Objectives] Start {o?.Id}");
        }

        /// <summary>Закрывает задание.</summary>
        public void Complete(ObjectiveData o)
        {
            Debug.Log($"[Objectives] Complete {o?.Id}");
            if (Current == o) Current = null;
            OnCompleted?.Invoke(o);
        }
    }
}
