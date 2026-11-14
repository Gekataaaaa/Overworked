using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using HB = Overworked.EditorTools.HallucinationPrefabBuilder;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Песочница P4 «вся игра»: копии MainMenu и этажей (Scenes/Sandbox/Sandbox_P4_MainMenu, Sandbox_P4_Floor_03/02/01),
    /// в которые P4 ставит галлюцинации, силуэты и финальный скример, не трогая чужие сцены.
    /// В копиях этажей: двери в комнаты работают (DoorInteractable), фонарик на 2 и 1 этажах,
    /// зоны галлюцинаций по нарративу, P4SandboxRoute (переходы ведут по копиям) и отладка HorrorDebugKeys.
    /// Запускать игру — из Sandbox_P4_MainMenu (или сразу с копии этажа).
    /// Запуск: меню Overworked → P4 → Build Game Sandbox,
    /// или batchmode -executeMethod Overworked.EditorTools.P4GameSandboxBuilder.Run.
    /// Копии каждый раз пересоздаются из оригиналов (правки руками в копиях пропадут).
    /// </summary>
    public static class P4GameSandboxBuilder
    {
        const string Scenes = "Assets/_Project/Scenes/";
        const float FloorY = -4.2f;
        const float PlayerY = FloorY + 0.9f;

        [MenuItem("Overworked/P4/Build Game Sandbox")]
        public static void Run()
        {
            var h = HB.EnsurePrefabs();

            var menu = Copy("MainMenu");
            var scene = EditorSceneManager.OpenScene(menu, OpenSceneMode.Single);
            new GameObject("P4_SandboxRoute").AddComponent<P4SandboxRoute>();
            Save(scene);

            BuildFloor03(Copy("Floor_03"), h);
            BuildFloor02(Copy("Floor_02"), h);
            BuildFloor01(Copy("Floor_01"), h);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] P4: песочница «вся игра» готова — старт со Scenes/Sandbox/Sandbox_P4_MainMenu");
        }

        static string Copy(string name)
        {
            string from = $"{Scenes}{name}.unity";
            string to = P4SandboxRoute.CopyOf(from);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(to) != null) AssetDatabase.DeleteAsset(to);
            if (!AssetDatabase.CopyAsset(from, to)) Debug.LogError($"[Setup] не скопировать {from} → {to}");
            return to;
        }

        // ------------------------------------------------------------ этажи

        // Kitchen → турникет у лестницы → после отказа турникета Галлюцинация 1 → лестница.
        static void BuildFloor03(string path, GameObject[] h)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Common(flashlight: false);
            Doors(scene, "Door_Kitchen", "Kitchen", "Enter the kitchen");
            var root = Root(scene, "Hallucinations");
            var h1 = Place(h[0], root);
            Zone(root, "H1_Zone_Turnstile", new Vector2(17f, PlayerY), h1);
            Save(scene);
        }

        // Коридор: H2 (силуэт) посередине; Room_201: H3 (дверь, свет, «DD»).
        static void BuildFloor02(string path, GameObject[] h)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Common(flashlight: true);
            Doors(scene, "Door_Room_201", "Room_201", "Enter room 201");
            var root = Root(scene, "Hallucinations");
            var h2 = Place(h[1], root);
            var h3 = Place(h[2], root);
            Zone(root, "H2_Zone_Corridor", new Vector2(-6f, PlayerY), h2);
            Zone(root, "H3_Zone_Room201", new Vector2(2f, -20f + PlayerY), h3, width: 2f);
            Save(scene);
        }

        // Room_101: H4 сразу при входе; Room_102 (комната отдыха): победа в Tiny UFO → выход в коридор
        // → финальный скример (FinalScreamerStarter) → FinalCollapse → WhiteRoom.
        static void BuildFloor01(string path, GameObject[] h)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Common(flashlight: true);
            var entry101 = Doors(scene, "Door_Room_101", "Room_101", "Enter room 101");
            Doors(scene, "Door_Room_102", "Room_102", "Enter room 102");
            var root = Root(scene, "Hallucinations");
            var h4 = Place(h[3], root);
            // Финальный скример уже есть в оригинале (AddToFloor01) — второй не ставим.
            bool hasFinal = Object.FindAnyObjectByType<FinalScreamerStarter>(FindObjectsInactive.Include) != null;
            // «После затемнения Дэнни внутри» — зона на точке входа: срабатывает сразу после телепорта.
            Zone(root, "H4_Zone_Room101", entry101 != null ? (Vector2)entry101.position : new Vector2(-5.5f, -20f + PlayerY), h4, width: 3f);
            if (!hasFinal) AddFinalScreamer(root, Place(h[4], root));
            Save(scene);
        }

        /// <summary>
        /// Финальный скример в настоящий Floor_01 (общая сцена): H5_FinalScreamer в Hallucinations
        /// и FinalScreamerStarter (победа в Tiny UFO + выход из Room_102). Повторный запуск заменяет прежние.
        /// </summary>
        [MenuItem("Overworked/P4/Add Final Screamer to Floor_01")]
        public static void AddToFloor01()
        {
            var h = HB.EnsurePrefabs();
            var scene = EditorSceneManager.OpenScene(Scenes + "Floor_01.unity", OpenSceneMode.Single);
            var root = Root(scene, "Hallucinations");
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                var child = root.transform.GetChild(i).gameObject;
                if (child.name.StartsWith(HB.H5) || child.name == StarterName) Object.DestroyImmediate(child);
            }
            AddFinalScreamer(root, Place(h[4], root));
            Save(scene);
            Debug.Log("[Setup] P4: финальный скример добавлен в Floor_01");
        }

        const string StarterName = "H5_Starter_AfterTinyUfo";

        /// <summary>
        /// H1–H4 в настоящие этажи (папка Hallucinations): Floor_03 — H1, Floor_02 — H2, H3, Floor_01 — H4.
        /// Ни к чему не привязаны (ни зон, ни шагов сценария) — запуск подключим позже.
        /// Уже стоящие не дублируются. Финальный скример — AddToFloor01.
        /// </summary>
        [MenuItem("Overworked/P4/Add Hallucinations to Floors")]
        public static void AddHallucinationsToFloors()
        {
            var h = HB.EnsurePrefabs();
            void Add(string floor, params GameObject[] prefabs)
            {
                var scene = EditorSceneManager.OpenScene($"{Scenes}{floor}.unity", OpenSceneMode.Single);
                var root = Root(scene, "Hallucinations");
                foreach (var p in prefabs) Place(p, root);
                Save(scene);
            }
            Add("Floor_03", h[0]);
            Add("Floor_02", h[1], h[2]);
            Add("Floor_01", h[3]);
            Debug.Log("[Setup] P4: H1–H4 стоят в папках Hallucinations этажей (без привязки к запуску)");
        }

        /// <summary>FinalScreamerStarter: комната Room_102, её аркадный автомат и H5.</summary>
        static void AddFinalScreamer(GameObject root, HallucinationEvent h5)
        {
            // Имя Room_102 носят и другие объекты (подпись, дверь) — ищем именно компонент Room.
            Room roomComp = null;
            foreach (var r in Object.FindObjectsByType<Room>(FindObjectsInactive.Include))
                if (r.name == "Room_102") roomComp = r;
            var arcade = Object.FindAnyObjectByType<ArcadeInteractable>(FindObjectsInactive.Include);
            if (roomComp == null || arcade == null)
                Debug.LogError("[Setup] Floor_01: нет Room на Room_102 или аркадного автомата — финальный скример не подключён");
            var starter = new GameObject(StarterName).AddComponent<FinalScreamerStarter>();
            starter.transform.SetParent(root.transform, false);
            HB.Set(starter, "room", roomComp);
            HB.Set(starter, "arcade", arcade);
            HB.Set(starter, "finalScreamer", h5);
        }

        /// <summary>Отладка, маршрут по копиям и (на 2 и 1 этажах) фонарик.</summary>
        static void Common(bool flashlight)
        {
            var go = new GameObject("P4_Sandbox");
            go.AddComponent<P4SandboxRoute>();
            go.AddComponent<HorrorDebugKeys>();
            if (flashlight && Object.FindAnyObjectByType<FlashlightController>() == null)
                new GameObject("Flashlight").AddComponent<FlashlightController>();
        }

        /// <summary>
        /// Дверь коридора → EntryPoint комнаты и ExitDoor комнаты → точка у двери в коридоре, со сменой камер.
        /// Возвращает EntryPoint комнаты.
        /// </summary>
        static Transform Doors(Scene scene, string corridorDoor, string room, string prompt)
        {
            var door = Find(scene, corridorDoor);
            var roomT = Find(scene, room);
            var corridorCam = Find(scene, "CM Corridor");
            var roomCam = Find(scene, $"CM {room}");
            if (door == null || roomT == null || corridorCam == null || roomCam == null)
            {
                Debug.LogError($"[Setup] {scene.name}: нет {corridorDoor}/{room}/камер — дверь не подключена");
                return null;
            }
            var entry = roomT.Find("EntryPoint");
            // Двери уже подключены в оригинале (P2, Wire Rooms) — не трогаем.
            if (door.GetComponent<DoorInteractable>() != null) return entry;
            var exitDoor = roomT.Find("ExitDoor/Door");

            var back = new GameObject($"{corridorDoor}_Out").transform;
            back.SetParent(door.parent, false);
            back.position = new Vector3(door.position.x, PlayerY, 0f);

            AddDoor(door.gameObject, prompt, entry, roomCam.gameObject, corridorCam.gameObject);
            if (exitDoor != null) AddDoor(exitDoor.gameObject, "Exit", back, corridorCam.gameObject, roomCam.gameObject);
            return entry;
        }

        static void AddDoor(GameObject go, string prompt, Transform target, GameObject camOn, GameObject camOff)
        {
            // Дверь — растянутый спрайт (1.4 × 2.5): коллайдер 1×1 в локальных единицах закрывает её целиком.
            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one;
            var d = go.GetComponent<DoorInteractable>();
            if (d == null) d = go.AddComponent<DoorInteractable>();
            HB.Set(d, "prompt", prompt);
            HB.Set(d, "target", target);
            HB.Set(d, "cameraOn", camOn);
            HB.Set(d, "cameraOff", camOff);
        }

        /// <summary>Экземпляр префаба в папке Hallucinations; если такой уже есть (стоит в оригинале этажа) — он.</summary>
        static HallucinationEvent Place(GameObject prefab, GameObject root)
        {
            foreach (var e in root.GetComponentsInChildren<HallucinationEvent>(true))
                if (e.name.StartsWith(prefab.name)) return e;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.scene);
            go.transform.SetParent(root.transform, false);
            return go.GetComponent<HallucinationEvent>();
        }

        static HallucinationTrigger Zone(GameObject root, string name, Vector2 position, HallucinationEvent h, float width = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.position = position;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(width, 5f);
            var t = go.AddComponent<HallucinationTrigger>();
            HB.Set(t, "hallucination", h);
            return t;
        }

        static GameObject Root(Scene scene, string name)
        {
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == name) return go;
            return new GameObject(name);
        }

        static Transform Find(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t;
            return null;
        }

        static void Save(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
