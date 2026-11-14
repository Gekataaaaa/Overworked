using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using static Overworked.EditorTools.UiBuild;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Собирает сцену MainMenu: меню Continue / New Game / Quit и пролог-терминал (паспорт, разделы 5 и 21).
    /// Заодно импортирует TMP Essentials, создаёт TMP-шрифты (PT Sans, IBM Plex Mono) и тексты пролога.
    /// Запуск: меню Overworked → P1 → Build Main Menu.
    /// Повторный запуск пересоздаёт только MainMenu Canvas; шрифты и тексты не перезаписывает.
    /// </summary>
    public static class MainMenuBuilder
    {
        const string FontsDir = "Assets/_Project/Fonts";
        const string MainFontPath = FontsDir + "/PTSans SDF.asset";
        const string MonoFontPath = FontsDir + "/IBMPlexMono SDF.asset";
        const string BootPath = "Assets/_Project/Data/Dialogues/Prologue_Boot.asset";
        const string IntroPath = "Assets/_Project/Data/Dialogues/Prologue_Intro.asset";
        const string WelcomePath = "Assets/_Project/Data/Dialogues/Prologue_Welcome.asset";
        const string ScenePath = "Assets/_Project/Scenes/MainMenu.unity";
        const string CrtMaterialPath = "Assets/_Project/Materials/TerminalCRT.mat";

        static readonly Color TerminalGreen = new Color(0.36f, 0.85f, 0.45f);
        static readonly Color PromptColor = new Color(0.6f, 1f, 0.68f);
        const float FontSize = 30;

        const string TmpEssentials = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        [MenuItem("Overworked/P1/Build Main Menu")]
        public static void Run()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                // TMP Essentials (настройки, шейдеры) ещё не в проекте — импортируем сами и продолжаем после импорта.
                Debug.Log("[Setup] импорт TMP Essential Resources…");
                AssetDatabase.importPackageCompleted += OnTmpImported;
                AssetDatabase.ImportPackage(TmpEssentials, false);
                return;
            }

            var mainFont = EnsureFontAsset($"{FontsDir}/PTSans-Regular.ttf", MainFontPath);
            var monoFont = EnsureFontAsset($"{FontsDir}/IBMPlexMono-Regular.ttf", MonoFontPath);
            SetDefaultFont(mainFont);

            // Стартовые тексты, только если ассетов ещё нет; дальше их правят в инспекторе.
            const string ok = "[  <color=#7CFF8A>OK</color>  ]";
            // Лог загрузки в стиле Arch Linux: ядро с метками времени, затем службы systemd.
            EnsureDialogue(BootPath,
                "<color=#8A8A8A>[    0.000000]</color> Linux version 6.9.4-wtts1-1 (wtts@buildhost) (gcc 14.1.1) #1 SMP PREEMPT_DYNAMIC",
                "<color=#8A8A8A>[    0.000000]</color> Command line: BOOT_IMAGE=/vmlinuz-wtts root=UUID=0f03-a114 rw quiet shift=night",
                "<color=#8A8A8A>[    0.000000]</color> BIOS-provided physical RAM map:",
                "<color=#8A8A8A>[    0.004118]</color> DMI: OVERWORK Corp. Office Terminal WS-114, BIOS 4.02 11/13/2009",
                "<color=#8A8A8A>[    0.118342]</color> ACPI: Core revision 20240322",
                "<color=#8A8A8A>[    0.296571]</color> PCI: Using configuration type 1 for base access",
                "<color=#8A8A8A>[    0.437902]</color> usb 1-1: new full-speed USB device number 2 (badge reader)",
                "<color=#8A8A8A>[    0.552018]</color> input: AT Translated Set 2 keyboard as /devices/platform/i8042/serio0/input/input3",
                "<color=#8A8A8A>[    0.873116]</color> EXT4-fs (sda2): mounted filesystem with ordered data mode. Quota mode: none.",
                "<color=#8A8A8A>[    1.002377]</color> systemd[1]: systemd 256 running in system mode",
                "<color=#8A8A8A>[    1.004121]</color> systemd[1]: Detected architecture x86-64.",
                "<color=#8A8A8A>[    1.010532]</color> systemd[1]: Hostname set to <noparse><floor03-ws114></noparse>.",
                "",
                "         Starting Journal Service...",
                "[  <color=#7CFF8A>OK</color>  ] Started Journal Service.",
                "[  <color=#7CFF8A>OK</color>  ] Reached target Local File Systems.",
                "         Starting Network Manager...",
                "[  <color=#7CFF8A>OK</color>  ] Started Network Manager.",
                "[  <color=#7CFF8A>OK</color>  ] Started Badge Reader Daemon.",
                "         Starting Light Control (Floor 03)...",
                "[  <color=#7CFF8A>OK</color>  ] Started Light Control (Floor 03).",
                "         Starting Main Exit Door Controller...",
                "[<color=#FF5C5C>FAILED</color>] Failed to start Main Exit Door Controller.",
                "See 'systemctl status exit-door.service' for details.",
                "         Starting Night Shift Service...",
                "[  <color=#7CFF8A>OK</color>  ] Started Night Shift Service.",
                "[  <color=#7CFF8A>OK</color>  ] Reached target Floor 03.",
                "[ <color=#FFC857>WARN</color> ] Operator not registered.");
            EnsureDialogue(IntroPath,
                "<color=#E0E0E0>WTTS v.4.02 — Work Time Tracking System (tty1)</color>",
                "",
                "Night shift requires mandatory operator registration.",
                "Enter your name to confirm access.",
                "",
                "Enter your name:");
            EnsureDialogue(WelcomePath,
                ok + " Operator registered: {name}",
                "",
                "WELCOME TO THE STAFF, {name}.",
                "DO NOT LEAVE YOUR WORKPLACE.");

            BuildScene(mainFont, monoFont);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] MainMenu: меню и пролог готовы");
        }

        static void OnTmpImported(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnTmpImported;
            AssetDatabase.Refresh();
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                Debug.LogError("[Setup] TMP Essential Resources не импортировались — Window → TextMeshPro → Import TMP Essential Resources");
                return;
            }
            Run();
        }

        // ------------------------------------------------------------ шрифты

        /// <summary>
        /// Динамический SDF-шрифт: глифы досоздаются по мере надобности, поэтому имя игрока
        /// на любом языке отрисуется. Латиница и кириллица заранее запечены в атлас.
        /// </summary>
        static TMP_FontAsset EnsureFontAsset(string ttfPath, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
            fa.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(fa, assetPath);
            // Атлас и материал должны лежать внутри ассета шрифта, иначе после перезапуска они теряются.
            fa.atlasTextures[0].name = fa.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = fa.name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);

            var chars = new System.Text.StringBuilder();
            for (char c = ' '; c <= '~'; c++) chars.Append(c);
            for (char c = 'А'; c <= 'я'; c++) chars.Append(c);
            chars.Append("Ёё—–…«»№’“”");
            fa.TryAddCharacters(chars.ToString());

            // Глифы, досозданные во время игры, не тащим в билд — меньше мусора в атласе.
            var so = new SerializedObject(fa);
            var clear = so.FindProperty("m_ClearDynamicDataOnBuild");
            if (clear != null) clear.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(fa);
            return fa;
        }

        static void SetDefaultFont(TMP_FontAsset font)
        {
            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            var so = new SerializedObject(settings);
            so.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        // ------------------------------------------------------------ тексты

        static DialogueData EnsureDialogue(string path, params string[] lines)
        {
            var existing = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (existing != null) return existing;
            var d = ScriptableObject.CreateInstance<DialogueData>();
            d.Lines = new DialogueLine[lines.Length];
            for (int i = 0; i < lines.Length; i++) d.Lines[i] = new DialogueLine { Speaker = Speaker.System, Text = lines[i] };
            AssetDatabase.CreateAsset(d, path);
            return d;
        }

        // ------------------------------------------------------------ сцена

        static void BuildScene(TMP_FontAsset mainFont, TMP_FontAsset monoFont)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == "MainMenu Canvas") Object.DestroyImmediate(go);

            var canvasGo = new GameObject("MainMenu Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // --- Меню
            var menu = Stretch(UI("Menu", canvasGo.transform));
            var title = Tmp("Title", menu, mainFont, "OVERWORKED", 140, MenuText, TextAlignmentOptions.Center);
            Place(title.rectTransform, new Vector2(0, 180), new Vector2(1400, 200));
            title.characterSpacing = 12;

            var buttons = UI("Buttons", menu);
            // Высота под три кнопки по 80 + отступы; Continue скрывается, если нет сохранения.
            Place(buttons, new Vector2(0, -180), new Vector2(360, 280));
            var vlg = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20;
            vlg.childControlHeight = vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            var cont = MenuButton("ContinueButton", buttons, mainFont, "Continue");
            var newGame = MenuButton("NewGameButton", buttons, mainFont, "New Game");
            var quit = MenuButton("QuitButton", buttons, mainFont, "Quit");

            // --- Пролог-терминал
            var terminal = Stretch(UI("PrologueTerminal", canvasGo.transform));
            terminal.gameObject.AddComponent<Image>().color = Color.black;

            // Как консоль Linux (tty): весь экран чёрный, текст от левого верхнего угла, без рамок.
            var content = Stretch(UI("Content", terminal), 56);
            var cvlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            cvlg.spacing = 0;
            cvlg.childControlHeight = cvlg.childControlWidth = true;
            cvlg.childForceExpandHeight = false;

            var output = Tmp("Output", content, monoFont, "", FontSize, TerminalGreen, TextAlignmentOptions.TopLeft);
            output.lineSpacing = 8;

            var row = UI("InputRow", content);
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 18;
            hlg.childControlHeight = hlg.childControlWidth = true;
            hlg.childForceExpandWidth = false;
            row.gameObject.AddComponent<LayoutElement>().minHeight = FontSize * 1.5f;
            var prompt = Tmp("Prompt", row, monoFont, "Enter your name:", FontSize, PromptColor, TextAlignmentOptions.Left);
            var input = NameInput(row, monoFont);

            var prologue = terminal.gameObject.AddComponent<PrologueTerminal>();
            // Тексты грузим здесь, ПОСЛЕ OpenScene: открытие сцены выгружает загруженные раньше ассеты,
            // и в сцену записались бы пустые ссылки (так пролог раньше остался без текстов).
            Wire(prologue, ("output", output), ("prompt", prompt), ("nameInput", input),
                ("boot", AssetDatabase.LoadAssetAtPath<DialogueData>(BootPath)),
                ("intro", AssetDatabase.LoadAssetAtPath<DialogueData>(IntroPath)),
                ("welcome", AssetDatabase.LoadAssetAtPath<DialogueData>(WelcomePath)),
                ("crtMaterial", AssetDatabase.LoadAssetAtPath<Material>(CrtMaterialPath)));

            var menuUi = canvasGo.AddComponent<MainMenuUI>();
            Wire(menuUi, ("menuPanel", menu.gameObject), ("continueButton", cont), ("newGameButton", newGame),
                ("quitButton", quit), ("prologue", prologue));

            terminal.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static TMP_InputField NameInput(RectTransform parent, TMP_FontAsset font)
        {
            var go = UI("NameInput", parent).gameObject;
            go.AddComponent<Image>().color = Color.clear;
            go.AddComponent<LayoutElement>().flexibleWidth = 1;

            var area = Stretch(UI("Text Area", go.transform));
            area.gameObject.AddComponent<RectMask2D>();
            var text = Tmp("Text", area, font, "", FontSize, PromptColor, TextAlignmentOptions.Left);
            Stretch(text.rectTransform);
            text.richText = false;

            var field = go.AddComponent<TMP_InputField>();
            field.textViewport = area;
            field.textComponent = text;
            field.fontAsset = font;
            field.pointSize = FontSize;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.richText = false;
            field.characterLimit = 16;
            // Толстая мигающая каретка — как курсор старого терминала.
            field.customCaretColor = true;
            field.caretColor = PromptColor;
            field.caretWidth = 14;
            field.caretBlinkRate = 1.2f;
            field.selectionColor = new Color(TerminalGreen.r, TerminalGreen.g, TerminalGreen.b, 0.3f);
            return field;
        }
    }
}
