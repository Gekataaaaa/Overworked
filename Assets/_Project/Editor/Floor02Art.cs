using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Арт Floor_02 по папке художника Floor02_Art (README + placement.json), собранный так же, как Floor_03:
    /// один фон коридора 7680×1736 с PPU 153.6 (50 × 11.3 юнита, стены на ±25.5), отдельные спрайты дверей и
    /// предметов (order -7), комната 201 — фон 1920×1080 с PPU 100 в Rooms/Room_201.
    /// Освещения пока нет: группы Lights пустые (лампы и свечение экрана добавим позже), затемнения тоже —
    /// Global Light 2D = 1, как на Floor_03.
    /// Маршрут, как на Floor_03: Дэнни появляется слева (у Door_Stairwell), идёт вправо, лестница вниз —
    /// лестничный проём справа (Stairs_Stairwell, из превью художника).
    /// Повторный запуск пересобирает арт с нуля; галлюцинации, фонарик и сюжет не трогает.
    /// Меню: Overworked → P4 → Art → Build Floor_02 Art.
    /// </summary>
    [InitializeOnLoad]
    public static class Floor02Art
    {
        const string ScenePath = "Assets/_Project/Scenes/Floor_02.unity";
        const string ArtDir = "Assets/_Project/Art/Environment/Floor02/";
        const string RequestFile = "Temp/p4_floor02_art.request";
        const string InspectMugPath = "Assets/_Project/Data/Inspectables/F2_Mug.asset";
        const string InspectLampPath = "Assets/_Project/Data/Inspectables/F2_BrokenLamp.asset";

        // Коридор: фон художника (7680×1080) вставлен в кадр Floor_03 (7680×1736) со сдвигом 522 px сверху.
        const float CorridorPpu = 153.6f;
        const float CorridorHalfH = 1736f / 2f;
        const float BandTop = 522f;
        // Комната: 1920×1080 при PPU 100, как кухня Floor_03.
        const float RoomPpu = 100f;
        const float FeetY = -3.3f;       // высота ReturnPoint/Player на Floor_03
        const float RoomEntryY = -3.36f; // EntryPoint кухни Floor_03
        const float PlayerStartX = -21.5f; // старт слева, у Door_Stairwell (как на Floor_03, x ≈ -22)

        // Спрайты только комнаты — PPU 100, остальные — PPU 153.6.
        static readonly string[] RoomSprites =
            { "Room201_Background", "Room201_ExitDoor", "Archive_Desk", "Archive_Computer", "Archive_Computer_Glow",
              "Archive_Drawer_Open", "Shift_Folder", "Room_Light_Tube_On", "Room_Light_Cone" };

        // Светильники, нарисованные на фоне коридора (центры трубок по X, пиксели Floor02_Corridor.png):
        // найдены по тёмным полосам у потолка. 4000 — Lamp_03 над диваном из README. Разбитая лампа
        // над осколками (x ≈ 3200) не горит.
        static readonly float[] CorridorFixtureX = { 481f, 1340f, 2160f, 4000f, 4841f, 5697f };

        // Этаж 2 почти без света (паспорт, раздел 7): горят 3 из 6 светильников (над диваном — Lamp_03 из README),
        // вдвое тусклее Floor_03,
        // и одна из горящих ламп неисправна — мигает. Остальные остаются тёмными, как нарисованы.
        static readonly bool[] CorridorLampOn = { true, false, true, true, false, false };
        const int FaultyLamp = 2;
        // Комната 201 маленькая — свет ещё вдвое слабее коридора, иначе заливает всю комнату.
        static readonly FloorLamps.Style RoomLight = new FloorLamps.Style
        {
            Color = FloorLamps.Office.Color, Intensity = FloorLamps.Office.Intensity * 0.25f,
            DarkLevel = FloorLamps.Office.DarkLevel, ConeDim = 0.75f,
        };
        // Тон предметов, которые ярче фона (как SpriteRenderer Дэнни в префабе Player).
        static readonly Color PropTint = new Color(0.62f, 0.64f, 0.6f, 1f);

        static readonly FloorLamps.Style DimLight = new FloorLamps.Style
        {
            Color = FloorLamps.Office.Color, Intensity = FloorLamps.Office.Intensity * 0.5f,
            DarkLevel = FloorLamps.Office.DarkLevel, ConeDim = 0.5f,
        };

        // Объекты, которые создаёт этот сборщик (пересоздаются при каждом запуске; остальное в группах — чужое).
        static readonly string[] CorridorProps =
            { "Stairs_Up", "Stairs_Stairwell", "Poster_67", "Couch", "Chair", "Plant_Augustin", "Piggy_Bank", "Statuette",
              "Moldy_Mug", "Lamp_Shards" };
        static readonly string[] RoomProps =
            { "Archive_Desk", "Archive_Computer", "Archive_Computer_Glow", "Archive_Drawer_Open", "Shift_Folder" };

        static Floor02Art()
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
            string previous = SceneManager.GetActiveScene().path;
            Build();
            // Песочница P4 копирует Floor_02 — пересобираем, чтобы в ней был этот же этаж.
            P4GameSandboxBuilder.Run();
            if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            RunRequest();
        }

        [MenuItem("Overworked/P4/Art/Build Floor_02 Art")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;

            ImportSprites();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var corridor = Find(scene, "Environment/Corridor");
            var room = Find(scene, "Environment/Rooms/Room_201");
            if (corridor == null || room == null)
            {
                Debug.LogError("[Setup] Floor_02: нет Environment/Corridor или Environment/Rooms/Room_201");
                return;
            }

            BuildCorridor(corridor.transform);
            BuildRoom(room.transform);
            PlaceStaircaseAndPlayer(scene);
            FollowArt(scene);
            NoDarkening(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(previous) && previous != ScenePath)
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log("[Setup] Floor_02: арт коридора и комнаты 201 собран");
        }

        // ------------------------------------------------------------ импорт

        static void ImportSprites()
        {
            // Лампа и конус нужны в двух масштабах: для комнаты — копии с префиксом Room_ (как в Floor01Art).
            foreach (var n in new[] { "Light_Tube_On", "Light_Cone" })
                if (!File.Exists(ArtDir + "Room_" + n + ".png") && File.Exists(ArtDir + n + ".png"))
                    File.Copy(ArtDir + n + ".png", ArtDir + "Room_" + n + ".png");
            AssetDatabase.Refresh();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                string name = Path.GetFileNameWithoutExtension(path);
                float ppu = System.Array.IndexOf(RoomSprites, name) >= 0 ? RoomPpu : CorridorPpu;
                bool wide = name == "Floor02_Corridor";
                if (ti.textureType == TextureImporterType.Sprite && ti.spriteImportMode == SpriteImportMode.Single
                    && Mathf.Approximately(ti.spritePixelsPerUnit, ppu) && ti.textureCompression == TextureImporterCompression.Uncompressed
                    && (!wide || ti.maxTextureSize == 8192))
                    continue;
                // Как в README художника: Sprite, Bilinear, без сжатия; пивот по центру, как у арта Floor_03.
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = ppu;
                ti.filterMode = FilterMode.Bilinear;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.maxTextureSize = wide ? 8192 : 2048;
                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                ti.SetTextureSettings(settings);
                ti.SaveAndReimport();
            }
        }

        static Sprite Spr(string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + name + ".png");
            if (s == null) Debug.LogError($"[Setup] Floor_02: нет спрайта {ArtDir}{name}.png");
            return s;
        }

        // ------------------------------------------------------------ коридор

        // Пиксели placement.json (x, y сверху, w, h в полосе 7680×1080) → центр в мире коридора.
        static Vector3 C(float x, float y, float w, float h) =>
            new Vector3((x + w / 2f - 3840f) / CorridorPpu, (CorridorHalfH - BandTop - (y + h / 2f)) / CorridorPpu, 0f);

        // Прямоугольник вырезки Stairs_Stairwell.png в полосе фона.
        static Vector3 StairwellPos => C(6225, 235, 935, 775);

        static void BuildCorridor(Transform corridor)
        {
            // Фон: заглушки каркаса прячем, вместо них — один спрайт, как BackgroundArt у Floor_03.
            var bg = Child(corridor, "Background");
            foreach (Transform t in bg) if (t.name != "BackgroundArt") t.gameObject.SetActive(false);
            Place(bg, "BackgroundArt", "Floor02_Corridor", Vector3.zero, -9);

            // Стены и рамка камеры — по ширине фона (50 юнитов), как на Floor_03.
            var walls = Child(corridor, "Walls");
            SetWall(walls.Find("WallLeft"), -25.5f);
            SetWall(walls.Find("WallRight"), 25.5f);
            SetBounds(corridor.Find("CameraBounds"), 25f, 5.65f);

            // Двери: серые заглушки каркаса (Door_<число>) убираем, Door_Room_201 (с DoorInteractable) переодеваем.
            // Остальное в Doors (чужие объекты) не трогаем.
            var doors = Child(corridor, "Doors");
            for (int i = doors.childCount - 1; i >= 0; i--)
            {
                var d = doors.GetChild(i);
                if (d.name == "Label") d.gameObject.SetActive(false);
                else if (Regex.IsMatch(d.name, @"^Door_-?\d+$") || d.name == "Door_Stairwell") Object.DestroyImmediate(d.gameObject);
            }
            var door201 = doors.Find("Door_Room_201");
            var door201Pos = C(1969, 515, 223, 485);
            if (door201 != null)
            {
                door201.localPosition = door201Pos;
                door201.localScale = Vector3.one;
                var sr = door201.GetComponent<SpriteRenderer>();
                sr.sprite = Spr("Door_201");
                sr.color = Color.white;
                sr.sortingOrder = -7;
                if (door201.TryGetComponent(out BoxCollider2D col)) { col.size = sr.sprite.bounds.size; col.offset = Vector2.zero; }
            }
            else Debug.LogError("[Setup] Floor_02: нет Doors/Door_Room_201 — сначала Overworked → P2 → All Floors → Wire Rooms On All Floors");
            // Door_203 подходит под шаблон декоративных дверей RoomWiring (Door_<число>) — E на ней не появится.
            Place(doors, "Door_203", "Door_203", C(5239, 515, 223, 485), -7);
            Place(doors, "Door_Stairwell", "Door_Stairwell", C(49, 515, 223, 485), -7);

            var back = corridor.Find("ReturnPoints/Room_201");
            if (back != null) back.localPosition = new Vector3(door201Pos.x, FeetY, 0f);

            // Предметы (красные пометки скетча и задачи ART_TASKS).
            var props = Child(corridor, "Props");
            Remove(props, CorridorProps);
            // Лестничный проём вырезан из Preview_Floor02_Corridor художника (Stairs_Up из атласа не используем).
            Place(props, "Stairs_Stairwell", "Stairs_Stairwell", StairwellPos, -7);
            Place(props, "Poster_67", "Poster_67", C(7168, 420, 295, 350), -7);
            Place(props, "Couch", "Couch", C(3530, 755, 720, 245), -7);
            Place(props, "Chair", "Chair", C(3304, 760, 132, 240), -7);
            Place(props, "Plant_Augustin", "Plant_Augustin", C(2413, 470, 354, 530), -7);
            Place(props, "Piggy_Bank", "Piggy_Bank", C(660, 728, 101, 78), -7);
            Place(props, "Statuette", "Statuette", C(1194, 650, 64, 156), -7);
            var mug = Place(props, "Moldy_Mug", "Moldy_Mug", C(1802, 714, 49, 52), -7);
            var shards = Place(props, "Lamp_Shards", "Lamp_Shards", C(3007, 968, 206, 32), -7);
            // Описания на подходе — тексты уже есть в Data/Inspectables (как кружка/кулер на Floor_03).
            Inspect(mug, InspectMugPath);
            Inspect(shards, InspectLampPath);

            // Трубка 302 px по центру светильника (у Lamp_03 из README левый край 3850 → центр 4001),
            // конус — на 359 px левее, y 272 (как в README).
            var fixtures = new System.Collections.Generic.List<FloorLamps.Fixture>();
            for (int i = 0; i < CorridorFixtureX.Length; i++)
            {
                if (!CorridorLampOn[i]) continue;
                float cx = CorridorFixtureX[i];
                fixtures.Add(new FloorLamps.Fixture(C(cx - 151, 241, 302, 62), C(cx - 151 - 359, 272, 1020, 768), i == FaultyLamp));
            }
            FloorLamps.Build(Child(corridor, "Lights"), Spr("Light_Tube_On"), Spr("Light_Cone"), fixtures,
                DimLight, Child(corridor, "Background"));
        }

        // ------------------------------------------------------------ комната 201

        // Пиксели комнаты 1920×1080 → локальный центр в Room_201.
        static Vector3 R(float x, float y, float w, float h) =>
            new Vector3((x + w / 2f - 960f) / RoomPpu, (540f - (y + h / 2f)) / RoomPpu, 0f);

        static void BuildRoom(Transform room)
        {
            var bg = Child(room, "Background");
            foreach (Transform t in bg) if (t.name != "BackgroundArt") t.gameObject.SetActive(false);
            Place(bg, "BackgroundArt", "Room201_Background", Vector3.zero, -10);

            // Стены оставляем коллайдерами, без серых заглушек; рамка камеры — во весь фон, как у кухни.
            var walls = Child(room, "Walls");
            foreach (Transform w in walls) if (w.TryGetComponent(out SpriteRenderer wsr)) wsr.enabled = false;
            SetBounds(room.Find("CameraBounds"), 9.6f, 5.4f);

            // Выход — дверь слева, как Kitchen_ExitDoor; Дэнни появляется перед ней.
            var exitPos = R(99, 515, 223, 485);
            var exit = room.Find("ExitDoor/Door");
            if (exit != null)
            {
                exit.localPosition = exitPos;
                exit.localScale = Vector3.one;
                var sr = exit.GetComponent<SpriteRenderer>();
                sr.sprite = Spr("Room201_ExitDoor");
                sr.color = Color.white;
                sr.sortingOrder = -8;
                if (exit.TryGetComponent(out BoxCollider2D col)) { col.size = sr.sprite.bounds.size; col.offset = Vector2.zero; }
            }
            var label = room.Find("ExitDoor/Label");
            if (label != null) label.gameObject.SetActive(false);
            var entry = room.Find("EntryPoint");
            if (entry != null) entry.localPosition = new Vector3(exitPos.x, RoomEntryY, 0f);

            var props = Child(room, "Props");
            Remove(props, RoomProps);
            Place(props, "Archive_Desk", "Archive_Desk", R(960, 821, 600, 179), -8);
            // Стол и компьютер — под конусом света лампы (order -6), иначе компьютер поверх дымки конуса ярче стола.
            var computerArt = Place(props, "Archive_Computer", "Archive_Computer", R(1029, 615, 222, 210), -7);
            // Бежевый корпус выбивался из тёмной комнаты — тот же приглушённый тон, что у спрайта Дэнни.
            computerArt.GetComponent<SpriteRenderer>().color = PropTint;
            // Открытый ящик: пустой спрайт на месте тумбы, DrawerInteractable P2 подставит openSprite по E.
            var drawerOpen = Place(props, "Archive_Drawer_Open", "Archive_Drawer_Open", R(1402, 847, 168, 78), -7);
            var drawerSr = drawerOpen.GetComponent<SpriteRenderer>();
            var openSprite = drawerSr.sprite;
            drawerSr.sprite = null;
            Place(props, "Shift_Folder", "Shift_Folder", R(1427, 830, 118, 33), -5).SetActive(false);

            // Логика комнаты от P2 (Floor02Room201): серые заглушки прячем — шкафы, стол и монитор уже на арте,
            // а сами зоны E переносим на арт: ящик — на тумбу стола, компьютер — на монитор.
            var cabinets = props.Find("FilingCabinets");
            if (cabinets != null) cabinets.gameObject.SetActive(false);
            var desk = props.Find("Desk");
            if (desk != null)
            {
                desk.localPosition = new Vector3(R(1402, 847, 168, 78).x, desk.localPosition.y, 0f);
                HidePlaceholders(desk);
                if (desk.TryGetComponent(out DrawerInteractable drawer))
                    RoomWiring.Set(drawer, ("drawerSprite", drawerSr), ("openSprite", openSprite));
            }
            var computer = props.Find("Computer");
            if (computer != null)
            {
                computer.localPosition = new Vector3(R(1029, 615, 222, 210).x, computer.localPosition.y, 0f);
                HidePlaceholders(computer);
            }

            // Лампа под светильником на потолке фона (центр ≈ 960, 262). Без BackgroundDimmer — он затемняет
            // всю сцену и спорил бы с коридором. Свечения экрана (Archive_Computer_Glow) пока нет.
            FloorLamps.Build(Child(room, "Lights"), Spr("Room_Light_Tube_On"), Spr("Room_Light_Cone"),
                new[] { new FloorLamps.Fixture(R(809, 231, 302, 62), R(450, 262, 1020, 768)) }, RoomLight, null);
        }

        // ------------------------------------------------------------ лестница и старт

        static void PlaceStaircaseAndPlayer(Scene scene)
        {
            // Лестница вниз на Floor_01 — лестничный проём справа (Stairs_Stairwell), как Stairs_Down на Floor_03;
            // заглушки префаба прячем.
            var stairs = Find(scene, "Staircase/Staircase") ?? Find(scene, "Staircase");
            if (stairs != null && stairs.name == "Staircase" && stairs.GetComponent<Collider2D>() != null)
            {
                var p = stairs.transform.position;
                stairs.transform.position = new Vector3(StairwellPos.x, p.y, p.z);
                foreach (Transform t in stairs.transform)
                    if (t.GetComponent<SpriteRenderer>() != null) t.gameObject.SetActive(false);
                foreach (var mb in stairs.GetComponents<MonoBehaviour>())
                {
                    var so = new SerializedObject(mb);
                    var prompt = so.FindProperty("prompt");
                    if (prompt == null) continue;
                    prompt.stringValue = "Go down";
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            else Debug.LogWarning("[Setup] Floor_02: Staircase не найден — лестницу вниз поставьте вручную");

            // Старт — в левой части коридора, у двери Door_Stairwell, как старт Floor_03 слева (x ≈ -22).
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                player.transform.position = new Vector3(PlayerStartX, FeetY, 0f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
            }
            else Debug.LogWarning("[Setup] Floor_02: Player не найден — старт не сдвинут");
            if (stairs != null)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(stairs.transform);
                foreach (Transform t in stairs.transform) PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
            }
        }

        // Логика P2/P3 в коридоре ставилась под серый каркас (дверь 201 на x = 4, лестница на 19.5, старт -19).
        // Переносим её по тем же правилам их сборщиков, но относительно арта.
        static void FollowArt(Scene scene)
        {
            // Замок лестницы (Floor02StairsLock) — поверх лестницы.
            var notice = Find(scene, "Story/StairsLockedNotice");
            var stairs = Find(scene, "Staircase/Staircase");
            if (notice != null && stairs != null) notice.transform.position = stairs.transform.position;

            // Запрет возврата (P3StoryBuilder) — на 2.5 левее двери 201.
            var noReturn = Find(scene, "Story/P3Chain/2.2 NoReturn");
            var door201 = Find(scene, "Environment/Corridor/Doors/Door_Room_201");
            if (noReturn != null && door201 != null)
            {
                var p = noReturn.transform.position;
                noReturn.transform.position = new Vector3(door201.transform.position.x - 2.5f, p.y, p.z);
            }

            // Фонарик на полу (Floor02Flashlight) — в 2.5 правее старта.
            var pickup = Find(scene, "Interactables/FlashlightPickup");
            if (pickup != null)
            {
                var p = pickup.transform.position;
                pickup.transform.position = new Vector3(PlayerStartX + 2.5f, p.y, p.z);
            }
        }

        // Затемнения пока нет: общий свет — 1, как на Floor_03. ChainBuilder (Floor 02) снова ставит 0.08 —
        // после него запустите этот сборщик ещё раз.
        static void NoDarkening(Scene scene)
        {
            var global = Find(scene, "Global Light 2D");
            var light = global != null ? global.GetComponent<UnityEngine.Rendering.Universal.Light2D>() : null;
            if (light != null) light.intensity = 1f;
            else Debug.LogWarning("[Setup] Floor_02: Global Light 2D не найден — общий свет не изменён");
        }

        // ------------------------------------------------------------ помощники

        static GameObject Place(Transform parent, string name, string sprite, Vector3 pos, int order)
        {
            var t = parent.Find(name);
            var go = t != null ? t.gameObject : new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one;
            go.SetActive(true);
            if (!go.TryGetComponent(out SpriteRenderer sr)) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Spr(sprite);
            sr.color = Color.white;
            sr.sortingOrder = order;
            return go;
        }

        static void Inspect(GameObject go, string dialoguePath)
        {
            var data = AssetDatabase.LoadAssetAtPath<DialogueData>(dialoguePath);
            if (data == null) return;
            // Зона подхода — от пола до роста Дэнни, независимо от высоты предмета.
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.4f, 3f);
            col.offset = new Vector2(0f, FeetY - go.transform.position.y);
            RoomWiring.Set(go.AddComponent<Inspectable>(), ("dialogue", data));
        }

        static void SetWall(Transform wall, float x)
        {
            if (wall == null) return;
            wall.localPosition = new Vector3(x, wall.localPosition.y, wall.localPosition.z);
            if (wall.TryGetComponent(out SpriteRenderer sr)) sr.enabled = false;
        }

        static void SetBounds(Transform bounds, float halfW, float halfH)
        {
            if (bounds == null || !bounds.TryGetComponent(out PolygonCollider2D poly)) return;
            poly.SetPath(0, new[]
            {
                new Vector2(-halfW, -halfH), new Vector2(halfW, -halfH),
                new Vector2(halfW, halfH), new Vector2(-halfW, halfH),
            });
        }

        static Transform Child(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t;
            t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static void Remove(Transform parent, string[] names)
        {
            foreach (var name in names)
            {
                var t = parent.Find(name);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }
        }

        // Серые прямоугольники-заглушки P2 (Placeholder_Square) внутри объекта логики.
        static void HidePlaceholders(Transform t)
        {
            foreach (var sr in t.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.transform != t && sr.sprite != null && sr.sprite.name.StartsWith("Placeholder"))
                    sr.gameObject.SetActive(false);
        }

        static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.DestroyImmediate(t.GetChild(i).gameObject);
        }

        static GameObject Find(Scene scene, string path)
        {
            var parts = path.Split('/');
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != parts[0]) continue;
                if (parts.Length == 1) return root;
                var t = root.transform.Find(string.Join("/", parts, 1, parts.Length - 1));
                if (t != null) return t.gameObject;
            }
            return null;
        }
    }
}
