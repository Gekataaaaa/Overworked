using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Overworked
{
    /// <summary>
    /// Сохранение на диск: Continue после перезапуска игры продолжает ровно с того места, где остановились.
    /// В файл (Application.persistentDataPath/overworked_save.json) пишется:
    /// — состояние игры: имя, этаж, флаги, задания (выполненные и активные), история телефона;
    /// — снимок этажа: позиция и комната Дэнни, рассудок, какие объекты сцены включены/выключены/уничтожены
    ///   относительно исходной сцены и одноразовые флажки компонентов (сценарий отыгран, триггер сработал,
    ///   предмет использован, дверь заперта, фонарик подобран/включён — см. StateFields).
    /// Когда пишется: при входе на этаж (чекпоинт без снимка — этаж с начала), автосохранение каждые
    /// autosaveInterval секунд, при выходе в меню из паузы и при закрытии игры — только если Дэнни свободен и
    /// ничего не идёт (StoryBusy): полусыгранный сценарий не сохранить — тогда остаётся прошлое сохранение.
    /// Continue (GameManager.ContinueFromSave): этаж грузится, снимок применяется до Start объектов сцены —
    /// отыгранные сценарии (в том числе стартовые) не повторяются; через кадр Дэнни встаёт на место.
    /// New Game и конец игры (титры) стирают файл.
    /// </summary>
    public static class SaveSystem
    {
        const int Version = 2;
        const string FileName = "overworked_save.json";
        const float AutosaveInterval = 4f;

        // Одноразовые флажки компонентов этажа (поля и автосвойства). Сохраняются у всех компонентов Overworked.
        static readonly HashSet<string> StateFields = new HashSet<string>
        {
            "used", "locked", "doorLocked", "fired", "done", "won", "started", "onboarded", "introDone", "visited",
            "<HasPlayed>k__BackingField", "<SanityApplied>k__BackingField", "<HasHit>k__BackingField",
            "<Available>k__BackingField", "<IsOn>k__BackingField", "<Visited>k__BackingField",
        };

        [Serializable] class ActiveEntry { public string path; public bool active; }
        [Serializable] class FieldEntry { public string path; public string type; public string field; public bool value; }

        [Serializable]
        class FloorState
        {
            public string scene;
            public float x, y;
            public int facing = 1;
            public string room;
            public float sanity = -1f;
            public List<ActiveEntry> actives = new List<ActiveEntry>();
            public List<string> destroyed = new List<string>();
            public List<FieldEntry> fields = new List<FieldEntry>();
        }

        [Serializable]
        class SaveData
        {
            public int version;
            public string playerName;
            public int floor;
            public List<string> flags = new List<string>();
            public List<string> completedObjectives = new List<string>();
            public List<string> activeObjectives = new List<string>();
            public List<string> messages = new List<string>();
            public List<string> unreadMessages = new List<string>();
            public bool hasFloorState;
            public FloorState floorState = new FloorState();
        }

        static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>Есть ли на диске сохранение, которое можно загрузить (файл есть и версия та же).</summary>
        public static bool HasSave
        {
            get
            {
                try
                {
                    if (!File.Exists(FilePath)) return false;
                    var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                    return data != null && data.version == Version;
                }
                catch { return false; }
            }
        }

        // Поля-флажки по типу компонента: отражение один раз на тип, а не каждое автосохранение.
        static readonly Dictionary<Type, FieldInfo[]> fieldCache = new Dictionary<Type, FieldInfo[]>();

        static FieldInfo[] StateFieldsOf(Type t)
        {
            if (fieldCache.TryGetValue(t, out var cached)) return cached;
            var list = new List<FieldInfo>();
            if (t.Namespace == "Overworked")
                for (var type = t; type != null && type != typeof(MonoBehaviour); type = type.BaseType)
                    foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                        if (f.FieldType == typeof(bool) && StateFields.Contains(f.Name)) list.Add(f);
            return fieldCache[t] = list.ToArray();
        }

        // Игра в этой сессии прошла через главное меню (New Game / Continue). Этаж, запущенный прямо из редактора
        // (или прыжками F2–F5 без меню), ничего не пишет — тестовый прогон не затрёт настоящее сохранение.
        static bool menuSeen;

        // Исходная сцена этажа: какие объекты были включены сразу после загрузки (до Start) — с ней сравнивается снимок.
        static Scene baselineScene;
        static Dictionary<string, bool> baseline;

        // Снимок этажа из файла, который надо применить, когда этот этаж загрузится (Continue).
        static FloorState pending;

        static Runner runner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            menuSeen = false;   // Enter Play Mode без перезагрузки домена — статика не сбрасывается сама
            pending = null;
            baseline = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (runner == null)
            {
                var go = new GameObject("SaveSystem");
                UnityEngine.Object.DontDestroyOnLoad(go);
                runner = go.AddComponent<Runner>();
            }
        }

        // ------------------------------------------------------------ загрузка сцен

        // До Start сцены: объекты проснулись, но сценарии ещё ничего не запустили.
        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu") { menuSeen = true; return; }
            if (mode != LoadSceneMode.Single) return;
            int floor = FloorOf(scene.name);
            if (floor < 0) return;

            baselineScene = scene;
            baseline = new Dictionary<string, bool>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    baseline[PathOf(t)] = t.gameObject.activeSelf;

            if (pending != null && pending.scene == scene.name)
            {
                var state = pending;
                pending = null;
                ApplyFloorState(scene, state);
                runner.StartCoroutine(ApplyAfterStart(state));
                return;   // файл уже содержит этот снимок — не затираем его чекпоинтом «с начала»
            }
            pending = null;
            if (menuSeen && GameManager.Instance != null) Write(floor, null);
        }

        static int FloorOf(string sceneName)
        {
            if (sceneName == "WhiteRoom") return 0;
            if (sceneName.StartsWith("Floor_0") && int.TryParse(sceneName.Substring(7), out int f)) return f;
            return -1;
        }

        // Путь объекта в сцене с номерами среди соседей — однозначный, пока сцена та же.
        static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (var c = t; c != null; c = c.parent) parts.Add(c.name + "#" + c.GetSiblingIndex());
            parts.Reverse();
            return string.Join("/", parts);
        }

        // ------------------------------------------------------------ сохранение

        /// <summary>
        /// Идёт ли что-то, что нельзя оборвать или сохранить посередине: реплика, сценарий, галлюцинация.
        /// Тогда пауза не пускает в меню, а автосохранение ждёт.
        /// </summary>
        public static bool StoryBusy()
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsPlaying) return true;
            foreach (var s in UnityEngine.Object.FindObjectsByType<ScriptedSequence>(FindObjectsSortMode.None))
                if (s.IsPlaying) return true;
            foreach (var h in UnityEngine.Object.FindObjectsByType<HallucinationEvent>(FindObjectsSortMode.None))
                if (h.IsPlaying) return true;
            return false;
        }

        /// <summary>
        /// Сохранить сейчас со снимком этажа (автосохранение, выход в меню, закрытие игры). Пропускается, если
        /// мы не на этаже, Дэнни не свободен или что-то идёт — остаётся прошлое сохранение.
        /// </summary>
        public static void SaveNow()
        {
            if (!menuSeen || GameManager.Instance == null || baseline == null) return;
            var scene = SceneManager.GetActiveScene();
            if (scene != baselineScene || !scene.isLoaded) return;
            var player = PlayerController.Instance;
            if (player == null || player.State != PlayerState.Normal) return;
            if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsLoading) return;
            if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused) return;
            if (StoryBusy()) return;
            Write(FloorOf(scene.name), CaptureFloorState(scene, player));
        }

        static FloorState CaptureFloorState(Scene scene, PlayerController player)
        {
            var st = new FloorState { scene = scene.name };
            var p = player.transform.position;
            st.x = p.x; st.y = p.y;
            st.facing = player.Facing;
            st.room = Room.Current != null ? Room.Current.name : null;
            if (SanityManager.Instance != null) st.sanity = SanityManager.Instance.Current;

            var seen = new HashSet<string>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    string path = PathOf(t);
                    seen.Add(path);
                    if (baseline.TryGetValue(path, out bool was) && was != t.gameObject.activeSelf)
                        st.actives.Add(new ActiveEntry { path = path, active = t.gameObject.activeSelf });
                    foreach (var mb in t.GetComponents<MonoBehaviour>())
                        CaptureFields(mb, path, st.fields);
                }
            foreach (var path in baseline.Keys)
                if (!seen.Contains(path)) st.destroyed.Add(path);
            return st;
        }

        static void CaptureFields(MonoBehaviour mb, string path, List<FieldEntry> into)
        {
            if (mb == null) return;
            foreach (var f in StateFieldsOf(mb.GetType()))
                into.Add(new FieldEntry { path = path, type = mb.GetType().Name, field = f.Name, value = (bool)f.GetValue(mb) });
        }

        // Записать файл: состояние игры сейчас + снимок этажа (null — чекпоинт «этаж с начала»).
        static void Write(int floor, FloorState floorState)
        {
            var gm = GameManager.Instance;
            var data = new SaveData
            {
                version = Version,
                playerName = gm.State.PlayerName,
                floor = floor,
                flags = new List<string>(gm.State.Flags),
                hasFloorState = floorState != null,
                floorState = floorState ?? new FloorState(),
            };
            var om = ObjectiveManager.Instance;
            if (om != null)
            {
                data.completedObjectives.AddRange(om.CompletedIds);
                foreach (var o in om.Active) if (o != null) data.activeObjectives.Add(o.Id);
            }
            var mm = MessageManager.Instance;
            if (mm != null)
                foreach (var m in mm.History)
                {
                    if (m == null) continue;
                    data.messages.Add(m.name);
                    if (mm.IsUnread(m)) data.unreadMessages.Add(m.name);
                }
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(data));
                if (floorState == null) Debug.Log($"[Save] чекпоинт: этаж {floor} с начала");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] не удалось записать {FilePath}: {e.Message}");
            }
        }

        // ------------------------------------------------------------ загрузка

        /// <summary>
        /// Прочитать сохранение и выставить по нему состояние игры (имя, флаги, задания, телефон); снимок этажа
        /// применится, когда этаж загрузится. Возвращает этаж для загрузки или -1, если сохранения нет / не читается.
        /// </summary>
        public static int Load()
        {
            SaveData data;
            try
            {
                if (!HasSave) return -1;
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] не удалось прочитать {FilePath}: {e.Message}");
                return -1;
            }
            if (data == null || data.version != Version) return -1;

            var gm = GameManager.Instance;
            if (gm == null) return -1;
            if (!string.IsNullOrEmpty(data.playerName)) gm.State.PlayerName = data.playerName;
            gm.State.CurrentFloor = data.floor;
            gm.State.Flags = new HashSet<string>(data.flags);

            var reg = Resources.Load<SaveRegistry>(SaveRegistry.ResourcePath);
            if (reg == null) Debug.LogWarning("[Save] нет Resources/SaveRegistry — активные задания и телефон не восстановятся");

            if (ObjectiveManager.Instance != null)
            {
                var active = new List<ObjectiveData>();
                if (reg != null)
                    foreach (var id in data.activeObjectives)
                    {
                        var o = reg.Objective(id);
                        if (o != null) active.Add(o);
                        else Debug.LogWarning($"[Save] задание {id} не найдено в SaveRegistry");
                    }
                ObjectiveManager.Instance.Restore(data.completedObjectives, active);
            }

            if (MessageManager.Instance != null && reg != null)
            {
                var history = new List<MessageData>();
                var unread = new List<MessageData>();
                foreach (var n in data.messages) { var m = reg.Message(n); if (m != null) history.Add(m); }
                foreach (var n in data.unreadMessages) { var m = reg.Message(n); if (m != null) unread.Add(m); }
                MessageManager.Instance.Restore(history, unread);
            }

            pending = data.hasFloorState ? data.floorState : null;
            menuSeen = true;
            Debug.Log($"[Save] загружено: этаж {data.floor}, имя {gm.State.PlayerName}{(data.hasFloorState ? ", с места остановки" : ", с начала этажа")}");
            return data.floor;
        }

        // До Start: включённость объектов и флажки — чтобы стартовые сценарии увидели «уже отыграно».
        static void ApplyFloorState(Scene scene, FloorState st)
        {
            var byPath = new Dictionary<string, Transform>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    byPath[PathOf(t)] = t;

            foreach (var e in st.actives)
                if (byPath.TryGetValue(e.path, out var t)) t.gameObject.SetActive(e.active);
            foreach (var path in st.destroyed)
                if (byPath.TryGetValue(path, out var t)) t.gameObject.SetActive(false);

            int applied = 0;
            foreach (var e in st.fields)
            {
                if (!byPath.TryGetValue(e.path, out var t)) continue;
                foreach (var mb in t.GetComponents<MonoBehaviour>())
                {
                    if (mb == null || mb.GetType().Name != e.type) continue;
                    foreach (var f in StateFieldsOf(mb.GetType()))
                        if (f.Name == e.field) { f.SetValue(mb, e.value); applied++; break; }
                }
            }

            // Фонарик: флажки выставлены в обход Toggle — свет приводим к IsOn сами (без щелчка и OnToggled).
            var fc = FlashlightController.Instance;
            if (fc != null)
            {
                var light = fc.GetComponentInChildren<Light2D>(true);
                if (light != null) light.enabled = fc.IsOn;
            }
            Debug.Log($"[Save] снимок этажа: объектов {st.actives.Count + st.destroyed.Count}, флажков {applied}");
        }

        // После Start (FloorSettings выставил рассудок этажа, камеры настроены): Дэнни на место, комната, рассудок.
        static IEnumerator ApplyAfterStart(FloorState st)
        {
            yield return null;
            var player = PlayerController.Instance;
            if (player != null)
            {
                player.Teleport(new Vector3(st.x, st.y, player.transform.position.z));
                player.RestoreFacing(st.facing);
            }
            if (!string.IsNullOrEmpty(st.room))
                foreach (var room in UnityEngine.Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
                    if (room.name == st.room) { room.RestoreInside(); break; }
            if (st.sanity >= 0f && SanityManager.Instance != null) SanityManager.Instance.SetSanityInstant(st.sanity);
        }

        /// <summary>Стереть сохранение (New Game, конец игры).</summary>
        public static void Delete()
        {
            pending = null;
            try
            {
                if (HasSave) File.Delete(FilePath);
                Debug.Log("[Save] сохранение стёрто");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] не удалось стереть {FilePath}: {e.Message}");
            }
        }

        // Автосохранение и сохранение при закрытии игры.
        class Runner : MonoBehaviour
        {
            float next;

            void Update()
            {
                if (Time.unscaledTime < next) return;
                next = Time.unscaledTime + AutosaveInterval;
                SaveNow();
            }

            void OnApplicationQuit() => SaveNow();
        }
    }
}
