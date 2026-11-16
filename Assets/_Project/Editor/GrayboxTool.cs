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
