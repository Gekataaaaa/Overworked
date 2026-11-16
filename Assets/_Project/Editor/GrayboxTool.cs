using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SB = Overworked.EditorTools.PlayerSandboxBuilder;

namespace Overworked.EditorTools
{
    /// <summary>
    /// «Серая коробка» для открытой сцены (рабочие копии вроде Work_P1/Floor_03_Work):
    /// фон и декорации прячутся (их SpriteRenderer выключаются, коллайдеры и свет остаются),
    /// а каждый спрайт объекта для взаимодействия заменяется квадратом того же размера и места.
    /// Объекты для взаимодействия — всё, на чём есть IInteractable или Inspectable (и их дочерние спрайты).
    /// Если у такого объекта нет спрайтов — квадрат по размеру его коллайдера.
    /// Вместо фона в коридоре и каждой комнате — серая стена, чёрный пол ниже горизонта, тёмно-серый потолок (Unlit).
    /// Повторный запуск безопасен: пересобирает серый фон, уже заменённое не трогает.
    /// Игрок не трогается. Отменяется через Ctrl+Z или копированием сцены заново.
    /// Запуск: открыть сцену → меню Overworked → P1 → Graybox Open Scene.
    /// </summary>
    public static class GrayboxTool
    {
        const string GrayName = "~Graybox";
        static readonly Color InteractColor = new Color(0.78f, 0.74f, 0.62f);

