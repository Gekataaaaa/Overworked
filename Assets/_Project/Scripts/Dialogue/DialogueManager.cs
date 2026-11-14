using System;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Показывает реплики Дэнни и описания объектов субтитрами внизу экрана.
    /// ЗАГЛУШКА: печатает в консоль и сразу вызывает done — заполняет P3.
    /// </summary>
    public class DialogueManager : SingletonBehaviour<DialogueManager>
    {
        /// <summary>Проигрывает все строки по очереди, затем вызывает done.</summary>
        public void Play(DialogueData d, Action done = null)
        {
            if (d != null && d.Lines != null)
                foreach (var line in d.Lines)
                    Debug.Log($"[Dialogue] {line.Speaker}: {TextFormatter.Format(line.Text)}");
            done?.Invoke();
        }
    }
}
