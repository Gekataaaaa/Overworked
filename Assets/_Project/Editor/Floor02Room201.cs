using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SB = Overworked.EditorTools.PlayerSandboxBuilder;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Room_201 на Floor_02, нарратив сцена 2.2: металлические картотечные шкафы, стол с ящиками,
    /// тускло светящийся компьютер.
    /// - Стол: «[E] Inspect the drawer» → лист A4 из папки графиков смен (F2_ShiftSchedule: имена
    ///   Charles, Max, Danny повторяются, внизу «{name} — Status: Active») → описание (F2_Drawer201), пауза, «Wait. That's your name…»
    ///   (F2_Drawer201_Name).
    /// - Компьютер: «[E] Check the computer» → мини-игра 3 «Настройка сигнала монитора» → на экране
    ///   «REPORT NOT CLOSED. SHIFT CONTINUES.» (F2_Monitor201) → задание F2_CheckDocs201 выполнено.
    ///   Доступен после ящика — как в нарративе: сначала документы, потом компьютер.
    /// Галлюцинация 3 и сообщения после неё — P4/P3 (onDone компьютера).
    /// Тексты — ассеты Data/ с именами из таблицы P3, строки дословно из NARRATIVE_EN; существующие не трогаются.
    /// Меню: Overworked → P2 → Floor_02 → Build Floor_02 Room 201.
    /// </summary>
    [InitializeOnLoad]
    public static class Floor02Room201
    {
        const string ScenePath = "Assets/_Project/Scenes/Floor_02.unity";
        const string RequestFile = "Temp/p2_room201.request";

        const string SheetPath = "Assets/_Project/Data/Dialogues/F2_ShiftSchedule.asset";
        const string DrawerPath = "Assets/_Project/Data/Dialogues/F2_Drawer201.asset";
        const string DrawerNamePath = "Assets/_Project/Data/Dialogues/F2_Drawer201_Name.asset";
        const string MonitorPath = "Assets/_Project/Data/Dialogues/F2_Monitor201.asset";
        const string CheckDocsPath = "Assets/_Project/Data/Objectives/F2_CheckDocs201.asset";
        const string CheckComputerPath = "Assets/_Project/Data/Objectives/F2_CheckComputer201.asset";

        // Флаг «документы в ящике просмотрены» — открывает компьютер.
        const string DocsReadFlag = "Docs201Read";

        static Floor02Room201()
        {
            if (File.Exists(RequestFile)) EditorApplication.delayCall += RunRequest;
        }

        static void RunRequest()
        {
            // В Play Mode сцену не сохранить — ждём выхода в редактор.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= OnPlayMode;
                EditorApplication.playModeStateChanged += OnPlayMode;
                return;
            }
            if (!File.Exists(RequestFile)) return;
            File.Delete(RequestFile);
            Build();
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            RunRequest();
        }

        [MenuItem("Overworked/P2/Floor_02/Build Floor_02 Room 201")]
        public static void Build()
        {
            // Несохранённые правки в открытой сцене — спросить, иначе OpenScene их выбросит.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;

            // Сначала сцена, потом ассеты: OpenScene выгружает «неиспользуемые» ассеты, и загруженные
            // до него тексты/задания превращались в null — в сцену сохранялись пустые ссылки.
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // --- Тексты (NARRATIVE_EN, сцена 2.2)
            // Лист из папки: по нарративу «на каждом листе повторяются одни и те же имена: Charles, Max, Danny…»,
            // «в самом низу — свежая строка: {name} — Status: Active».
            var sheet = Dialogue(SheetPath,
                (Speaker.System, "SHIFT SCHEDULE"),
                (Speaker.System, ""),
                (Speaker.System, "Charles"), (Speaker.System, "Max"), (Speaker.System, "Danny"),
                (Speaker.System, "Charles"), (Speaker.System, "Max"), (Speaker.System, "Danny"),
                (Speaker.System, "Charles"), (Speaker.System, "Max"), (Speaker.System, "Danny"),
                (Speaker.System, "Charles"), (Speaker.System, "Max"), (Speaker.System, "Danny"),
                (Speaker.System, ""),
                (Speaker.System, "{name} — Status: Active"));
            var drawerText = Dialogue(DrawerPath,
                (Speaker.Description, "The same names repeat on every sheet: Charles, Max, Danny…"),
                (Speaker.Description, "At the very bottom, a fresh line: {name} — Status: Active."),
                (Speaker.Danny, "What is this nonsense…"),
                (Speaker.Danny, "Who's {name}? Nobody by that name ever worked in our department."));
            var drawerName = Dialogue(DrawerNamePath,
                (Speaker.Danny, "Wait. That's your name, isn't it?"),
                (Speaker.Danny, "How did it get here?.."));
            var monitor = Dialogue(MonitorPath, (Speaker.System, "REPORT NOT CLOSED.\nSHIFT CONTINUES."));
            var checkDocs = Objective(CheckDocsPath, "F2_CheckDocs201", "Check the documents in room 201.");
            // Второе задание в 201: после документов — разобраться с компьютером.
            var checkComputer = Objective(CheckComputerPath, "F2_CheckComputer201", "Deal with the computer.");

            Transform room = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Environment") room = root.transform.Find("Rooms/Room_201");
            if (room == null)
            {
                Debug.LogError("[Setup] Floor_02: нет Environment/Rooms/Room_201");
                return;
            }

            var props = room.Find("Props");
            if (props == null)
            {
                props = new GameObject("Props").transform;
                props.SetParent(room, false);
            }
            foreach (var name in new[] { "FilingCabinets", "Desk", "Computer" })
            {
                var old = props.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            // Пол комнаты — на 0.9 ниже точки входа (ноги Дэнни).
            var entry = room.Find("EntryPoint");
            float floorY = entry != null ? entry.localPosition.y - 0.9f : -4.2f;
            var square = SB.EnsureSquareSprite();

            // --- Картотечные шкафы (обстановка, без E): у левой стены.
            var cabinets = new GameObject("FilingCabinets");
            cabinets.transform.SetParent(props, false);
            for (int i = 0; i < 2; i++)
                SB.Rect(cabinets, $"Cabinet_{i}", square, new Vector2(-7f + i * 1.1f, floorY + 1.1f),
                    new Vector2(1f, 2.2f), new Color(0.42f, 0.45f, 0.48f), -8);

            // --- Стол с ящиками: слева от двери выхода (она по центру комнаты).
            var deskSize = new Vector2(2.4f, 1.1f);
            var desk = Thing(props, "Desk", -3.5f, floorY, deskSize);
            SB.Rect(desk, "Sprite", square, Vector2.zero, deskSize, new Color(0.36f, 0.3f, 0.24f), -7);
            RoomWiring.Set(desk.AddComponent<DrawerInteractable>(),
                ("prompt", "Inspect the drawer"),
                ("documentSheet", sheet),
                ("contentsDialogue", drawerText),
                ("afterPauseDialogue", drawerName),
                ("setsFlag", DocsReadFlag),
                ("completesObjective", checkDocs));
            // Документы прочитаны → задание «Deal with the computer.»
            var giveComputer = desk.AddComponent<ObjectiveGiver>();
            RoomWiring.Set(giveComputer, ("objective", checkComputer));
            UnityEditor.Events.UnityEventTools.AddPersistentListener(desk.GetComponent<DrawerInteractable>().onDone, giveComputer.Give);

            // --- Компьютер: тускло светящийся монитор на столике справа от двери выхода.
            var compSize = new Vector2(1.6f, 1.7f);
            var computer = Thing(props, "Computer", 4f, floorY, compSize);
            SB.Rect(computer, "Table", square, new Vector2(0f, -0.4f), new Vector2(1.6f, 0.9f), new Color(0.3f, 0.27f, 0.23f), -7);
            SB.Rect(computer, "Monitor", square, new Vector2(0f, 0.45f), new Vector2(0.9f, 0.7f), new Color(0.12f, 0.3f, 0.2f), -6);
            var game = computer.AddComponent<SignalTuningMiniGame>();
            RoomWiring.Set(game, ("screenText", monitor));
            RoomWiring.Set(computer.AddComponent<ComputerInteractable>(),
                ("prompt", "Check the computer"),
                ("miniGame", game),
                ("requiresFlag", DocsReadFlag),
                ("completesObjective", checkComputer));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(previous) && previous != ScenePath)
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log("[Setup] Floor_02: Room_201 — шкафы, стол с ящиком, компьютер с мини-игрой 3");
        }

        // Объект на полу: триггер нужного размера, низ — на линии пола.
        static GameObject Thing(Transform parent, string name, float x, float floorY, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, floorY + size.y / 2f, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
            return go;
        }

        static DialogueData Dialogue(string path, params (Speaker speaker, string text)[] lines)
        {
            var d = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (d != null) return d; // уже есть (мог сделать P3) — не трогаем
            d = ScriptableObject.CreateInstance<DialogueData>();
            d.Lines = new DialogueLine[lines.Length];
            for (int i = 0; i < lines.Length; i++) d.Lines[i] = new DialogueLine { Speaker = lines[i].speaker, Text = lines[i].text };
            AssetDatabase.CreateAsset(d, path);
            return d;
        }

        static ObjectiveData Objective(string path, string id, string title)
        {
            var o = AssetDatabase.LoadAssetAtPath<ObjectiveData>(path);
            if (o != null) return o;
            o = ScriptableObject.CreateInstance<ObjectiveData>();
            o.Id = id;
            o.Title = title;
            AssetDatabase.CreateAsset(o, path);
            return o;
        }
    }
}