        /// <summary>
        /// Без вступления: сценариям, которые запускаются сами при старте сцены (1.0 Opening — Дэнни сидит, реплики,
        /// HUD по частям, подсказка управления), выключается playOnStart. Сцена стартует с управлением сразу.
        /// Остальные сценарии (зоны, турникет) не трогаются. Отмена — Ctrl+Z.
        /// </summary>
        [MenuItem("Overworked/P1/Skip Opening In Open Scene")]
        public static void SkipOpening()
        {
            var scene = EditorSceneManager.GetActiveScene();
            int count = 0;
            foreach (var seq in Object.FindObjectsByType<ScriptedSequence>(FindObjectsInactive.Include))
            {
                var so = new SerializedObject(seq);
                var prop = so.FindProperty("playOnStart");
                if (prop == null || !prop.boolValue) continue;
                Undo.RecordObject(seq, "Skip Opening");
                prop.boolValue = false;
                so.ApplyModifiedProperties();
                Debug.Log($"[Graybox] {scene.name}: «{seq.name}» больше не запускается на старте");
                count++;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (count == 0) Debug.Log($"[Graybox] {scene.name}: сценариев на старте нет");
        }

        /// <summary>
        /// Удлиняет коридор этажа в 3 раза вперёд (вправо): правая стена и граница камеры уезжают, а всё, что стояло
        /// в конце коридора (турникет, лестница, лампы у них), переносится в новый конец. Начало коридора, кухня и
        /// объекты в середине остаются на месте. Серый фон пересобирается под новую длину. Отмена — Ctrl+Z.
        /// </summary>
        [MenuItem("Overworked/P1/Extend Corridor x3 In Open Scene")]
        public static void ExtendCorridor()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var corridor = GameObject.Find("Environment/Corridor");
            var bounds = corridor != null ? corridor.transform.Find("CameraBounds")?.GetComponent<PolygonCollider2D>() : null;
            var walls = corridor != null ? corridor.transform.Find("Walls") : null;
            Transform left = null, right = null;
            if (walls != null)
                foreach (Transform w in walls)
                {
                    if (w.name.Contains("Left")) left = w;
                    if (w.name.Contains("Right")) right = w;
                }
            if (bounds == null || left == null || right == null)
            {
                Debug.LogWarning($"[Graybox] {scene.name}: нет Environment/Corridor с CameraBounds и Walls/WallLeft, WallRight");
                return;
            }

            Undo.SetCurrentGroupName("Extend Corridor");
            int group = Undo.GetCurrentGroup();

            float length = right.position.x - left.position.x;
            float delta = length * 2f;                 // было L, станет 3L
            float endZone = right.position.x - 8f;     // всё, что правее, — «конец коридора»

            // 1. Правая стена и граница камеры.
            Undo.RecordObject(right, "Extend Corridor");
            right.position += Vector3.right * delta;
            Undo.RecordObject(bounds, "Extend Corridor");
            float maxLocal = float.MinValue;
            foreach (var p in bounds.points) maxLocal = Mathf.Max(maxLocal, p.x);
            var pts = bounds.points;
            for (int i = 0; i < pts.Length; i++)
                if (pts[i].x >= maxLocal - 0.01f) pts[i].x += delta / bounds.transform.lossyScale.x;
            bounds.points = pts;

            // 2. Конец коридора — в новый конец: турникет, лестница, лампы и прочее справа, кроме стен и фона.
            int moved = 0;
            var toMove = new List<Transform>();
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
                if ((mb is TurnstileInteractable || mb is StairInteractable) && mb.gameObject.scene == scene)
                    toMove.Add(mb.transform);
            var lights = corridor.transform.Find("Lights");
            if (lights != null)
                foreach (Transform l in lights)
                    if (l.position.x > endZone) toMove.Add(l);
            foreach (var t in toMove)
            {
                Undo.RecordObject(t, "Extend Corridor");
                t.position += Vector3.right * delta;
                moved++;
            }

            // 3. Серый фон под новую длину.
            BuildBackdrops(SB.EnsureSquareSprite());

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Graybox] {scene.name}: коридор удлинён ×3 (+{delta:0.#} юнитов вправо), перенесено в конец: {moved}");
        }

        const string SideName = "Corridor_B";
        static readonly Color PassageColor = new Color(0.06f, 0.06f, 0.07f);

        /// <summary>
        /// Второй коридор: проход шириной в две двери (тёмный прямоугольник) в центре основного коридора ведёт
        /// в пустой коридор длиной 2/3 основного, который стоит в сцене над этажом. Обратно — такой же проход
        /// в начале нового коридора (у левой стены), Дэнни появляется перед ним. Работает на механике комнат P2 (Room + DoorInteractable): затемнение,
        /// телепорт, своя камера с границами. Повторный запуск пересоздаёт второй коридор. Отмена — Ctrl+Z.
        /// </summary>
        [MenuItem("Overworked/P1/Add Side Corridor In Open Scene")]
        public static void AddSideCorridor()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var corridor = GameObject.Find("Environment/Corridor");
            var rooms = GameObject.Find("Environment/Rooms");
            var cameras = GameObject.Find("Cameras");
            var corridorCam = cameras != null ? cameras.transform.Find("CM Corridor") : null;
            var mainBounds = corridor != null ? corridor.transform.Find("CameraBounds")?.GetComponent<PolygonCollider2D>() : null;
            var walls = corridor != null ? corridor.transform.Find("Walls") : null;
            Transform left = null, right = null;
            if (walls != null)
                foreach (Transform w in walls)
                {
                    if (w.name.Contains("Left")) left = w;
                    if (w.name.Contains("Right")) right = w;
                }
            if (corridor == null || rooms == null || corridorCam == null || mainBounds == null || left == null || right == null)
            {
                Debug.LogWarning($"[Graybox] {scene.name}: нужен каркас этажа (Environment/Corridor, Rooms, Cameras/CM Corridor, стены)");
                return;
            }

            Undo.SetCurrentGroupName("Add Side Corridor");
            int group = Undo.GetCurrentGroup();
            var square = SB.EnsureSquareSprite();
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitPath);

            // Старый второй коридор, его камеру, проход и точку возврата — убираем (пересборка).
            foreach (var old in new[] { rooms.transform.Find(SideName), cameras.transform.Find("CM " + SideName),
                         corridor.transform.Find("Doors/Door_" + SideName), corridor.transform.Find("ReturnPoints/" + SideName) })
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

            float mainLength = right.position.x - left.position.x;
            float centerX = (left.position.x + right.position.x) / 2f;
            float halfB = mainLength / 3f;                       // длина 2/3 основного → половина 1/3
            float bottom = float.MaxValue, top = float.MinValue;
            foreach (var p in mainBounds.points) { bottom = Mathf.Min(bottom, p.y); top = Mathf.Max(top, p.y); }
            float originY = corridor.transform.position.y + (top - bottom) + 10f;   // над этажом, с запасом

