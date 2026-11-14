using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Собирает сцену WhiteRoom (паспорт, раздел 10): белое пространство с линией пола, Дэнни, камера,
    /// точки реплик по пути, кровать (проявляется), сценарий WhiteRoomSequence, белый слой и титры.
    /// Реплики — ассеты WR_01…WR_06 и WR_Bed в Data/Dialogues (имена по таблице P3 docs/systems/Texts.md, тексты из NARRATIVE_EN, сцена 4.0); готовые ассеты не перезаписываются.
    /// Запуск: меню Overworked → P4 → Build White Room. Объекты сцены, которые собирает этот скрипт, пересоздаются;
    /// FloorSettings, Story, Main Camera и свет из каркаса сцены не трогаются.
    /// </summary>
    public static class WhiteRoomBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/WhiteRoom.unity";
        const string DialogueDir = "Assets/_Project/Data/Dialogues";
        const string FontPath = "Assets/_Project/Fonts/PTSans SDF.asset";

        // ВРЕМЕННО, пока сцену проверяем: кровать видна сразу (место то же), на полу метки (иначе в белом не видно,
        // что Дэнни идёт), шкала рассудка и пульс из HUD P3 не прячутся. Перед финалом выключить — по паспорту HUD скрыт.
        const bool DebugMode = true;

        // Кадр камеры 19.2 × 10.8 (orthographic size 5.4), как в остальных сценах.
        const float HalfHeight = 5.4f;
        const float FloorY = -2.5f;
        const float StartX = 0f;
        const float BedX = 32f;
        const float RightWallX = BedX + 3f;

        static readonly Color FloorLine = new Color(0.78f, 0.78f, 0.78f);
        static readonly Color BedFrame = new Color(0.62f, 0.64f, 0.66f);
        static readonly Color BedSheet = new Color(0.88f, 0.89f, 0.9f);
        static readonly Color CreditsColor = new Color(0.35f, 0.36f, 0.38f);

        [MenuItem("Overworked/P4/Build White Room")]
        public static void Run()
        {
            // Сцену открываем ДО загрузки ассетов: OpenScene(Single) выгружает «неиспользуемые» ассеты,
            // и загруженные раньше DialogueData превращаются в null — реплики в триггеры не попадали.
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var square = PlayerSandboxBuilder.EnsureSquareSprite();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            var intro = Dialogue("WR_01", "{name}…", "Are you still here?");
            var where = Dialogue("WR_02", "I don't understand where we are.", "This isn't the office anymore, is it?");
            var bedSeen = Dialogue("WR_Bed", "I think I know this place.");
            var rest = Dialogue("WR_03", "I kept thinking that if I just finished the work, I could rest.",
                "One more report. One more shift. Just a little longer.");
            var home = Dialogue("WR_04", "And then I could go home.");
            var sleep = Dialogue("WR_05", "Funny. I can't even remember the last time I slept properly.");
            var thanks = Dialogue("WR_06", "{name}…", "Thank you for staying with me till the end.");
            var creditsData = Credits();

            BuildScene(square, font, intro, creditsData, new[]
            {
                (6f, where, 0f, false, false),
                (BedX - 10f, bedSeen, 1.5f, !DebugMode, false),
                (BedX - 7.5f, rest, 0f, false, false),
                (BedX - 5.5f, home, 0f, false, false),
                (BedX - 4f, sleep, 0f, false, false),
                (BedX - 2.8f, thanks, 0f, false, false),
                (BedX - 1.6f, null, 0f, false, true),
            });
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] P4: WhiteRoom собрана");
        }

        // ------------------------------------------------------------ тексты

        static DialogueData Dialogue(string assetName, params string[] lines)
        {
            string path = $"{DialogueDir}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (existing != null) return existing;

            var d = ScriptableObject.CreateInstance<DialogueData>();
            // Без имени свежий ассет до переимпорта безымянный — триггеры в сцене назывались бы Trigger_Point.
            d.name = assetName;
            d.Lines = new DialogueLine[lines.Length];
            for (int i = 0; i < lines.Length; i++) d.Lines[i] = new DialogueLine { Speaker = Speaker.Danny, Text = lines[i] };
            AssetDatabase.CreateAsset(d, path);
            return d;
        }

        /// <summary>Титры — заготовка: имена команды впишет тот, кто их знает (ассет WR_Credits).</summary>
        static DialogueData Credits()
        {
            string path = $"{DialogueDir}/WR_Credits.asset";
            var existing = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (existing != null) return existing;

            string[] lines =
            {
                "<size=150%>OVERWORKED</size>", "",
                "Programming", "Programmer 1 — Core / Player", "Programmer 2 — Interaction / Mini-games",
                "Programmer 3 — Phone / Dialogue / Story", "Programmer 4 — Horror / Sound", "",
                "Art", "Artist 1 — Characters", "Artist 2 — Environment", "",
                "Thank you for playing, {name}.",
            };
            var d = ScriptableObject.CreateInstance<DialogueData>();
            d.Lines = new DialogueLine[lines.Length];
            for (int i = 0; i < lines.Length; i++) d.Lines[i] = new DialogueLine { Speaker = Speaker.System, Text = lines[i] };
            AssetDatabase.CreateAsset(d, path);
            return d;
        }

        // ------------------------------------------------------------ сцена

        static void BuildScene(Sprite square, TMP_FontAsset font, DialogueData intro, DialogueData creditsData,
            (float x, DialogueData lines, float hold, bool revealBed, bool lieDown)[] points)
        {
            // Сцена уже открыта в Run (см. комментарий там) — повторный OpenScene снова выгрузил бы ассеты.
            var scene = EditorSceneManager.GetActiveScene();
            foreach (var go in scene.GetRootGameObjects())
                if (go.name is "Environment" or "Player" or "Cameras" or "WhiteRoom" or "WhiteRoomUI")
                    Object.DestroyImmediate(go);

            // Белое пространство: фон даёт белый цвет камеры (каркас сцены), здесь — только линия пола.
            var env = new GameObject("Environment");
            PlayerSandboxBuilder.Rect(env, "FloorLine", square, new Vector2((StartX + RightWallX) / 2f, FloorY), new Vector2(120f, 0.04f), FloorLine, -5);

            if (DebugMode)
                for (float x = StartX - 8f; x <= RightWallX + 8f; x += 2f)
                    PlayerSandboxBuilder.Rect(env, $"DebugMark_{x:0}", square, new Vector2(x, FloorY - 0.15f), new Vector2(0.04f, 0.3f), FloorLine, -5);

            // Влево не пускает SetOnlyRight, стена слева — страховка; справа — за кроватью.
            Wall(env, "WallLeft", StartX - 2f);
            Wall(env, "WallRight", RightWallX);

            var bed = new GameObject("Bed");
            bed.transform.SetParent(env.transform, false);
            bed.transform.position = new Vector3(BedX, FloorY, 0f);
            // Rect ставит localPosition — координаты частей от точки кровати на полу.
            var frame = PlayerSandboxBuilder.Rect(bed, "Frame", square, new Vector2(0f, 0.45f), new Vector2(3.2f, 0.9f), BedFrame, -2);
            var sheet = PlayerSandboxBuilder.Rect(bed, "Sheet", square, new Vector2(0.2f, 1.0f), new Vector2(2.8f, 0.25f), BedSheet, -1);
            var pillow = PlayerSandboxBuilder.Rect(bed, "Pillow", square, new Vector2(-1.1f, 1.2f), new Vector2(0.8f, 0.3f), Color.white, 0);
            var head = PlayerSandboxBuilder.Rect(bed, "Headboard", square, new Vector2(-1.55f, 1.0f), new Vector2(0.12f, 2f), BedFrame, -2);
            var bedPoint = new GameObject("BedPoint").transform;
            bedPoint.SetParent(bed.transform, false);
            bedPoint.position = new Vector3(BedX, FloorY + 1.3f, 0f);

            var bounds = new GameObject("CameraBounds");
            bounds.transform.SetParent(env.transform, false);
            var poly = bounds.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            float top = HalfHeight + 0.25f, bottom = -HalfHeight - 0.25f;
            float left = StartX - 9.8f, right = RightWallX + 1f;
            poly.points = new[] { new Vector2(left, bottom), new Vector2(right, bottom), new Vector2(right, top), new Vector2(left, top) };

            var player = (GameObject)PrefabUtility.InstantiatePrefab(PlayerSandboxBuilder.EnsurePlayerPrefab(square), scene);
            player.transform.position = new Vector3(StartX, FloorY + 0.9f, 0f);

            var cameras = new GameObject("Cameras");
            var vcam = (GameObject)PrefabUtility.InstantiatePrefab(PlayerSandboxBuilder.EnsureCameraPrefab(), scene);
            vcam.name = "CM WhiteRoom";
            vcam.transform.SetParent(cameras.transform, false);
            vcam.GetComponent<CinemachineConfiner2D>().BoundingShape2D = poly;

            var ui = BuildUi(font, out var white, out var credits, out var creditsText);

            var root = new GameObject("WhiteRoom");
            var sequence = root.AddComponent<WhiteRoomSequence>();
            UiBuild.Wire(sequence, ("introLines", intro), ("bedPoint", bedPoint), ("whiteOverlay", white),
                ("credits", credits), ("creditsText", creditsText), ("creditsLines", creditsData));
            var seqSo = new SerializedObject(sequence);
            seqSo.FindProperty("debugShowHorrorHud").boolValue = DebugMode;
            seqSo.ApplyModifiedPropertiesWithoutUndo();

            var bedParts = new[] { frame, sheet, pillow, head };
            foreach (var p in points)
            {
                var go = new GameObject(p.lieDown ? "Trigger_LieDown" : $"Trigger_{(p.lines != null ? p.lines.name : "Point")}");
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(p.x, FloorY + 1f, 0f);
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(0.4f, 3f);
                var trigger = go.AddComponent<WhiteRoomTrigger>();
                var so = new SerializedObject(trigger);
                so.FindProperty("sequence").objectReferenceValue = sequence;
                so.FindProperty("lines").objectReferenceValue = p.lines;
                so.FindProperty("holdPlayer").floatValue = p.hold;
                so.FindProperty("lieDown").boolValue = p.lieDown;
                var reveal = so.FindProperty("reveal");
                reveal.arraySize = p.revealBed ? bedParts.Length : 0;
                for (int i = 0; p.revealBed && i < bedParts.Length; i++)
                    reveal.GetArrayElementAtIndex(i).objectReferenceValue = bedParts[i].GetComponent<SpriteRenderer>();
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Свой Canvas сцены: белый слой и титры. Порядок 90 — под HUD и чёрным затемнением _Systems (100).</summary>
        static GameObject BuildUi(TMP_FontAsset font, out Image white, out CanvasGroup credits, out TextMeshProUGUI creditsText)
        {
            var root = new GameObject("WhiteRoomUI", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var whiteRt = UiBuild.Stretch(UiBuild.UI("WhiteOverlay", root.transform));
            white = whiteRt.gameObject.AddComponent<Image>();
            white.color = new Color(1f, 1f, 1f, 0f);
            white.raycastTarget = false;

            var creditsRt = UiBuild.Stretch(UiBuild.UI("Credits", root.transform));
            credits = creditsRt.gameObject.AddComponent<CanvasGroup>();
            credits.alpha = 0f;
            credits.blocksRaycasts = false;
            creditsText = UiBuild.Tmp("CreditsText", creditsRt, font, "", 34f, CreditsColor, TextAlignmentOptions.Center);
            UiBuild.Stretch(creditsText.rectTransform, 120f);
            return root;
        }

        static void Wall(GameObject parent, string name, float x)
        {
            // Невидимая: в белой комнате нет стен, только упор для Дэнни.
            var wall = new GameObject(name);
            wall.transform.SetParent(parent.transform, false);
            wall.transform.position = new Vector3(x, 0f, 0f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(1f, HalfHeight * 2f);
        }
    }
}
