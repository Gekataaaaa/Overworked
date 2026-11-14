using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SB = Overworked.EditorTools.PlayerSandboxBuilder;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Каркасы этажей на серых прямоугольниках: коридор со стенами, комнаты, игрок, камеры, лестница.
    /// Запуск: Overworked → P1 → Build Floor Skeletons. Этаж, где в Corridor/Background уже что-то есть,
    /// пропускается — чужую работу не перезаписывает. Дальше этажи собирают их владельцы (docs/TASKS.md).
    /// </summary>
    public static class FloorSkeletonBuilder
    {
        const float HalfHeight = 5.4f;
        const float FloorY = -4.2f;
        const float Margin = 0.25f;
        const float RoomHalfWidth = 8f;
        const string FontPath = "Assets/_Project/Fonts/PTSans SDF.asset";

        /// <summary>Комната: имя объекта в Environment/Rooms, подпись на двери, X двери в коридоре.</summary>
        struct RoomSpec
        {
            public string Name, Label;
            public float DoorX;
            public RoomSpec(string name, string label, float doorX) { Name = name; Label = label; DoorX = doorX; }
        }

        /// <summary>Этаж: длина коридора, палитра, комнаты, куда ведёт лестница (-1 — лестницы нет).</summary>
        struct FloorSpec
        {
            public string Scene;
            public float HalfWidth;
            public Color Wall, Floor;
            public RoomSpec[] Rooms;
            public int StairsTo;
        }

        // Палитра по паспорту: грязно-зелёный, серый, выцветший синий; каждый этаж темнее и страннее.
        static readonly FloorSpec[] Floors =
        {
            new FloorSpec
            {
                Scene = "Floor_03", HalfWidth = 25f, StairsTo = 2,
                Wall = new Color(0.32f, 0.36f, 0.33f), Floor = new Color(0.2f, 0.22f, 0.21f),
                Rooms = new[] { new RoomSpec("Kitchen", "KITCHEN", 6f) },
            },
            new FloorSpec
            {
                Scene = "Floor_02", HalfWidth = 22f, StairsTo = 1,
                Wall = new Color(0.2f, 0.23f, 0.28f), Floor = new Color(0.11f, 0.12f, 0.14f),
                Rooms = new[] { new RoomSpec("Room_201", "201", 4f) },
            },
            new FloorSpec
            {
                // Этаж «неестественно вытянут» (паспорт, раздел 8), лестницы нет.
                Scene = "Floor_01", HalfWidth = 36f, StairsTo = -1,
                Wall = new Color(0.31f, 0.32f, 0.25f), Floor = new Color(0.16f, 0.15f, 0.12f),
                Rooms = new[] { new RoomSpec("Room_101", "101", -6f), new RoomSpec("Room_102", "102", 14f) },
            },
        };

        [MenuItem("Overworked/P1/Build Floor Skeletons")]
        public static void Run()
        {
            var square = SB.EnsureSquareSprite();
            var player = SB.EnsurePlayerPrefab(square);
            var camera = SB.EnsureCameraPrefab();
            var stairs = SB.EnsureStairsPrefab(square);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            foreach (var f in Floors) BuildFloor(f, square, player, camera, stairs, font);
            Debug.Log("[Setup] Floor skeletons: готово");
        }

        static void BuildFloor(FloorSpec f, Sprite square, GameObject playerPrefab, GameObject cameraPrefab, GameObject stairsPrefab, TMP_FontAsset font)
        {
            var scene = EditorSceneManager.OpenScene($"Assets/_Project/Scenes/{f.Scene}.unity", OpenSceneMode.Single);
            GameObject Root(string n)
            {
                foreach (var go in scene.GetRootGameObjects()) if (go.name == n) return go;
                return new GameObject(n);
            }

            var env = Root("Environment");
            var corridor = Child(env, "Corridor");
            var background = Child(corridor, "Background");
            if (background.transform.childCount > 0)
            {
                Debug.LogWarning($"[Setup] {f.Scene}: коридор уже собран — пропускаю");
                return;
            }

            // --- Коридор
            float top = HalfHeight + Margin, bottom = -HalfHeight - Margin, w = f.HalfWidth;
            SB.Rect(background, "BackWall", square, new Vector2(0, (top + FloorY) / 2), new Vector2(w * 2, top - FloorY), f.Wall, -10);
            SB.Rect(background, "Floor", square, new Vector2(0, (FloorY + bottom) / 2), new Vector2(w * 2, FloorY - bottom), f.Floor, -5);
            Walls(corridor, square, w, Dim(f.Wall, 0.5f));
            var corridorBounds = Bounds(corridor, w + 1f);

            // Декоративные закрытые двери каждые 7 юнитов, кроме мест под двери комнат.
            var doors = Child(corridor, "Doors");
            for (float x = -w + 5f; x < w - 4f; x += 7f)
            {
                bool nearRoom = false;
                foreach (var r in f.Rooms) nearRoom |= Mathf.Abs(r.DoorX - x) < 3f;
                if (!nearRoom) Door(doors, square, $"Door_{Mathf.RoundToInt(x)}", x, new Color(0.38f, 0.35f, 0.3f), null, font);
            }

            // --- Комнаты: ниже коридора (их позиции задал ProjectSetup), вход/выход — дело DoorInteractable (P2).
            var rooms = Child(env, "Rooms");
            var cameras = Root("Cameras");
            foreach (Transform t in cameras.transform) if (t.name == "CM Corridor") { Object.DestroyImmediate(t.gameObject); break; }

            foreach (var r in f.Rooms)
            {
                Door(doors, square, $"Door_{r.Name}", r.DoorX, new Color(0.55f, 0.47f, 0.3f), r.Label, font);

                var room = Child(rooms, r.Name);
                var roomBg = Child(room, "Background");
                SB.Rect(roomBg, "BackWall", square, new Vector2(0, (top + FloorY) / 2), new Vector2(RoomHalfWidth * 2, top - FloorY), Dim(f.Wall, 0.85f), -10);
                SB.Rect(roomBg, "Floor", square, new Vector2(0, (FloorY + bottom) / 2), new Vector2(RoomHalfWidth * 2, FloorY - bottom), Dim(f.Floor, 0.9f), -5);
                Walls(room, square, RoomHalfWidth, Dim(f.Wall, 0.45f));
                var roomBounds = Bounds(room, RoomHalfWidth + 1f);

                Child(room, "EntryPoint").transform.localPosition = new Vector3(-RoomHalfWidth + 2.5f, FloorY + 0.9f, 0);
                var exit = Child(room, "ExitDoor");
                Door(exit, square, "Door", -RoomHalfWidth + 1f, new Color(0.55f, 0.47f, 0.3f), "EXIT", font);

                // Камера комнаты выключена: её включает дверь при входе (P2).
                var roomCam = MakeCamera(cameras, cameraPrefab, $"CM {r.Name}", roomBounds, scene);
                roomCam.SetActive(false);
            }

            MakeCamera(cameras, cameraPrefab, "CM Corridor", corridorBounds, scene).transform.SetAsFirstSibling();

            // --- Игрок: вместо пустого объекта Player из шаблона — префаб.
            var oldPlayer = Root("Player");
            if (oldPlayer.GetComponent<PlayerController>() == null && oldPlayer.transform.childCount == 0)
                Object.DestroyImmediate(oldPlayer);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.position = new Vector3(-w + 3f, FloorY + 0.9f, 0);
            player.transform.SetSiblingIndex(2);

            // --- Лестница в правом конце коридора.
            if (f.StairsTo >= 0)
            {
                var stairs = (GameObject)PrefabUtility.InstantiatePrefab(stairsPrefab, scene);
                stairs.transform.SetParent(Root("Staircase").transform, false);
                stairs.transform.position = new Vector3(w - 2.5f, FloorY + 1.5f, 0);
                var so = new SerializedObject(stairs.GetComponent<StairInteractable>());
                so.FindProperty("targetFloor").intValue = f.StairsTo;
                // E ловит InteractionSystem (P2); автономный режим лестницы только для сцен без него.
                so.FindProperty("standaloneE").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Setup] {f.Scene}: каркас собран");
        }

        // ------------------------------------------------------------ помощники

        /// <summary>Темнее в k раз, альфа остаётся 1.</summary>
        static Color Dim(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);

        static GameObject Child(GameObject parent, string name)
        {
            var t = parent.transform.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static void Walls(GameObject parent, Sprite square, float halfWidth, Color color)
        {
            var walls = Child(parent, "Walls");
            foreach (var x in new[] { -halfWidth - 0.5f, halfWidth + 0.5f })
            {
                var wall = SB.Rect(walls, x < 0 ? "WallLeft" : "WallRight", square, new Vector2(x, 0), new Vector2(1f, (HalfHeight + Margin) * 2), color, -4);
                wall.AddComponent<BoxCollider2D>();
            }
        }

        /// <summary>Границы камеры: по высоте больше кадра, иначе Confiner2D прибьёт камеру к центру.</summary>
        static PolygonCollider2D Bounds(GameObject parent, float halfWidth)
        {
            var go = Child(parent, "CameraBounds");
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

        static void Door(GameObject parent, Sprite square, string name, float x, Color color, string label, TMP_FontAsset font)
        {
            var door = SB.Rect(parent, name, square, new Vector2(x, FloorY + 1.25f), new Vector2(1.4f, 2.5f), color, -8);
            if (string.IsNullOrEmpty(label) || font == null) return;
            // Подпись над дверью — чтобы на прямоугольниках было понятно, где какая комната.
            var go = new GameObject("Label");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = new Vector3(x, FloorY + 3f, 0);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = font;
            tmp.text = label;
            tmp.fontSize = 3.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.85f, 0.85f, 0.8f);
            tmp.sortingOrder = -7;
            tmp.rectTransform.sizeDelta = new Vector2(5f, 1f);
        }

        static GameObject MakeCamera(GameObject parent, GameObject prefab, string name, PolygonCollider2D bounds, UnityEngine.SceneManagement.Scene scene)
        {
            var cam = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            cam.name = name;
            cam.transform.SetParent(parent.transform, false);
            cam.GetComponent<Unity.Cinemachine.CinemachineConfiner2D>().BoundingShape2D = bounds;
            return cam;
        }
    }
}
