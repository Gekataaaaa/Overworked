using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static Overworked.EditorTools.UiBuild;

namespace Overworked.EditorTools
{
    /// <summary>
    /// P3: собирает телефон и тост внутри HUD префаба _Systems и тестовую сцену Sandbox_P3.
    /// Меню Overworked → P3 → Build Phone UI: пересоздаёт объекты PhoneButton / PhonePanel / Toast в HUD
    /// (остальное в _Systems не трогает) и проставляет ссылки в PhoneManager, UIManager.
    /// Меню Overworked → P3 → Build Phone Sandbox: коридор, игрок, камера и PhoneTester в Sandbox_P3.
    /// </summary>
    public static class PhoneUiBuilder
    {
        const string SystemsPath = "Assets/_Project/Prefabs/Systems/Resources/_Systems.prefab";
        const string FontPath = "Assets/_Project/Fonts/PTSans SDF.asset";
        const string ScenePath = "Assets/_Project/Scenes/Sandbox/Sandbox_P3.unity";
        static readonly string[] TestMessages =
        {
            "Assets/_Project/Data/Messages/F3_Max_01.asset",
            "Assets/_Project/Data/Messages/F3_Boss_01.asset",
            "Assets/_Project/Data/Messages/F3_Max_02.asset",
        };
        static readonly string[] TestDialogues =
        {
            "Assets/_Project/Data/Dialogues/F3_Start.asset",
            "Assets/_Project/Data/Inspectables/F3_CharlesDesk.asset",
            "Assets/_Project/Data/Dialogues/F2_Monitor201.asset",
        };

        // Палитра проекта: грязно-зелёный, серый, выцветший синий.
        static readonly Color Body = new Color(0.07f, 0.08f, 0.08f, 0.97f);
        static readonly Color Screen = new Color(0.13f, 0.16f, 0.15f);
        static readonly Color Header = new Color(0.18f, 0.22f, 0.2f);
        static readonly Color Row = new Color(0.17f, 0.2f, 0.19f);
        static readonly Color BubbleMax = new Color(0.22f, 0.3f, 0.36f);
        static readonly Color TextLight = new Color(0.86f, 0.88f, 0.84f);
        static readonly Color TextDim = new Color(0.55f, 0.6f, 0.57f);
        static readonly Color Unread = new Color(0.85f, 0.25f, 0.2f);

        [MenuItem("Overworked/P3/Build Phone UI")]
        public static void BuildPhoneUi()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                Debug.LogError("[Setup] нет шрифта PT Sans — сначала Overworked → P1 → Build Main Menu");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(SystemsPath);
            try
            {
                var hud = root.transform.Find("HUD") as RectTransform;
                if (hud == null)
                {
                    Debug.LogError("[Setup] в _Systems нет объекта HUD");
                    return;
                }
                foreach (var old in new[] { "PhoneButton", "PhonePanel", "Toast" })
                {
                    var t = hud.Find(old);
                    if (t != null) Object.DestroyImmediate(t.gameObject);
                }

                var phoneUi = hud.GetComponent<PhoneUI>();
                if (phoneUi == null) phoneUi = hud.gameObject.AddComponent<PhoneUI>();
                var toast = BuildToast(hud, font, out var toastText);
                var button = BuildPhoneButton(hud, font, out var buttonDot);
                BuildPhonePanel(hud, font, phoneUi, button, buttonDot);

                // FadePanel должен остаться последним — он перекрывает весь HUD при переходах.
                var fade = hud.Find("FadePanel");
                if (fade != null) fade.SetAsLastSibling();

                var phoneManager = root.GetComponentInChildren<PhoneManager>(true);
                var uiManager = root.GetComponentInChildren<UIManager>(true);
                if (phoneManager == null || uiManager == null)
                {
                    Debug.LogError("[Setup] в _Systems нет PhoneManager или UIManager");
                    return;
                }
                Wire(phoneManager, ("ui", phoneUi));
                Wire(uiManager, ("toastGroup", toast), ("toastText", toastText), ("phoneUI", phoneUi));

                PrefabUtility.SaveAsPrefabAsset(root, SystemsPath);
                Debug.Log("[Setup] Phone UI: телефон и тост собраны в _Systems/HUD");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ------------------------------------------------------------ тост

        static CanvasGroup BuildToast(RectTransform hud, TMP_FontAsset font, out TextMeshProUGUI text)
        {
            var rt = UI("Toast", hud);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -40);
            rt.sizeDelta = new Vector2(900, 70);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = RoundSprite();
            img.type = Image.Type.Sliced;
            img.color = new Color(0f, 0f, 0f, 0.7f);
            img.raycastTarget = false;
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            text = Tmp("Text", rt, font, "Received: …", 34, TextLight, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 10);
            return group;
        }

