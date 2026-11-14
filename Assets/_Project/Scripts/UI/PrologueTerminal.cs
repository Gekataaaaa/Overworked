using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Overworked
{
    /// <summary>
    /// Пролог (паспорт, раздел 5) в виде Linux-терминала: сначала лог загрузки в стиле Arch Linux
    /// (ядро с метками времени, службы [  OK  ] / [FAILED], прокрутка, любая клавиша — пропустить),
    /// затем экран очищается, заголовок tty1 печатается посимвольно и появляется строка ввода «Enter your name: █». Имя — не пустое, до 16 символов; Enter → приветствие → Floor_03.
    /// Тексты — в DialogueData (Data/Dialogues/Prologue_*), поддерживают rich text (&lt;color&gt;).
    /// Последняя строка intro — подпись у поля ввода, остальные печатаются выше.
    /// </summary>
    public class PrologueTerminal : MonoBehaviour
    {
        [Tooltip("Текст терминала (лог сверху)")]
        [SerializeField] TMP_Text output;

        [Tooltip("Подпись слева от поля ввода, берётся из последней строки intro")]
        [SerializeField] TMP_Text prompt;

        [Tooltip("Поле ввода имени")]
        [SerializeField] TMP_InputField nameInput;

        [Tooltip("Лог загрузки в стиле Arch Linux (ядро + службы). Строки выводятся целиком, быстро, с прокруткой")]
        [SerializeField] DialogueData boot;

        [Tooltip("Пауза между строками лога загрузки: от (x) до (y) секунд, случайно")]
        [SerializeField] Vector2 bootLineDelay = new Vector2(0.015f, 0.12f);

        [Tooltip("Дополнительная пауза после строки с [FAILED] — чтобы игрок успел её заметить")]
        [SerializeField] float failedPause = 0.8f;

        [Tooltip("Сколько держать конец лога перед очисткой экрана")]
        [SerializeField] float bootHold = 1.2f;

        [Tooltip("Сколько строк лога помещается на экран; старые уезжают вверх")]
        [SerializeField] int maxBootLines = 24;

        [Tooltip("Экран после загрузки: заголовок tty1 и приглашение; последняя строка — подпись у поля ввода")]
        [SerializeField] DialogueData intro;

        [Tooltip("Ответ после ввода имени, поддерживает {name}")]
        [SerializeField] DialogueData welcome;

        [Tooltip("Секунд на один символ при печати")]
        [SerializeField] float charDelay = 0.02f;

        [Tooltip("Сколько секунд держать приветствие перед затемнением")]
        [SerializeField] float welcomeHold = 2.5f;

        [Tooltip("Максимальная длина имени")]
        [SerializeField] int maxNameLength = 16;

        [Tooltip("Материал CRT (как у автомата Tiny UFO); пусто — терминал без эффекта")]
        [SerializeField] Material crtMaterial;

        bool submitted;
        GameObject inputRow;

        /// <summary>Показывает терминал и начинает печать. Вызывает MainMenuUI после Start.</summary>
        public void Begin()
        {
            gameObject.SetActive(true);
            submitted = false;
            // Подпись и поле живут в одной строке-контейнере, прячем и показываем её целиком.
            inputRow = nameInput.transform.parent.gameObject;
            inputRow.SetActive(false);

            nameInput.characterLimit = maxNameLength;
            nameInput.onValidateInput = ValidateChar;
            nameInput.onSubmit.RemoveAllListeners();
            nameInput.onSubmit.AddListener(OnSubmit);
            nameInput.text = "";
            EnableCrt();
            StartCoroutine(IntroRoutine());
        }

        /// <summary>
        /// Включает CRT на всей сцене MainMenu (меню и терминал); после пролога грузится Floor_03 —
        /// эффект уходит вместе со сценой. MainMenuUI вызывает его на старте меню, Begin — ещё раз (безопасно).
        /// </summary>
        public void EnableCrt()
        {
            var cam = Camera.main;
            if (crtMaterial == null || cam == null) return;
            // CRT — проход камеры, а overlay-холст рисуется поверх кадра мимо неё. Переводим холст
            // меню на эту камеру, чтобы эффект лёг на текст. true — терминал на старте выключен.
            var canvas = GetComponentInParent<Canvas>(true).rootCanvas;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            // Не «??»: у UnityEngine.Object отсутствующий компонент — «фальшивый null», ?? его не видит.
            var crt = cam.GetComponent<TerminalCrt>();
            if (crt == null) crt = cam.gameObject.AddComponent<TerminalCrt>();
            crt.material = crtMaterial;
            crt.enabled = true;
        }

        IEnumerator IntroRoutine()
        {
            int last = intro != null && intro.Lines != null ? intro.Lines.Length - 1 : -1;
            prompt.text = last >= 0 ? intro.Lines[last].Text : "Enter your name:";
            output.text = "";
            if (boot != null && boot.Lines != null && boot.Lines.Length > 0)
            {
                yield return BootRoutine();
                // Как getty после загрузки: экран очищается, остаётся заголовок tty и приглашение.
                output.text = "";
                yield return new WaitForSecondsRealtime(0.4f);
            }
            yield return Type(Join(intro, last), false);
            inputRow.SetActive(true);
            nameInput.ActivateInputField();
        }

        /// <summary>
        /// Лог загрузки: строки выводятся целиком (не посимвольно) с неровными паузами, экран прокручивается.
        /// Любая клавиша — промотать до конца.
        /// </summary>
        IEnumerator BootRoutine()
        {
            var shown = new List<string>();
            bool skip = false;
            output.maxVisibleCharacters = int.MaxValue;
            foreach (var line in boot.Lines)
            {
                shown.Add(line.Text);
                if (shown.Count > maxBootLines) shown.RemoveAt(0);
                output.text = string.Join("\n", shown);
                if (skip) continue;
                float delay = Random.Range(bootLineDelay.x, bootLineDelay.y);
                if (line.Text != null && line.Text.Contains("FAILED")) delay += failedPause;
                yield return WaitOrSkip(delay, () => skip = true);
            }
            if (!skip) yield return WaitOrSkip(bootHold, () => { });
        }

        /// <summary>Ждёт seconds (реальное время); если нажата любая клавиша — сразу выходит и вызывает onSkip.</summary>
        static IEnumerator WaitOrSkip(float seconds, System.Action onSkip)
        {
            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end)
            {
                if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                {
                    onSkip();
                    yield break;
                }
                yield return null;
            }
        }

        void OnSubmit(string value)
        {
            if (submitted) return;
            string name = value.Trim();
            if (name.Length == 0)
            {
                // Пустое имя не принимаем: оставляем поле активным, игрок вводит заново.
                nameInput.text = "";
                nameInput.ActivateInputField();
                return;
            }
            submitted = true;
            GameManager.Instance.State.PlayerName = name;
            Debug.Log($"[UI] Prologue: имя {name}");
            nameInput.DeactivateInputField();
            inputRow.SetActive(false);
            // Введённая строка остаётся в логе терминала, приветствие печатается под ней.
            output.text += "\n" + prompt.text + " " + name;
            output.maxVisibleCharacters = int.MaxValue;
            StartCoroutine(WelcomeRoutine());
        }

        IEnumerator WelcomeRoutine()
        {
            yield return Type("\n" + TextFormatter.Format(Join(welcome, -1)), true);
            yield return new WaitForSecondsRealtime(welcomeHold);
            // TODO(P4): гул ламп и стук клавиатуры на чёрном экране (паспорт, раздел 5).
            GameManager.Instance.LoadFloor(3);
        }

        /// <summary>Печатает текст посимвольно через maxVisibleCharacters (append — дописать к уже показанному).</summary>
        IEnumerator Type(string text, bool append)
        {
            // Считаем видимые символы, а не длину строки: теги <color> в счёт не идут.
            output.ForceMeshUpdate();
            int start = append ? output.textInfo.characterCount : 0;
            output.text = append ? output.text + text : text;
            output.ForceMeshUpdate();
            int total = output.textInfo.characterCount;
            for (int i = start; i <= total; i++)
            {
                output.maxVisibleCharacters = i;
                yield return new WaitForSecondsRealtime(charDelay);
            }
        }

        /// <summary>Склеивает строки через перенос; skipIndex — строка, которую пропустить (-1 — никакую).</summary>
        static string Join(DialogueData data, int skipIndex)
        {
            if (data == null || data.Lines == null) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < data.Lines.Length; i++)
            {
                if (i == skipIndex) continue;
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(data.Lines[i].Text);
            }
            return sb.ToString();
        }

        // Имя только латиницей (игра на английском): буквы A–Z, цифры, пробел и дефис не первым символом.
        // Заодно отсекаются < > { }, которые сломали бы rich text и подстановку {name}.
        static char ValidateChar(string text, int index, char c)
        {
            bool latin = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
            bool digit = c >= '0' && c <= '9';
            bool allowed = latin || digit || ((c == ' ' || c == '-') && index > 0);
            return allowed ? c : '\0';
        }
    }
}
