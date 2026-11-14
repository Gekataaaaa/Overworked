using System;
using System.Collections.Generic;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Текущее задание: выдаёт, закрывает, сообщает остальным. Одно задание за раз (так в нарративе).
    /// Живёт в _Systems и переживает смену этажей.
    /// </summary>
    public class ObjectiveManager : SingletonBehaviour<ObjectiveManager>
    {
        /// <summary>Выдано новое задание (UI показывает карточку).</summary>
        public event Action<ObjectiveData> OnStarted;

        /// <summary>Задание выполнено (UI ставит галочку, сценарий идёт дальше).</summary>
        public event Action<ObjectiveData> OnCompleted;

        /// <summary>Текущее задание или null.</summary>
        public ObjectiveData Current { get; private set; }

        // Выполненные — по Id, а не по ассету: два ассета с одним Id (сделаны в разных ветках) — одно задание.
        readonly HashSet<string> completed = new HashSet<string>();

        /// <summary>Одно ли это задание (по Id).</summary>
        public static bool Same(ObjectiveData a, ObjectiveData b) =>
            a != null && b != null && (a == b || a.Id == b.Id);

        /// <summary>
        /// Выдаёт новое задание. Повторная выдача текущего или уже выполненного ничего не делает —
        /// триггер, сработавший дважды, не сбросит прогресс.
        /// </summary>
        public void StartObjective(ObjectiveData o)
        {
            if (o == null || Same(o, Current) || completed.Contains(o.Id)) return;
            Current = o;
            Debug.Log($"[Objectives] Start {o.Id}");
            OnStarted?.Invoke(o);
        }

        /// <summary>
        /// Закрывает задание. Можно закрыть и не текущее (игрок сделал шаг раньше, чем получил задание) —
        /// тогда оно просто запоминается как выполненное. Повторный вызов ничего не делает.
        /// </summary>
        public void Complete(ObjectiveData o)
        {
            if (o == null || !completed.Add(o.Id)) return;
            Debug.Log($"[Objectives] Complete {o.Id}");
            if (Same(Current, o)) Current = null;
            OnCompleted?.Invoke(o);
        }

        /// <summary>Выполнено ли задание.</summary>
        public bool IsCompleted(ObjectiveData o) => o != null && completed.Contains(o.Id);

        /// <summary>Выполнено ли задание с таким Id.</summary>
        public bool IsCompleted(string id) => !string.IsNullOrEmpty(id) && completed.Contains(id);

        /// <summary>Сброс для нового прохождения (из главного меню).</summary>
        public void ResetAll()
        {
            Current = null;
            completed.Clear();
        }
    }
}