        // ------------------------------------------------------------ кнопка телефона

        static Button BuildPhoneButton(RectTransform hud, TMP_FontAsset font, out GameObject dot)
        {
            var rt = UI("PhoneButton", hud);
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-100, 110);
            rt.sizeDelta = new Vector2(96, 150);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = RoundSprite();
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Body;
            colors.highlightedColor = colors.selectedColor = Header;
            colors.pressedColor = Row;
            btn.colors = colors;

            // «Экран» внутри корпуса.
            var screen = UI("Screen", rt);
            Stretch(screen, 10);
            screen.offsetMax = new Vector2(-10, -22);
            screen.offsetMin = new Vector2(10, 22);
            var simg = screen.gameObject.AddComponent<Image>();
            simg.color = Screen;
            simg.raycastTarget = false;
            var label = Tmp("Label", screen, font, "Tab", 26, TextDim, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);

            dot = Dot("UnreadDot", rt, 30);
            var drt = (RectTransform)dot.transform;
            drt.anchorMin = drt.anchorMax = new Vector2(1f, 1f);
            drt.anchoredPosition = new Vector2(-6, -6);
            dot.SetActive(false);
            return btn;
        }

        // ------------------------------------------------------------ окно телефона

        static void BuildPhonePanel(RectTransform hud, TMP_FontAsset font, PhoneUI ui, Button phoneButton, GameObject phoneButtonDot)
        {
            var panel = UI("PhonePanel", hud);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 0f);
            panel.anchoredPosition = new Vector2(-170, 40);
            panel.sizeDelta = new Vector2(520, 860);
            var body = panel.gameObject.AddComponent<Image>();
            body.sprite = RoundSprite();
            body.type = Image.Type.Sliced;
            body.color = Body;

            var screen = UI("Screen", panel);
            Stretch(screen);
            screen.offsetMin = new Vector2(18, 60);
            screen.offsetMax = new Vector2(-18, -40);
            screen.gameObject.AddComponent<Image>().color = Screen;