            // 1. Сам коридор: корень-«комната», стены, границы камеры, точка появления.
            var root = new GameObject(SideName);
            Undo.RegisterCreatedObjectUndo(root, "Add Side Corridor");
            root.transform.SetParent(rooms.transform, false);
            root.transform.position = new Vector3(centerX, originY, 0f);

            var wallsB = new GameObject("Walls");
            wallsB.transform.SetParent(root.transform, false);
            foreach (var (name, x) in new[] { ("WallLeft", -halfB - 0.5f), ("WallRight", halfB + 0.5f) })
            {
                var w = new GameObject(name);
                w.transform.SetParent(wallsB.transform, false);
                w.transform.localPosition = new Vector3(x, 0f, 0f);
                w.AddComponent<BoxCollider2D>().size = new Vector2(1f, top - bottom);
            }

            var boundsGo = new GameObject("CameraBounds");
            boundsGo.transform.SetParent(root.transform, false);
            var bounds = boundsGo.AddComponent<PolygonCollider2D>();
            bounds.isTrigger = true;
            float bx = halfB + 1f;
            bounds.points = new[] { new Vector2(-bx, bottom), new Vector2(bx, bottom), new Vector2(bx, top), new Vector2(-bx, top) };

            // Вход и обратный проход — в начале нового коридора (у левой стены): Дэнни появляется перед проходом.
            float startX = -halfB + 2.5f;
            var entry = new GameObject("EntryPoint").transform;
            entry.SetParent(root.transform, false);
            entry.localPosition = new Vector3(startX, FloorY + 0.9f, 0f);

            // 2. Камера коридора B — как у комнат: префаб камеры игрока, свои границы, выключена до входа.
            var camPrefab = SB.EnsureCameraPrefab();
            var cam = (GameObject)PrefabUtility.InstantiatePrefab(camPrefab, scene);
            Undo.RegisterCreatedObjectUndo(cam, "Add Side Corridor");
            cam.name = "CM " + SideName;
            cam.transform.SetParent(cameras.transform, false);
            cam.transform.position = new Vector3(centerX, originY, cam.transform.position.z);
            cam.GetComponent<Unity.Cinemachine.CinemachineConfiner2D>().BoundingShape2D = bounds;
            cam.SetActive(false);

            // 3. Room + точка возврата в основном коридоре.
            var back = new GameObject(SideName).transform;
            var returnParent = corridor.transform.Find("ReturnPoints");
            if (returnParent == null)
            {
                returnParent = new GameObject("ReturnPoints").transform;
                returnParent.SetParent(corridor.transform, false);
            }
            back.SetParent(returnParent, false);
            back.position = new Vector3(centerX, corridor.transform.position.y + FloorY + 0.9f, 0f);
            Undo.RegisterCreatedObjectUndo(back.gameObject, "Add Side Corridor");

            var room = root.AddComponent<Room>();
            RoomWiring.Set(room, ("entryPoint", entry), ("returnPoint", back), ("roomCamera", cam), ("corridorCamera", corridorCam.gameObject));

            // 4. Проходы: туда — в центре основного коридора, обратно — в начале нового.
            // Размер — по двери в кухню: та же высота, ширина в две такие двери.
            var size = KitchenDoorSize();
            var passageSize = new Vector2(size.x * 2f, size.y);
            var doorsParent = corridor.transform.Find("Doors");
            Passage(doorsParent != null ? doorsParent : corridor.transform, "Door_" + SideName,
                new Vector3(centerX, corridor.transform.position.y + FloorY, 0f), room,
                DoorInteractable.Direction.IntoRoom, "Go through", passageSize, square, unlit);
            Passage(root.transform, "ExitPassage", new Vector3(centerX + startX, originY + FloorY, 0f), room,
                DoorInteractable.Direction.OutToCorridor, "Go back", passageSize, square, unlit);

