using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Арт Floor_01 по папке художника Floor01_Art, собранный так же, как Floor_02 (Floor02Art):
    /// фон коридора 7680×1736 с PPU 153.6 (50 × 11.3 юнита, стены на ±25.5), отдельные спрайты дверей и
    /// предметов (order -7), комнаты 101 и 102 — фоны 1920×1080 с PPU 100. Координаты — пиксели превью
    /// художника (Preview_Floor01_*), найдены сопоставлением спрайтов атласа с превью.
    /// Варианты коридора (CorridorVariants): V1 — старт; после Room_101 без ключа — V2 (галлюцинации, без фигуры),
    /// с ключом — V3 (появляется дверь CHILL ZONE 102). Пока двери 102 нет, задание «Find the key in room 101»
    /// выдаёт главный выход (раньше — запертая дверь 102).
    /// Освещения и затемнения пока нет: группы Lights пустые, свечения аркады нет, общий свет — 1.
    /// Повторный запуск пересобирает арт с нуля; галлюцинации H4/H5, фонарик и сюжет не трогает.
    /// Меню: Overworked → P4 → Art → Build Floor_01 Art.
    /// </summary>
    [InitializeOnLoad]
    public static class Floor01Art
    {
        const string ScenePath = "Assets/_Project/Scenes/Floor_01.unity";
        const string ArtDir = "Assets/_Project/Art/Environment/Floor01/";
        const string RequestFile = "Temp/p4_floor01_art.request";
        const string InspectGlassPath = "Assets/_Project/Data/Inspectables/F1_DarkGlass.asset";
        const string InspectFormsPath = "Assets/_Project/Data/Inspectables/F1_Forms.asset";
        const string PersonGonePath = "Assets/_Project/Data/Dialogues/F1_PersonGone.asset";
        const string FindKeyPath = "Assets/_Project/Data/Objectives/F1_FindKey101.asset";

        // Коридор: фон художника (7680×1080) вставлен в кадр 7680×1736 со сдвигом 522 px сверху, как Floor_02.
        const float CorridorPpu = 153.6f;
        const float CorridorHalfH = 1736f / 2f;
        const float BandTop = 522f;
        const float RoomPpu = 100f;
        const float FeetY = -3.3f;       // ReturnPoint/Player, как на Floor_02/03
        const float RoomEntryY = -3.36f; // EntryPoint комнат, как Room_201

        // Спрайты комнат — PPU 100, остальные — PPU 153.6. Лампы и конусы есть в обоих: для комнат — копии Room_*.
        static readonly string[] RoomSprites =
            { "Room101_Background", "Room102_Background", "Room101_ExitDoor", "Room102_ExitDoor", "Room101_Tumba",
              "Room101_Drawer_Open", "Mumbling_Figure", "Worker_BlueSuit", "Vending_Machine", "Arcade_Machine",
              "Arcade_Screen_Glow", "Silhouette_ChairMan", "Room_Light_Tube_On", "Room_Light_Cone", "Key_102", "Locker_Tall" };


        static Floor01Art()
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

        [MenuItem("Overworked/P4/Art/Build Floor_01 Art")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;

            ImportSprites();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var corridor = Find(scene, "Environment/Corridor");
            var room101 = Find(scene, "Environment/Rooms/Room_101");
            var room102 = Find(scene, "Environment/Rooms/Room_102");
            if (corridor == null || room101 == null || room102 == null)
            {
                Debug.LogError("[Setup] Floor_01: нет Environment/Corridor или Rooms/Room_101 / Room_102");
                return;
            }

            BuildCorridor(corridor.transform, room101.GetComponent<Room>());
            BuildRoom101(room101.transform);
            BuildRoom102(room102.transform);
            PlacePlayer();
            NoDarkening(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(previous) && previous != ScenePath)
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log("[Setup] Floor_01: арт коридора (V1/V2/V3) и комнат 101, 102 собран");
        }

        // ------------------------------------------------------------ импорт

        static void ImportSprites()
        {
            // Лампа и конус нужны в двух масштабах: для комнат — копии с префиксом Room_.
            foreach (var n in new[] { "Light_Tube_On", "Light_Cone" })
                if (!File.Exists(ArtDir + "Room_" + n + ".png") && File.Exists(ArtDir + n + ".png"))
                    File.Copy(ArtDir + n + ".png", ArtDir + "Room_" + n + ".png");
            AssetDatabase.Refresh();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                // backgrn/ — справочные превью и листы художника, в игру не идут: их импорт не трогаем.
                if (ti == null || path.Contains("/backgrn/")) continue;
                string name = Path.GetFileNameWithoutExtension(path);
                bool roomSprite = System.Array.IndexOf(RoomSprites, name) >= 0 || name.StartsWith("ChairMan_LookBack_");
                float ppu = roomSprite ? RoomPpu : CorridorPpu;
                bool wide = name == "Floor01_Corridor";
                if (ti.textureType == TextureImporterType.Sprite && ti.spriteImportMode == SpriteImportMode.Single
                    && Mathf.Approximately(ti.spritePixelsPerUnit, ppu) && ti.textureCompression == TextureImporterCompression.Uncompressed
                    && (!wide || ti.maxTextureSize == 8192))
                    continue;
                // Как у арта Floor_02: Sprite, Bilinear, без сжатия, пивот по центру.
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
            if (s == null) Debug.LogError($"[Setup] Floor_01: нет спрайта {ArtDir}{name}.png");
            return s;
        }

        // ------------------------------------------------------------ коридор

        // Пиксели превью (x, y сверху, w, h в полосе 7680×1080) → центр в мире коридора.
        static Vector3 C(float x, float y, float w, float h) =>
            new Vector3((x + w / 2f - 3840f) / CorridorPpu, (CorridorHalfH - BandTop - (y + h / 2f)) / CorridorPpu, 0f);

        static void BuildCorridor(Transform corridor, Room room101)
        {
            var bg = Child(corridor, "Background");
            foreach (Transform t in bg) if (t.name != "BackgroundArt") t.gameObject.SetActive(false);
            Place(bg, "BackgroundArt", "Floor01_Corridor", Vector3.zero, -9);

            var walls = Child(corridor, "Walls");
            SetWall(walls.Find("WallLeft"), -25.5f);
            SetWall(walls.Find("WallRight"), 25.5f);
            SetBounds(corridor.Find("CameraBounds"), 25f, 5.65f);

            // Двери: декоративные заглушки каркаса убираем, Door_Room_101/102 (с DoorInteractable) переодеваем.
            var doors = Child(corridor, "Doors");
            for (int i = doors.childCount - 1; i >= 0; i--)
            {
                var d = doors.GetChild(i);
                if (d.name == "Door_Room_101" || d.name == "Door_Room_102") continue;
                if (d.name == "Label") { d.gameObject.SetActive(false); continue; }
                Object.DestroyImmediate(d.gameObject);
            }
            var door101Pos = C(2391, 440, 249, 560);
            // CHILL ZONE 102 — на месте тёмной фигуры из V2 (Anomaly_Figure_Ahead, x 4880–5140):
            // правым краем к цветку (x 5220), левым — сразу за висящей лампочкой (x ≈ 4510).
            var door102Pos = C(4517, 340, 688, 660);
            var door101 = Dress(doors.Find("Door_Room_101"), "Door_101", door101Pos, -7, 0f);
            // Дверь 102 — стеклянная стена комнаты отдыха; подсказка E — только у самой двери, не по всей ширине.
            var door102 = Dress(doors.Find("Door_Room_102"), "Door_102_ChillZone", door102Pos, -7, 2.4f);
            if (door101 == null || door102 == null)
                Debug.LogError("[Setup] Floor_01: нет Doors/Door_Room_101 или Door_Room_102 — сначала Overworked → P2 → All Floors → Wire Rooms On All Floors");
            Place(doors, "Door_Archive", "Door_Archive", C(829, 400, 302, 600), -7);

            var returns = corridor.Find("ReturnPoints");
            if (returns != null)
            {
                var back101 = returns.Find("Room_101");
                var back102 = returns.Find("Room_102");
                if (back101 != null) back101.localPosition = new Vector3(door101Pos.x, FeetY, 0f);
                if (back102 != null) back102.localPosition = new Vector3(door102Pos.x, FeetY, 0f);
            }

            BuildMainExit(corridor);

            // Постоянные предметы (V1 и дальше).
            var props = Child(corridor, "Props");
            Clear(props);
            Place(props, "Painting_Keyhole", "Painting_Keyhole", C(1708, 420, 194, 300), -7);
            Place(props, "Anomaly_Handprints", "Anomaly_Handprints", C(2690, 561, 310, 248), -8);
            Place(props, "News_Board", "News_Board", C(3158, 330, 384, 530), -7);
            Place(props, "Plant_Augustin", "Plant_Augustin", C(5220, 415, 391, 585), -7);
            Place(props, "Poster_Crocodile", "Poster_Crocodile", C(5980, 332, 720, 366), -7);
            var booth = Place(props, "Security_Booth", "Security_Booth", C(6094, 560, 493, 440), -6);
            var forms = Place(props, "Resignation_Forms", "Resignation_Forms", C(6434, 776, 108, 33), -5);
            // Необязательные описания сцены 3.0: тёмное стекло будки охраны и стопка бланков на ней.
            Inspect(booth, InspectGlassPath, -0.8f);
            Inspect(forms, InspectFormsPath, 0.8f);

            BuildVariants(corridor, room101, door102);

            // Горящие лампы с превью — Light_Tube_On + Light_Cone с левым краем трубки x = 350, 2110, 3870, 5630
            // (y 241; конус: x − 359, y 272). Этаж 1: лампы «постоянно мигают» (паспорт, раздел 8) — LightFlicker.
            var fixtures = new List<FloorLamps.Fixture>();
            foreach (var x in new[] { 350f, 2110f, 3870f, 5630f })
                fixtures.Add(new FloorLamps.Fixture(C(x, 241, 302, 62), C(x - 359, 272, 1020, 768)));
            FloorLamps.Build(Child(corridor, "Lights"), Spr("Light_Tube_On"), Spr("Light_Cone"), fixtures,
                SickLight, Child(corridor, "Background"));
        }

        /// <summary>Свет этажа 1: желтоватый, тусклее, мерцает на низком рассудке.</summary>
        static readonly FloorLamps.Style SickLight = new FloorLamps.Style
        {
            Color = new Color(0.95f, 0.9f, 0.7f), Intensity = 0.28f, Flicker = true, DarkLevel = 0.4f, ConeDim = 0.5f,
        };

        /// <summary>Лампы комнат 101/102: ещё вдвое слабее коридора (комната маленькая, свет заливал её целиком).</summary>
        static readonly FloorLamps.Style RoomSickLight = new FloorLamps.Style
        {
            Color = SickLight.Color, Intensity = 0.14f, Flicker = true, DarkLevel = 0.4f, ConeDim = 0.75f,
        };

        // Главный выход — стеклянные двери справа (Exit_Door_Glass), заглушку прячем, замок и текст остаются.
        static void BuildMainExit(Transform corridor)
        {
            var exit = corridor.Find("MainExit");
            if (exit == null)
            {
                Debug.LogWarning("[Setup] Floor_01: нет MainExit — сначала Overworked → P2 → Floor_01 → Build Floor_01 Corridor");
                return;
            }
            var pos = C(6960, 432, 500, 568);
            exit.localPosition = pos;
            var placeholder = exit.Find("Sprite");
            if (placeholder != null) placeholder.gameObject.SetActive(false);
            if (!exit.TryGetComponent(out SpriteRenderer sr)) sr = exit.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Spr("Exit_Door_Glass");
            sr.color = Color.white;
            sr.sortingOrder = -7;
            if (exit.TryGetComponent(out BoxCollider2D col)) { col.size = sr.sprite.bounds.size; col.offset = Vector2.zero; }

            // Двери 102 на старте нет (V1) — задание «Find the key in room 101» выдаёт проверка выхода.
            var findKey = AssetDatabase.LoadAssetAtPath<ObjectiveData>(FindKeyPath);
            if (findKey == null || !exit.TryGetComponent(out DoorInteractable door)) return;
            if (!exit.TryGetComponent(out ObjectiveGiver giver)) giver = exit.gameObject.AddComponent<ObjectiveGiver>();
            RoomWiring.Set(giver, ("objective", findKey));
            for (int i = door.onLockedTried.GetPersistentEventCount() - 1; i >= 0; i--)
                if (door.onLockedTried.GetPersistentTarget(i) == giver) UnityEventTools.RemovePersistentListener(door.onLockedTried, i);
            UnityEventTools.AddPersistentListener(door.onLockedTried, giver.Give);
            EditorUtility.SetDirty(door);
        }

        // V2 / V3 из превью: что появляется после выхода из Room_101.
        static void BuildVariants(Transform corridor, Room room101, Transform door102)
        {
            var root = Child(corridor, "Variants");
            Clear(root);

            var after = Child(root, "AfterRoom101");
            Place(after, "Anomaly_WallWriting", "Anomaly_WallWriting", C(3600, 385, 520, 330), -8);
            Place(after, "Anomaly_Spider", "Anomaly_Spider", C(5240, 270, 180, 150), -6);

            // Галлюцинации без ключа: картинки поверх обычных (те же прямоугольники, order выше).
            var noKey = Child(root, "NoKey");
            Place(noKey, "Door_Archive_Open", "Door_Archive_Open", C(829, 400, 302, 600), -6);
            Place(noKey, "Painting_Keyhole_Upside", "Painting_Keyhole_Upside", C(1708, 420, 194, 300), -6);
            Place(noKey, "Anomaly_Eyes", "Anomaly_Eyes", C(18, 503, 142, 487), -6);
            // Фигуры впереди (Anomaly_Figure_Ahead с превью V2) в этом варианте нет — решение команды;
            // её место с ключом занимает дверь CHILL ZONE 102.

            after.gameObject.SetActive(false);
            noKey.gameObject.SetActive(false);
            if (door102 != null) door102.gameObject.SetActive(false);

            var variants = root.gameObject.AddComponent<CorridorVariants>();
            var so = new SerializedObject(variants);
            so.FindProperty("watchedRoom").objectReferenceValue = room101;
            so.FindProperty("flag").stringValue = "Key102";
            SetArray(so.FindProperty("afterVisit"), after.gameObject);
            SetArray(so.FindProperty("withoutFlag"), noKey.gameObject);
            SetArray(so.FindProperty("withFlag"), door102 != null ? door102.gameObject : null);
            so.ApplyModifiedPropertiesWithoutUndo();
            if (room101 == null) Debug.LogWarning("[Setup] Floor_01: у Room_101 нет компонента Room — варианты коридора не переключатся");
        }

        // ------------------------------------------------------------ комнаты

        // Пиксели комнаты 1920×1080 → локальный центр в Room_X.
        static Vector3 R(float x, float y, float w, float h) =>
            new Vector3((x + w / 2f - 960f) / RoomPpu, (540f - (y + h / 2f)) / RoomPpu, 0f);

        static void BuildRoom101(Transform room)
        {
            RoomShell(room, "Room101_Background", "Room101_ExitDoor", R(103, 515, 215, 485), -7.5f);

            var props = Child(room, "Props");
            // Тумба P2 (Cabinet с DrawerInteractable) — встаёт на место тумбы с превью, заглушку прячем.
            var tumbaPos = R(1008, 720, 245, 280);
            var cabinet = props.Find("Cabinet");
            for (int i = props.childCount - 1; i >= 0; i--)
                // Ключ-карту 102 (Pickup P2, флаг Key102) не трогаем: без неё дверь 102 не открыть.
                if (props.GetChild(i) != cabinet && props.GetChild(i).name != "Key102") Object.DestroyImmediate(props.GetChild(i).gameObject);
            // Тумба, ящик и бормочущий — под конусом света лампы (order -6), как стол с компьютером в Room_201:
            // поверх дымки конуса они выбивались из фона.
            var drawerOpen = Place(props, "Room101_Drawer_Open", "Room101_Drawer_Open", R(1018, 728, 225, 105), -7);
            drawerOpen.SetActive(false);
            if (cabinet != null)
            {
                cabinet.localPosition = tumbaPos;
                var placeholder = cabinet.Find("Sprite");
                if (placeholder != null) placeholder.gameObject.SetActive(false);
                var art = Place(cabinet, "Art", "Room101_Tumba", Vector3.zero, -8);
                if (cabinet.TryGetComponent(out BoxCollider2D col)) { col.size = art.GetComponent<SpriteRenderer>().sprite.bounds.size; col.offset = Vector2.zero; }
                // Ящик открыт — показать выдвинутый ящик (подписка без дублей при пересборке).
                if (cabinet.TryGetComponent(out DrawerInteractable drawer))
                {
                    RemoveListenersTo(drawer.onDone, drawerOpen);
                    UnityEventTools.AddBoolPersistentListener(drawer.onDone, drawerOpen.SetActive, true);
                    EditorUtility.SetDirty(drawer);
                }
            }
            else Debug.LogWarning("[Setup] Floor_01: нет Room_101/Props/Cabinet — сначала Overworked → P2 → Build Floor_01 Room 101 Drawer");

            // Ключ-карта лежит на столе (нарисован на фоне) рядом с листками — видна сразу, подбирается по E.
            // Pickup ставит P2 (Floor01Room101); если его нет — создаём такой же, иначе дверь 102 не открыть.
            var key = props.Find("Key102");
            if (key == null) key = CreateKeyPickup(props);
            key.gameObject.SetActive(true);
            key.localPosition = R(590, 750, 103, 47);
            var keyPlaceholder = key.Find("Sprite");
            if (keyPlaceholder != null) keyPlaceholder.gameObject.SetActive(false);
            Place(key, "Art", "Key_102", Vector3.zero, -7);
            if (key.TryGetComponent(out BoxCollider2D keyCol)) { keyCol.size = new Vector2(1.2f, 0.8f); keyCol.offset = Vector2.zero; }

            // Сидящий человек (сцена 3.1) — слева от фигуры в костюме, кадры ChairMan_LookBack (спиной, оборачивается).
            // До E — первый кадр. По E кадры проигрываются один раз под сильный глитч (GlitchVanish), потом он
            // просто пропадает: сжатие PersonInteractable направлено на пустую заглушку, сама фигура не уменьшается.
            var frames = new Sprite[8];
            for (int i = 0; i < frames.Length; i++) frames[i] = Spr($"ChairMan/ChairMan_LookBack_{i}");
            var person = Place(props, "Mumbling_Figure", "ChairMan/ChairMan_LookBack_0", R(1440, 555, 239, 440), -7);
            var pcol = person.AddComponent<BoxCollider2D>();
            pcol.isTrigger = true;
            pcol.size = new Vector2(1.6f, 3f);
            pcol.offset = new Vector2(0f, RoomEntryY - person.transform.localPosition.y);
            var dummy = new GameObject("ShrinkDummy").transform;
            dummy.SetParent(person.transform, false);
            var glitch = person.AddComponent<GlitchVanish>();
            RoomWiring.Set(glitch, ("watch", dummy), ("fps", 8f), ("jumpDistance", 0.5f), ("dropChance", 0.35f));
            var so = new SerializedObject(glitch);
            var list = so.FindProperty("frames");
            list.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            // 8 кадров при 8 fps = 1 с анимации + 0.3 с глитча на последнем кадре, затем исчезает.
            // Разговор с ним — обязательное задание F1_TalkToMan (P1, Floor01TalkToMan); ассета нет — просто без задания.
            RoomWiring.Set(person.AddComponent<PersonInteractable>(), ("prompt", "Interact"), ("visual", dummy),
                ("shrinkTime", 1.3f), ("afterDialogue", AssetDatabase.LoadAssetAtPath<DialogueData>(PersonGonePath)),
                ("completesObjective", AssetDatabase.LoadAssetAtPath<ObjectiveData>(Floor01TalkToMan.TalkPath)));

            RoomLamp(room);
            // Фон 101 — самый тёмный из всех (средняя яркость ~15/255): картинку не трогаем, а подсвечиваем
            // комнату ровным рассеянным светом, чтобы надписи и мебель на стенах читались.
            FloorLamps.AddAmbient(Child(room, "Lights"), 0.9f, SickLight.Color);
        }

        /// <summary>
        /// Ключ-карта 102 — так же, как её ставит P2 (Floor01Room101): подбор по E, тост, флаг Key102,
        /// задание «Find the key in room 101» выполнено. «Finish work in room 102» — после сообщения Босса (P2StoryWiring).
        /// </summary>
        static Transform CreateKeyPickup(Transform props)
        {
            var key = new GameObject("Key102");
            key.transform.SetParent(props, false);
            var col = key.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            var pickup = key.AddComponent<PickupInteractable>();
            RoomWiring.Set(pickup,
                ("prompt", "Pick up the key"),
                ("receivedToast", "Received: Room 102 key."),
                ("setsFlag", "Key102"),
                ("completesObjective", AssetDatabase.LoadAssetAtPath<ObjectiveData>(FindKeyPath)));
            // «Finish work in room 102» выдаёт не ключ, а цепочка 3.1 AfterKey после сообщения Босса
            // (Overworked → P2 → All Floors → Wire Story And Hallucinations).
            EditorUtility.SetDirty(pickup);
            Debug.Log("[Setup] Floor_01: ключ-карта Key102 создана");
            return key.transform;
        }

        static void BuildRoom102(Transform room)
        {
            // Выход справа; Дэнни появляется левее двери, чтобы не стоять в правой стене (±8.5).
            RoomShell(room, "Room102_Background", "Room102_ExitDoor", R(1630, 555, 260, 445), 7.3f);

            var props = Child(room, "Props");
            var arcade = props.Find("Arcade");
            for (int i = props.childCount - 1; i >= 0; i--)
                if (props.GetChild(i) != arcade) Object.DestroyImmediate(props.GetChild(i).gameObject);
            // Тёмный шкаф слева (превью Preview_Floor01_Room102). Стол со стулом за ним стёрт с фона Room102_Background.
            Place(props, "Locker_Tall", "Locker_Tall", R(643, 440, 237, 560), -7);
            Place(props, "Vending_Machine", "Vending_Machine", R(1060, 431, 250, 569), -7);
            // Свечения экрана аркады пока нет (Arcade_Screen_Glow: R(1320, 462, 300, 300), гаснет после победы).
            if (arcade != null)
            {
                arcade.localPosition = R(1356, 470, 228, 530);
                var placeholder = arcade.Find("Sprite");
                if (placeholder != null) placeholder.gameObject.SetActive(false);
                var art = Place(arcade, "Art", "Arcade_Machine", Vector3.zero, -7);
                if (arcade.TryGetComponent(out BoxCollider2D col)) { col.size = art.GetComponent<SpriteRenderer>().sprite.bounds.size; col.offset = Vector2.zero; }
                // Подписку на свечение от прошлой сборки убираем — объект свечения удалён выше.
                if (arcade.TryGetComponent(out ArcadeInteractable game))
                {
                    for (int i = game.onDone.GetPersistentEventCount() - 1; i >= 0; i--)
                        if (game.onDone.GetPersistentTarget(i) == null) UnityEventTools.RemovePersistentListener(game.onDone, i);
                    EditorUtility.SetDirty(game);
                }
            }
            else Debug.LogWarning("[Setup] Floor_01: нет Room_102/Props/Arcade — сначала Overworked → P2 → Floor_01 → Place Floor_01 Arcade");

            RoomLamp(room);
        }

        // Фон, стены, рамка камеры, дверь выхода и точка входа комнаты — как Room_201 на Floor_02.
        static void RoomShell(Transform room, string background, string exitSprite, Vector3 exitPos, float entryX)
        {
            var bg = Child(room, "Background");
            foreach (Transform t in bg) if (t.name != "BackgroundArt") t.gameObject.SetActive(false);
            Place(bg, "BackgroundArt", background, Vector3.zero, -10);

            var walls = Child(room, "Walls");
            foreach (Transform w in walls) if (w.TryGetComponent(out SpriteRenderer wsr)) wsr.enabled = false;
            SetBounds(room.Find("CameraBounds"), 9.6f, 5.4f);

            Dress(room.Find("ExitDoor/Door"), exitSprite, exitPos, -8, 0f);
            var label = room.Find("ExitDoor/Label");
            if (label != null) label.gameObject.SetActive(false);
            var entry = room.Find("EntryPoint");
            if (entry != null) entry.localPosition = new Vector3(entryX, RoomEntryY, 0f);
        }

        // Лампа комнаты — Room_Light_Tube_On R(810, 241, 302, 62) и Room_Light_Cone R(451, 272, 1020, 768).
        // Без BackgroundDimmer: он затемняет всю сцену и спорил бы с затемнением коридора.
        static void RoomLamp(Transform room) =>
            FloorLamps.Build(Child(room, "Lights"), Spr("Room_Light_Tube_On"), Spr("Room_Light_Cone"),
                new[] { new FloorLamps.Fixture(R(810, 241, 302, 62), R(451, 272, 1020, 768)) }, RoomSickLight, null);

        // ------------------------------------------------------------ старт

        static void PlacePlayer()
        {
            // Старт — слева у тёмного проёма лестницы (откуда Дэнни спустился), как старт Floor_02.
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null) player.transform.position = new Vector3(-22f, FeetY, 0f);
            else Debug.LogWarning("[Setup] Floor_01: Player не найден — старт не сдвинут");
        }

        static void NoDarkening(Scene scene)
        {
            var global = Find(scene, "Global Light 2D");
            var light = global != null ? global.GetComponent<UnityEngine.Rendering.Universal.Light2D>() : null;
            if (light != null) light.intensity = 1f;
            else Debug.LogWarning("[Setup] Floor_01: Global Light 2D не найден — общий свет не изменён");
        }

        // ------------------------------------------------------------ помощники

        // Переодеть дверь с DoorInteractable: спрайт, место, коллайдер (colliderWidth 0 — по спрайту).
        static Transform Dress(Transform door, string sprite, Vector3 pos, int order, float colliderWidth)
        {
            if (door == null) return null;
            door.localPosition = pos;
            door.localScale = Vector3.one;
            if (!door.TryGetComponent(out SpriteRenderer sr)) sr = door.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Spr(sprite);
            sr.color = Color.white;
            sr.sortingOrder = order;
            if (door.TryGetComponent(out BoxCollider2D col) && sr.sprite != null)
            {
                var size = sr.sprite.bounds.size;
                col.size = new Vector2(colliderWidth > 0f ? colliderWidth : size.x, size.y);
                col.offset = Vector2.zero;
            }
            return door;
        }

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

        // Зона подхода — от пола до роста Дэнни; offsetX разводит соседние подсказки (стекло и бланки).
        static void Inspect(GameObject go, string dialoguePath, float offsetX)
        {
            var data = AssetDatabase.LoadAssetAtPath<DialogueData>(dialoguePath);
            if (data == null) return;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.4f, 3f);
            col.offset = new Vector2(offsetX, FeetY - go.transform.position.y);
            RoomWiring.Set(go.AddComponent<Inspectable>(), ("dialogue", data));
        }

        static void RemoveListenersTo(UnityEventBase ev, Object target)
        {
            for (int i = ev.GetPersistentEventCount() - 1; i >= 0; i--)
                if (ev.GetPersistentTarget(i) == target) UnityEventTools.RemovePersistentListener(ev, i);
        }

        static void SetArray(SerializedProperty prop, GameObject value)
        {
            prop.arraySize = value != null ? 1 : 0;
            if (value != null) prop.GetArrayElementAtIndex(0).objectReferenceValue = value;
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