            // Шапка: «назад» + заголовок.
            var header = UI("Header", screen);
            header.anchorMin = new Vector2(0, 1);
            header.anchorMax = new Vector2(1, 1);
            header.pivot = new Vector2(0.5f, 1);
            header.sizeDelta = new Vector2(0, 80);
            header.anchoredPosition = Vector2.zero;
            header.gameObject.AddComponent<Image>().color = Header;
            var title = Tmp("Title", header, font, "Messages", 38, TextLight, TextAlignmentOptions.Center);
            Stretch(title.rectTransform);
            var back = SmallButton("BackButton", header, font, "<");
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0f, 0.5f);
            brt.anchoredPosition = new Vector2(10, 0);
            brt.sizeDelta = new Vector2(64, 60);

            var body2 = UI("Body", screen);
            Stretch(body2);
            body2.offsetMax = new Vector2(0, -80);

            // Список контактов.
            var contacts = UI("Contacts", body2);
            Stretch(contacts, 0);
            var vlg = contacts.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.spacing = 10;
            vlg.childControlWidth = vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            var max = ContactRow("MaxContact", contacts, font, "Max", out var maxDot);
            var boss = ContactRow("BossContact", contacts, font, "Boss", out var bossDot);

            // Чат.
            var chat = UI("Chat", body2);
            Stretch(chat);
            var scroll = chat.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;
            var viewport = UI("Viewport", chat);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            // Прозрачная подложка ловит мышь: без неё колесо не прокручивает чат —
            // у пузырей raycastTarget выключен, и ScrollRect не получает OnScroll.
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            var content = UI("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            var cvlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            cvlg.padding = new RectOffset(14, 50, 14, 14);
            cvlg.spacing = 10;
            cvlg.childControlWidth = cvlg.childControlHeight = true;
            cvlg.childForceExpandWidth = true;
            cvlg.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;

            var bubble = UI("BubbleTemplate", content);
            var bimg = bubble.gameObject.AddComponent<Image>();
            bimg.sprite = RoundSprite();
            bimg.type = Image.Type.Sliced;
            bimg.color = BubbleMax;
            bimg.raycastTarget = false;
            var bvlg = bubble.gameObject.AddComponent<VerticalLayoutGroup>();
            bvlg.padding = new RectOffset(16, 16, 10, 10);
            bvlg.childControlWidth = bvlg.childControlHeight = true;
            bvlg.childForceExpandHeight = false;
            var btext = Tmp("Text", bubble, font, "Message", 30, TextLight, TextAlignmentOptions.TopLeft);
            btext.textWrappingMode = TextWrappingModes.Normal;
            bubble.gameObject.SetActive(false);

            // Подсказка управления под экраном.
            var hint = Tmp("Hint", panel, font, "A / D — switch chat     Wheel — scroll     Tab / Esc — close", 22, TextDim, TextAlignmentOptions.Center);
            hint.rectTransform.anchorMin = new Vector2(0, 0);
            hint.rectTransform.anchorMax = new Vector2(1, 0);
            hint.rectTransform.pivot = new Vector2(0.5f, 0);
            hint.rectTransform.sizeDelta = new Vector2(0, 50);
            hint.rectTransform.anchoredPosition = new Vector2(0, 6);

            chat.gameObject.SetActive(false);
            panel.gameObject.SetActive(false);

            Wire(ui,
                ("phoneButton", phoneButton), ("phoneButtonDot", phoneButtonDot),
                ("panel", panel.gameObject), ("title", title), ("backButton", back),
                ("contactsView", contacts.gameObject), ("maxContact", max), ("maxDot", maxDot),
                ("bossContact", boss), ("bossDot", bossDot),
                ("chatView", chat.gameObject), ("chatScroll", scroll), ("chatContent", content),
                ("bubbleTemplate", bubble.gameObject));
        }

        static Button ContactRow(string name, RectTransform parent, TMP_FontAsset font, string label, out GameObject dot)
        {
            var rt = UI(name, parent);
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white;
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Row;
            colors.highlightedColor = colors.selectedColor = Header;
            colors.pressedColor = BubbleMax;
            btn.colors = colors;
            var t = Tmp("Name", rt, font, label, 38, TextLight, TextAlignmentOptions.Left);
            Stretch(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(28, 0);
            dot = Dot("UnreadDot", rt, 26);
            var drt = (RectTransform)dot.transform;
            drt.anchorMin = drt.anchorMax = new Vector2(1f, 0.5f);
            drt.anchoredPosition = new Vector2(-36, 0);
            dot.SetActive(false);
            return btn;
        }

        static Button SmallButton(string name, RectTransform parent, TMP_FontAsset font, string label)
        {
            var rt = UI(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = RoundSprite();
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Header;
            colors.highlightedColor = colors.selectedColor = Row;
            colors.pressedColor = BubbleMax;
            btn.colors = colors;
            var t = Tmp("Label", rt, font, label, 36, TextLight, TextAlignmentOptions.Center);
            Stretch(t.rectTransform);
            return btn;
        }

        static GameObject Dot(string name, RectTransform parent, float size)
        {
            var rt = UI(name, parent);
            rt.sizeDelta = new Vector2(size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            img.color = Unread;
            img.raycastTarget = false;
            return rt.gameObject;
        }

        static Sprite RoundSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // ------------------------------------------------------------ песочница

        [MenuItem("Overworked/P3/Build Phone Sandbox")]
        public static void BuildSandbox()
        {
            var square = PlayerSandboxBuilder.EnsureSquareSprite();
            var playerPrefab = PlayerSandboxBuilder.EnsurePlayerPrefab(square);
            var cameraPrefab = PlayerSandboxBuilder.EnsureCameraPrefab();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects())
                if (go.name is "Environment" or "Player" or "Cameras" or "PhoneTester" or "DialogueTester" or "HudTester" or "StoryDemo") Object.DestroyImmediate(go);

            const float halfWidth = 15f, floorY = -4.2f, top = 5.65f, bottom = -5.65f;
            var env = new GameObject("Environment");
            PlayerSandboxBuilder.Rect(env, "BackWall", square, new Vector2(0, (top + floorY) / 2), new Vector2(halfWidth * 2, top - floorY), new Color(0.32f, 0.36f, 0.33f), -10);
            PlayerSandboxBuilder.Rect(env, "Floor", square, new Vector2(0, (floorY + bottom) / 2), new Vector2(halfWidth * 2, floorY - bottom), new Color(0.2f, 0.22f, 0.21f), -5);
            for (int i = -2; i <= 2; i++)
                PlayerSandboxBuilder.Rect(env, $"Door_{i + 3}", square, new Vector2(i * 6f, floorY + 1.25f), new Vector2(1.4f, 2.5f), new Color(0.45f, 0.4f, 0.3f), -8);
            foreach (var x in new[] { -halfWidth - 0.5f, halfWidth + 0.5f })
                PlayerSandboxBuilder.Rect(env, "Wall", square, new Vector2(x, 0), new Vector2(1f, top - bottom), new Color(0.15f, 0.16f, 0.15f), -4)
                    .AddComponent<BoxCollider2D>();

            var bounds = new GameObject("CameraBounds");
            bounds.transform.SetParent(env.transform, false);
            var poly = bounds.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            poly.points = new[]
            {
                new Vector2(-halfWidth - 1, bottom), new Vector2(halfWidth + 1, bottom),
                new Vector2(halfWidth + 1, top), new Vector2(-halfWidth - 1, top),
            };

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.position = new Vector3(0, floorY + 0.9f, 0);

            var cameras = new GameObject("Cameras");
            var vcam = (GameObject)PrefabUtility.InstantiatePrefab(cameraPrefab, scene);
            vcam.name = "CM Corridor";
            vcam.transform.SetParent(cameras.transform, false);
            vcam.GetComponent<CinemachineConfiner2D>().BoundingShape2D = poly;
            // Без CinemachineBrain на Main Camera виртуальная камера ничего не двигает.
            foreach (var cam in Object.FindObjectsByType<Camera>())
                if (cam.CompareTag("MainCamera") && cam.GetComponent<CinemachineBrain>() == null)
                    cam.gameObject.AddComponent<CinemachineBrain>();

            var tester = new GameObject("PhoneTester").AddComponent<PhoneTester>();
            var so = new SerializedObject(tester);
            var arr = so.FindProperty("messages");
            arr.arraySize = TestMessages.Length;
            for (int i = 0; i < TestMessages.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<MessageData>(TestMessages[i]);
            so.ApplyModifiedPropertiesWithoutUndo();

            var dialogueTester = new GameObject("DialogueTester").AddComponent<DialogueTester>();
            var dso = new SerializedObject(dialogueTester);
            var darr = dso.FindProperty("dialogues");
            darr.arraySize = TestDialogues.Length;
            for (int i = 0; i < TestDialogues.Length; i++)
                darr.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<DialogueData>(TestDialogues[i]);
            dso.ApplyModifiedPropertiesWithoutUndo();

            new GameObject("HudTester").AddComponent<HudTester>();
            BuildStoryDemo(square, floorY);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Setup] Sandbox_P3: коридор, игрок, PhoneTester (1–3, 0), DialogueTester (Z/X/C, V), HudTester (N/H/J/K), StoryDemo (зона справа) готовы");
        }

        /// <summary>
        /// Пример сценария: жёлтая зона справа от старта. Дэнни входит → стоит → сообщение Макса
        /// (ждём прочтения) → реплики → тост → управление возвращается.
        /// </summary>
        static void BuildStoryDemo(Sprite square, float floorY)
        {
            var go = new GameObject("StoryDemo");
            go.transform.position = new Vector3(6f, floorY + 2f, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 4f);
            PlayerSandboxBuilder.Rect(go, "Marker", square, Vector2.zero, new Vector2(1f, 4f), new Color(0.9f, 0.8f, 0.2f, 0.25f), -6);

            var seq = go.AddComponent<ScriptedSequence>();
            var so = new SerializedObject(seq);
            var steps = so.FindProperty("steps");
            SequenceStep[] demo =
            {
                new LockPlayerStep { Locked = true },
                new MessageStep { Message = AssetDatabase.LoadAssetAtPath<MessageData>(TestMessages[0]), WaitUntilRead = true },
                new DialogueStep { Dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(TestDialogues[0]), Wait = true },
                new ToastStep { Text = "Demo sequence finished" },
                new LockPlayerStep { Locked = false },
            };
            steps.arraySize = demo.Length;
            for (int i = 0; i < demo.Length; i++) steps.GetArrayElementAtIndex(i).managedReferenceValue = demo[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            var trigger = go.AddComponent<SequenceTrigger>();
            var tso = new SerializedObject(trigger);
            tso.FindProperty("sequence").objectReferenceValue = seq;
            tso.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