            // 5. Серый фон — и в новом коридоре.
            BuildBackdrops(square);

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Graybox] {scene.name}: второй коридор {SideName} ({halfB * 2f:0.#} юнитов) над этажом, проход в центре (x={centerX:0.#})");
        }

        /// <summary>
        /// Размер двери в кухню по её спрайтам (в серой коробке — по квадрату). Нет кухни — дверь каркаса 1.4 × 2.5.
        /// Высота — от пола (FloorY) до верха двери: проход тоже стоит на полу.
        /// </summary>
        static Vector2 KitchenDoorSize()
        {
            foreach (var door in Object.FindObjectsByType<DoorInteractable>(FindObjectsInactive.Include))
            {
                var so = new SerializedObject(door);
                var room = so.FindProperty("room").objectReferenceValue as Component;
                if (room == null || !room.name.Contains("Kitchen") ||
                    so.FindProperty("direction").enumValueIndex != (int)DoorInteractable.Direction.IntoRoom) continue;
                bool any = false;
                var b = new Bounds();
                foreach (var sr in door.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.sprite == null) continue;
                    if (!any) { b = sr.bounds; any = true; } else b.Encapsulate(sr.bounds);
                }
                if (any) return new Vector2(b.size.x, b.max.y - (door.transform.root.position.y + FloorY));
            }
            return new Vector2(1.4f, 2.5f);
        }

        /// <summary>Проход шириной в две двери: тёмный прямоугольник от пола, триггер, DoorInteractable.</summary>
        static void Passage(Transform parent, string name, Vector3 floorPos, Room room, DoorInteractable.Direction dir,
                            string prompt, Vector2 size, Sprite square, Material unlit)
        {
            float width = size.x, height = size.y;
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Add Side Corridor");
            go.transform.SetParent(parent, true);
            go.transform.position = floorPos + Vector3.up * (height / 2f);

            var visual = new GameObject("~Graybox");
            visual.transform.SetParent(go.transform, false);
            var s = square.bounds.size;
            visual.transform.localScale = new Vector3(width / s.x, height / s.y, 1f);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = PassageColor;
            sr.sortingOrder = -8;
            if (unlit != null) sr.sharedMaterial = unlit;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(width, height);
            var door = go.AddComponent<DoorInteractable>();
            RoomWiring.Set(door, ("room", room), ("direction", dir), ("prompt", prompt));
        }

        // Компоненты сюжета: сценарии, зоны-триггеры, галлюцинации, вступление HUD.
        static readonly HashSet<string> StoryComponents = new HashSet<string>
        {
            "ScriptedSequence", "SequenceTrigger", "RoomSequenceTrigger", "FlashlightSequenceTrigger",
            "NoReturnTrigger", "HudOnboarding", "ObjectiveTrigger", "ObjectiveGiver", "MessageTrigger",
            "HallucinationTrigger", "HallucinationEvent", "SilhouetteBehaviour", "Inspectable",
        };

        /// <summary>
        /// Голая сцена: убирает весь сюжет — сценарии, зоны-триггеры, сообщения в телефон, реплики и описания,
        /// галлюцинации. Остаются интерактивные объекты (двери, пропуск, записка, турникет, лестница) и HUD.
        /// У объектов вычищаются ссылки на реплики/сообщения и подписки на сценарии, снимаются замки и условия
        /// по заданиям (их раньше открывал сюжет) — всё доступно сразу. Отмена — Ctrl+Z.
        /// </summary>
        [MenuItem("Overworked/P1/Strip Story In Open Scene")]
        public static void StripStory()
        {
            var scene = EditorSceneManager.GetActiveScene();
            Undo.SetCurrentGroupName("Strip Story");
            int group = Undo.GetCurrentGroup();
            int removed = 0, cleaned = 0;

            // 1. Компоненты сюжета — удаляем (выключенный скрипт всё равно получает OnTriggerEnter2D).
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (mb == null || mb.gameObject.scene != scene || !StoryComponents.Contains(mb.GetType().Name)) continue;
                Undo.DestroyObjectImmediate(mb);
                removed++;
            }
            // Пустые группы сюжета прячем целиком (силуэты, лампы галлюцинаций).
            foreach (var root in scene.GetRootGameObjects())
                if ((root.name == "Story" || root.name == "Hallucinations") && root.activeSelf)
                {
                    Undo.RecordObject(root, "Strip Story");
                    root.SetActive(false);
                }

