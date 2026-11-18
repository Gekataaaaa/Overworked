using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SB = Overworked.EditorTools.PlayerSandboxBuilder;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Песочница P2: коридор и две комнаты на серых прямоугольниках со всеми объектами взаимодействия —
    /// записка, пропуск, запертая дверь, дверь в комнату, ящик с ключом, бормочущий человек, турникет
    /// со считывателем, дверь на ключ, аркадный автомат с Tiny UFO. Задания и тексты — тестовые,
    /// лежат рядом со сценой (Sandbox_P2_Data), настоящие данные этажей делает P3.
    /// Запуск: Overworked → P2 → Sandbox → Build Interaction Sandbox. Пересобирает сцену целиком.
    /// </summary>
    public static class P2SandboxBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Sandbox/Sandbox_P2.unity";
        const string DataDir = "Assets/_Project/Scenes/Sandbox/Sandbox_P2_Data";
        const string FontPath = "Assets/_Project/Fonts/PTSans SDF.asset";
        const float HalfWidth = 30f, HalfHeight = 5.4f, FloorY = -4.2f, Margin = 0.25f, RoomHalfWidth = 9f;

        static Sprite square;
        static TMP_FontAsset font;

        [MenuItem("Overworked/P2/Sandbox/Build Interaction Sandbox")]
        public static void Run()
        {
            square = SB.EnsureSquareSprite();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var playerPrefab = SB.EnsurePlayerPrefab(square);
            var cameraPrefab = SB.EnsureCameraPrefab();

            // --- Тестовые данные
            if (!AssetDatabase.IsValidFolder(DataDir)) AssetDatabase.CreateFolder("Assets/_Project/Scenes/Sandbox", "Sandbox_P2_Data");
            var takePass = Objective("T_TakeMaxPass", "Take Max's pass");
            var findKey = Objective("T_FindKey", "Find the key in the side room");
            var play = Objective("T_PlayArcade", "Finish work: play one round");
            var noteLines = Dialogue("T_NoteAfter", (Speaker.Danny, "That's for sure..."));
            var drawerLines = Dialogue("T_DrawerContents",
                (Speaker.Description, "A heavy key and a cracked plastic badge."),
                (Speaker.Description, "The badge says: {name}, Senior Operator."),
                (Speaker.Danny, "Senior Operator?.."));
            var deskLines = Dialogue("T_DeskDescription",
                (Speaker.Description, "Trash, crumpled sheets and a dusty nameplate: Charles."),
                (Speaker.Description, "There's a note stuck to the monitor."));

            // --- Сцена
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects())
                if (go.name is "Environment" or "Player" or "Cameras" or "Interactables" or "Story")
                    Object.DestroyImmediate(go);

            var mainCam = GameObject.Find("Main Camera");
            if (mainCam != null && mainCam.GetComponent<CinemachineBrain>() == null) mainCam.AddComponent<CinemachineBrain>();

            var env = new GameObject("Environment");
            var cameras = new GameObject("Cameras");
            var inter = new GameObject("Interactables");

            // Коридор
            var corridor = Child(env, "Corridor");
            Room(corridor, HalfWidth, new Color(0.3f, 0.34f, 0.31f), new Color(0.19f, 0.21f, 0.2f));
            var corridorCam = MakeCam(cameras, cameraPrefab, "CM Corridor", Bounds(corridor, HalfWidth + 1f));

            // Структура как у каркаса этажа P1 (Environment/Rooms/Room_X, Corridor/Doors/Door_Room_X,
            // Cameras/CM Room_X): двери потом разводит тот же инструмент, что и на этажах (RoomWiring).
            var rooms = Child(env, "Rooms");
            var doors = Child(corridor, "Doors");
            var roomA = Child(rooms, "Room_A");
            roomA.transform.position = new Vector3(0, -20, 0);
            Room(roomA, RoomHalfWidth, new Color(0.25f, 0.3f, 0.34f), new Color(0.16f, 0.18f, 0.2f));
            var camA = MakeCam(cameras, cameraPrefab, "CM Room_A", Bounds(roomA, RoomHalfWidth + 1f));
            camA.SetActive(false);

            var roomB = Child(rooms, "Room_B");
            roomB.transform.position = new Vector3(0, -40, 0);
            Room(roomB, RoomHalfWidth, new Color(0.3f, 0.28f, 0.33f), new Color(0.18f, 0.17f, 0.2f));
            var camB = MakeCam(cameras, cameraPrefab, "CM Room_B", Bounds(roomB, RoomHalfWidth + 1f));
            camB.SetActive(false);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.position = new Vector3(-27f, FloorY + 0.9f, 0);

            // Задание выдаётся на старте
            var start = Thing(inter, "StartObjective", -27f, new Vector2(2f, 3f), Color.clear, null);
            var trigger = start.AddComponent<ObjectiveTrigger>();
            Set(trigger, ("objective", takePass));

            // 1. Стол с запиской: описание при подходе + записка по E
            var desk = Thing(inter, "Desk_Note", -21f, new Vector2(2.4f, 1.2f), new Color(0.45f, 0.4f, 0.3f), "NOTE");
            Set(desk.AddComponent<Inspectable>(), ("dialogue", deskLines));
            Set(desk.AddComponent<NoteInteractable>(), ("prompt", "Take note"),
                ("noteText", "THE BOSS DOESN'T LIKE IT WHEN I LEAVE BEFORE HIM!!!"),
                ("receivedToast", "Received: Charles's note"), ("afterDialogue", noteLines));

            // 2. Пропуск: ставит MaxPass и закрывает задание
            var pass = Thing(inter, "Pickup_MaxPass", -15f, new Vector2(0.6f, 0.4f), new Color(0.9f, 0.85f, 0.4f), "PASS");
            Set(pass.AddComponent<PickupInteractable>(), ("prompt", "Pick up pass"),
                ("receivedToast", "Received: Max's pass"), ("setsFlag", "MaxPass"), ("completesObjective", takePass));

            // 3. Декоративная дверь (Door_<число>): просто картинка, E на ней нет
            Thing(doors, "Door_-9", -9f, new Vector2(1.4f, 2.5f), new Color(0.38f, 0.35f, 0.3f), "NO ENTRY");

            // 4. Комната A: дверь в коридоре, точка входа, дверь выхода
            Thing(doors, "Door_Room_A", -3f, new Vector2(1.4f, 2.5f), new Color(0.55f, 0.47f, 0.3f), "ROOM A");
            Point(roomA, "EntryPoint", 0f);
            Thing(Child(roomA, "ExitDoor"), "Door", 0f, new Vector2(1.4f, 2.5f), new Color(0.55f, 0.47f, 0.3f), "EXIT");

            // В комнате A: ящик с ключом и бормочущий человек
            var drawer = Thing(roomA, "Drawer_Key", 3f, new Vector2(1.6f, 1.2f), new Color(0.4f, 0.32f, 0.25f), "DRAWER");
            Set(drawer.AddComponent<DrawerInteractable>(), ("prompt", "Open drawer"), ("contentsDialogue", drawerLines),
                ("receivedToast", "Received: Key to room B"), ("setsFlag", "KeyB"), ("completesObjective", findKey));
            var person = Thing(roomA, "Person", 6f, new Vector2(0.9f, 1.6f), new Color(0.1f, 0.1f, 0.1f), null);
            Set(person.AddComponent<PersonInteractable>(), ("prompt", "Interact"));

            // 5. Турникет: своя карта — отказ, с MaxPass — считыватель
            var turnstile = Thing(inter, "Turnstile", 4f, new Vector2(0.8f, 1.8f), new Color(0.5f, 0.55f, 0.6f), "TURNSTILE");
            var reader = turnstile.AddComponent<CardReaderMiniGame>();
            Set(turnstile.AddComponent<TurnstileInteractable>(), ("cardReader", reader));

            // 6. Комната B — на ключ KeyB; попытка без ключа выдаёт задание
            var doorB = Thing(doors, "Door_Room_B", 12f, new Vector2(1.4f, 2.5f), new Color(0.55f, 0.47f, 0.3f), "ROOM B");
            Point(roomB, "EntryPoint", 0f);
            Thing(Child(roomB, "ExitDoor"), "Door", 0f, new Vector2(1.4f, 2.5f), new Color(0.55f, 0.47f, 0.3f), "EXIT");

            // Разводка комнат — тем же инструментом, что и этажи
            RoomWiring.Wire(scene);
            var doorBComp = doorB.GetComponent<DoorInteractable>();
            Set(doorBComp, ("doorLocked", true), ("unlockFlag", "KeyB"), ("lockedMessage", "Locked. The key must be in room A."));
            // Задание «найди ключ» — при первой попытке открыть запертую дверь
            var giveKeyTask = doorB.AddComponent<ObjectiveGiver>();
            Set(giveKeyTask, ("objective", findKey));
            UnityEditor.Events.UnityEventTools.AddPersistentListener(doorBComp.onLockedTried, giveKeyTask.Give);
            // Задание «сыграй» — при входе в комнату B
            var playTask = Thing(roomB, "PlayObjective", 0f, new Vector2(2f, 3f), Color.clear, null);
            Set(playTask.AddComponent<ObjectiveTrigger>(), ("objective", play));

            // 7. Аркадный автомат с Tiny UFO
            var arcade = Thing(roomB, "Arcade", 4f, new Vector2(1.4f, 2.6f), new Color(0.2f, 0.25f, 0.5f), "ARCADE");
            var ufo = arcade.AddComponent<TinyUfoMiniGame>();
            Set(arcade.AddComponent<ArcadeInteractable>(), ("prompt", "Play"), ("miniGame", ufo), ("completesObjective", play));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Sandbox_P2: песочница взаимодействий готова");
        }

        // ---------------------------------------------------------------- помощники

        static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static void Room(GameObject root, float halfWidth, Color wall, Color floor)
        {
            float top = HalfHeight + Margin, bottom = -HalfHeight - Margin;
            SB.Rect(root, "BackWall", square, new Vector2(0, (top + FloorY) / 2), new Vector2(halfWidth * 2, top - FloorY), wall, -10);
            SB.Rect(root, "Floor", square, new Vector2(0, (FloorY + bottom) / 2), new Vector2(halfWidth * 2, FloorY - bottom), floor, -5);
            foreach (var x in new[] { -halfWidth - 0.5f, halfWidth + 0.5f })
                SB.Rect(root, "Wall", square, new Vector2(x, 0), new Vector2(1f, (HalfHeight + Margin) * 2), new Color(wall.r * 0.5f, wall.g * 0.5f, wall.b * 0.5f), -4)
                    .AddComponent<BoxCollider2D>();
        }

        static PolygonCollider2D Bounds(GameObject root, float halfWidth)
        {
            var go = Child(root, "CameraBounds");
            var poly = go.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            float top = HalfHeight + Margin, bottom = -HalfHeight - Margin;
            poly.points = new[]
            {
                new Vector2(-halfWidth, bottom), new Vector2(halfWidth, bottom),
                new Vector2(halfWidth, top), new Vector2(-halfWidth, top),
            };
            return poly;
        }

        static GameObject MakeCam(GameObject parent, GameObject prefab, string name, PolygonCollider2D bounds)
        {
            var cam = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            cam.name = name;
            cam.transform.SetParent(parent.transform, false);
            cam.GetComponent<CinemachineConfiner2D>().BoundingShape2D = bounds;
            return cam;
        }

        static GameObject Point(GameObject parent, string name, float x)
        {
            var go = Child(parent, name);
            go.transform.localPosition = new Vector3(x, FloorY + 0.9f, 0);
            return go;
        }

        /// <summary>Объект на полу: триггер нужного размера, серый прямоугольник и подпись сверху.</summary>
        static GameObject Thing(GameObject parent, string name, float x, Vector2 size, Color color, string label)
        {
            var go = Child(parent, name);
            go.transform.localPosition = new Vector3(x, FloorY + size.y / 2f, 0);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
            if (color.a > 0f) SB.Rect(go, "Sprite", square, Vector2.zero, size, color, -7);
            if (!string.IsNullOrEmpty(label))
            {
                var l = Child(go, "Label");
                l.transform.localPosition = new Vector3(0, size.y / 2f + 0.5f, 0);
                var tmp = l.AddComponent<TextMeshPro>();
                if (font != null) tmp.font = font;
                tmp.text = label;
                tmp.fontSize = 3f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.85f, 0.85f, 0.8f);
                tmp.sortingOrder = -6;
                tmp.rectTransform.sizeDelta = new Vector2(5f, 1f);
            }
            return go;
        }

        static ObjectiveData Objective(string id, string title)
        {
            string path = $"{DataDir}/{id}.asset";
            var o = AssetDatabase.LoadAssetAtPath<ObjectiveData>(path);
            if (o == null)
            {
                o = ScriptableObject.CreateInstance<ObjectiveData>();
                AssetDatabase.CreateAsset(o, path);
            }
            o.Id = id;
            o.Title = title;
            EditorUtility.SetDirty(o);
            return o;
        }

        static DialogueData Dialogue(string id, params (Speaker speaker, string text)[] lines)
        {
            string path = $"{DataDir}/{id}.asset";
            var d = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (d == null)
            {
                d = ScriptableObject.CreateInstance<DialogueData>();
                AssetDatabase.CreateAsset(d, path);
            }
            d.Lines = new DialogueLine[lines.Length];
            for (int i = 0; i < lines.Length; i++) d.Lines[i] = new DialogueLine { Speaker = lines[i].speaker, Text = lines[i].text };
            EditorUtility.SetDirty(d);
            return d;
        }

        /// <summary>Заполняет [SerializeField]-поля по имени: ссылки, строки, bool.</summary>
        static void Set(Object target, params (string field, object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in values)
            {
                var p = so.FindProperty(field);
                if (p == null)
                {
                    Debug.LogError($"[Setup] {target.GetType().Name}: нет поля {field}");
                    continue;
                }
                switch (value)
                {
                    case Object o: p.objectReferenceValue = o; break;
                    case string s: p.stringValue = s; break;
                    case bool b: p.boolValue = b; break;
                    case float f: p.floatValue = f; break;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
