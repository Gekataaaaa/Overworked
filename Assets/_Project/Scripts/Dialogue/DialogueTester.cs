using UnityEngine;
using UnityEngine.InputSystem;

namespace Overworked
{
    /// <summary>
    /// Отладка субтитров в Sandbox_P3: Z / X / C проигрывают диалоги из списка,
    /// V — все подряд (проверка очереди). В консоль пишется, когда сработал done.
    /// В сцены этажей не класть.
    /// </summary>
    public class DialogueTester : MonoBehaviour
    {
        [Tooltip("Тестовые диалоги: Z — первый, X — второй, C — третий")]
        [SerializeField] DialogueData[] dialogues;

        static readonly Key[] Keys = { Key.Z, Key.X, Key.C };

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || dialogues == null || DialogueManager.Instance == null) return;

            for (int i = 0; i < Keys.Length && i < dialogues.Length; i++)
                if (kb[Keys[i]].wasPressedThisFrame) Play(dialogues[i]);

            if (kb[Key.V].wasPressedThisFrame)
                foreach (var d in dialogues) Play(d);
        }

        void Play(DialogueData d)
        {
            if (d == null) return;
            DialogueManager.Instance.Play(d, () => Debug.Log($"[Dialogue] done: {d.name}"));
        }
    }
}