            // 2. Интерактивные объекты: без реплик и сообщений, без сюжетных замков.
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (mb == null || mb.gameObject.scene != scene) continue;
                if (!(mb is IInteractable)) continue;
                var so = new SerializedObject(mb);
                bool changed = false;
                var calls = new List<string>();
                var it = so.GetIterator();
                while (it.Next(true))
                {
                    if (it.propertyType == SerializedPropertyType.ObjectReference &&
                        (it.objectReferenceValue is DialogueData || it.objectReferenceValue is MessageData))
                    {
                        it.objectReferenceValue = null;
                        changed = true;
                    }
                    if (it.isArray && it.propertyPath.EndsWith("m_PersistentCalls.m_Calls")) calls.Add(it.propertyPath);
                }
                // Подписки событий (onDenied турникета и т. п.) на удалённые сценарии — пустые, выкидываем.
                foreach (var path in calls)
                {
                    var arr = so.FindProperty(path);
                    for (int i = arr.arraySize - 1; i >= 0; i--)
                    {
                        var target = arr.GetArrayElementAtIndex(i).FindPropertyRelative("m_Target");
                        if (target != null && target.objectReferenceValue == null)
                        {
                            arr.DeleteArrayElementAtIndex(i);
                            changed = true;
                        }
                    }
                }
                foreach (var name in new[] { "startLocked", "doorLocked" })
                {
                    var p = so.FindProperty(name);
                    if (p != null && p.boolValue) { p.boolValue = false; changed = true; }
                }
                var req = so.FindProperty("requiresObjective");
                if (req != null && req.objectReferenceValue != null) { req.objectReferenceValue = null; changed = true; }
                if (changed) { Undo.RecordObject(mb, "Strip Story"); so.ApplyModifiedProperties(); cleaned++; }
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Graybox] {scene.name}: сюжет убран — компонентов удалено {removed}, объектов очищено {cleaned}");
        }

        /// <summary>
        /// Бег со стаминой (Shift) на игроке открытой сцены: allowRun + материал полоски (Unlit — видна в темноте).
        /// Пока механика не утверждена — только в рабочих сценах. Переопределение префаба Player в этой сцене.
        /// </summary>
        [MenuItem("Overworked/P1/Enable Running In Open Scene")]
        public static void EnableRunning()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var player = Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (player == null) { Debug.LogWarning($"[Graybox] {scene.name}: нет PlayerController"); return; }
            var so = new SerializedObject(player);
            Undo.RecordObject(player, "Enable Running");
            so.FindProperty("allowRun").boolValue = true;
            so.FindProperty("staminaHudMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(UnlitPath);
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Graybox] {scene.name}: бег на Shift со стаминой включён");
        }

        [MenuItem("Overworked/P1/Graybox Open Scene")]
        public static void Run()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path.EndsWith("/Floor_03.unity") || scene.path.EndsWith("/Floor_02.unity") ||
                scene.path.EndsWith("/Floor_01.unity") || scene.path.EndsWith("/WhiteRoom.unity"))
            {
                // Основные сцены общие — серую коробку делаем только в рабочих копиях.
                if (!EditorUtility.DisplayDialog("Graybox", $"Это основная сцена {scene.name}. Всё равно сделать серую коробку?",
                        "Да", "Отмена")) return;
            }

            var square = SB.EnsureSquareSprite();
            Undo.SetCurrentGroupName("Graybox Open Scene");
            int group = Undo.GetCurrentGroup();

            // 1. Корни объектов для взаимодействия.
            var interactRoots = new HashSet<GameObject>();
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
                if (mb is IInteractable || mb is Inspectable) interactRoots.Add(mb.gameObject);

            int hidden = 0, boxes = 0;
            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            {
                if (IsGraybox(sr.transform)) continue;   // наши квадраты и серый фон при повторном запуске
                if (sr.GetComponentInParent<PlayerController>(true) != null) continue;
                bool interactive = IsInside(sr.transform, interactRoots);
                if (!sr.enabled) continue;
                // Фон — только то, что в Environment (стены, мебель, лампы, комнаты). Силуэты галлюцинаций и прочее не трогаем.
                if (!interactive && sr.transform.root.name != "Environment") continue;

                Undo.RecordObject(sr, "Graybox");
                sr.enabled = false;
                if (!interactive) { hidden++; continue; }
                AddBox(sr.transform, LocalBounds(sr), sr.sortingLayerID, sr.sortingOrder, square);
                boxes++;
            }

            // 2. Объекты для взаимодействия без спрайтов — квадрат по коллайдеру.
            foreach (var root in interactRoots)
            {
                // Зона описания (Inspectable без E) — это область подхода, а не предмет: квадрат не нужен.
                if (root.GetComponent<IInteractable>() == null) continue;
                if (root.GetComponentInChildren<SpriteRenderer>(true) != null) continue;
                var col = root.GetComponent<Collider2D>();
                if (col == null) continue;
                var b = new Bounds(root.transform.InverseTransformPoint(col.bounds.center),
                    Divide(col.bounds.size, root.transform.lossyScale));
                AddBox(root.transform, b, 0, 5, square);
                boxes++;
            }

            // 3. Свой простой фон вместо спрятанного: серая стена, чёрный пол ниже горизонта, тёмно-серый потолок.
            int areas = BuildBackdrops(square);

            // 4. Мебель, без которой объект «висит в воздухе»: записка Чарльза — на мониторе, монитор — на столе.
            BuildCharlesDesk(square);

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Graybox] {scene.name}: фон спрятан ({hidden} спрайтов), квадратов для взаимодействия: {boxes}, " +
                      $"серый фон в зонах: {areas}");
        }

        // ------------------------------------------------------------ свой фон

        const string BackdropName = "~GrayBackdrop";
        const float FloorY = -4.2f;    // верх пола относительно коридора/комнаты (как в FloorSkeletonBuilder)
        const float CeilingY = 3.6f;   // низ потолка
        static readonly Color WallColor = new Color(0.45f, 0.46f, 0.45f);
        static readonly Color FloorColor = Color.black;
        static readonly Color CeilingColor = new Color(0.18f, 0.18f, 0.19f);

        /// <summary>
        /// В коридоре и в каждой комнате (по их CameraBounds) — три полосы на всю ширину: пол, стена, потолок.
        /// Материал Unlit: на тёмных этажах (2D-свет) серая коробка видна без ламп.
        /// </summary>
        static int BuildBackdrops(Sprite square)
        {
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitPath);
            int count = 0;
            foreach (var col in Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Include))
            {
                if (col.name != "CameraBounds" || col.transform.root.name != "Environment") continue;
                var area = col.transform.parent;               // Corridor или комната
                var old = area.Find(BackdropName);
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

                var group = new GameObject(BackdropName);
                Undo.RegisterCreatedObjectUndo(group, "Graybox");
                group.transform.SetParent(area, false);

                var b = col.bounds;
                float originY = area.position.y;
                float floorTop = originY + FloorY, ceiling = originY + CeilingY;
                Strip(group, "Floor", square, unlit, b.min.x, b.max.x, b.min.y, floorTop, FloorColor);
                Strip(group, "Wall", square, unlit, b.min.x, b.max.x, floorTop, ceiling, WallColor);
                Strip(group, "Ceiling", square, unlit, b.min.x, b.max.x, ceiling, b.max.y, CeilingColor);
                count++;
            }
            return count;
        }

        static void Strip(GameObject parent, string name, Sprite square, Material mat,
                          float xMin, float xMax, float yMin, float yMax, Color color, int order = -100)
        {
            if (yMax <= yMin) return;
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, true);
            go.transform.position = new Vector3((xMin + xMax) / 2f, (yMin + yMax) / 2f, 0f);
            var s = square.bounds.size;
            var ps = parent.transform.lossyScale;
            go.transform.localScale = new Vector3((xMax - xMin) / s.x / ps.x, (yMax - yMin) / s.y / ps.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = color;
            sr.sortingOrder = order;
            if (mat != null) sr.sharedMaterial = mat;
        }

        // ------------------------------------------------------------ мебель

        const string PropsName = "~GrayProps";
        static readonly Color DeskColor = new Color(0.32f, 0.28f, 0.24f);
        static readonly Color MonitorColor = new Color(0.12f, 0.13f, 0.14f);
        static readonly Color ScreenColor = new Color(0.22f, 0.3f, 0.32f);

        /// <summary>
        /// Стол с компьютером под запиской Чарльза (нарратив 1.1: «на мониторе прикреплена записка»).
        /// Монитор — по центру записки, под ним подставка и стол до пола. Всё за запиской, чтобы её было видно и брать по E.
        /// </summary>
        static void BuildCharlesDesk(Sprite square)
        {
            var old = GameObject.Find(PropsName);
            if (old != null) Undo.DestroyObjectImmediate(old);
            var note = GameObject.Find("CharlesNote");
            if (note == null) return;

            var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitPath);
            var root = new GameObject(PropsName);
            Undo.RegisterCreatedObjectUndo(root, "Graybox");

            Vector3 n = note.transform.position;
            float floorTop = FloorY;                 // записка в коридоре: пол коридора
            const float deskTop = -2.9f, deskWidth = 3.2f;
            const float monitorW = 1.6f, monitorH = 1.15f;
            // Монитор центрируем по записке, но не ниже стола.
            float monitorCy = Mathf.Max(n.y, deskTop + 0.35f + monitorH / 2f);

            Strip(root, "Desk", square, unlit, n.x - deskWidth / 2f, n.x + deskWidth / 2f, floorTop, deskTop, DeskColor, -60);
            Strip(root, "MonitorStand", square, unlit, n.x - 0.12f, n.x + 0.12f, deskTop, monitorCy - monitorH / 2f, MonitorColor, -55);
            Strip(root, "Monitor", square, unlit, n.x - monitorW / 2f, n.x + monitorW / 2f,
                monitorCy - monitorH / 2f, monitorCy + monitorH / 2f, MonitorColor, -55);
            Strip(root, "Screen", square, unlit, n.x - monitorW / 2f + 0.1f, n.x + monitorW / 2f - 0.1f,
                monitorCy - monitorH / 2f + 0.1f, monitorCy + monitorH / 2f - 0.1f, ScreenColor, -54);
            // Записка должна быть на мониторе: если она ниже/выше — подвинем её на экран.
            if (Mathf.Abs(n.y - monitorCy) > 0.01f)
            {
                Undo.RecordObject(note.transform, "Graybox");
                note.transform.position = new Vector3(n.x, monitorCy, n.z);
            }
        }

        const string UnlitPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        static bool IsGraybox(Transform t)
        {
            for (var p = t; p != null; p = p.parent)
                if (p.name.StartsWith("~")) return true;
            return false;
        }

        static bool IsInside(Transform t, HashSet<GameObject> roots)
        {
            for (var p = t; p != null; p = p.parent)
                if (roots.Contains(p.gameObject)) return true;
            return false;
        }

        /// <summary>Границы спрайта в локальных координатах его объекта (с учётом pivot, режима отрисовки и flip).</summary>
        static Bounds LocalBounds(SpriteRenderer sr)
        {
            if (sr.drawMode != SpriteDrawMode.Simple) return new Bounds(Vector3.zero, sr.size);
            if (sr.sprite == null) return new Bounds(Vector3.zero, Vector3.one);
            var b = sr.sprite.bounds;
            var c = b.center;
            if (sr.flipX) c.x = -c.x;
            if (sr.flipY) c.y = -c.y;
            return new Bounds(c, b.size);
        }

        static void AddBox(Transform parent, Bounds local, int sortingLayer, int order, Sprite square)
        {
            var go = new GameObject(GrayName);
            Undo.RegisterCreatedObjectUndo(go, "Graybox");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(local.center.x, local.center.y, 0f);
            var box = go.AddComponent<SpriteRenderer>();
            box.sprite = square;
            // Квадрат растягиваем масштабом до размера спрайта, который он заменяет (в координатах родителя).
            var s = square.bounds.size;
            go.transform.localScale = new Vector3(Mathf.Max(0.05f, local.size.x) / s.x, Mathf.Max(0.05f, local.size.y) / s.y, 1f);
            box.color = InteractColor;
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitPath);
            if (unlit != null) box.sharedMaterial = unlit;   // видно и на тёмном этаже
            box.sortingLayerID = sortingLayer;
            box.sortingOrder = order;
        }

        static Vector3 Divide(Vector3 a, Vector3 b) =>
            new Vector3(a.x / (Mathf.Approximately(b.x, 0f) ? 1f : b.x), a.y / (Mathf.Approximately(b.y, 0f) ? 1f : b.y), 1f);
    }
}
