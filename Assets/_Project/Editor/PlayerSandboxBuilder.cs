using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Собирает префаб игрока-заглушки и тестовый коридор в Sandbox_P1 (серые прямоугольники).
    /// Запуск: меню Overworked → P1 → Build Player Sandbox,
    /// или batchmode -executeMethod Overworked.EditorTools.PlayerSandboxBuilder.Run.
    /// Префаб не перезаписывается, если уже есть; объекты коридора в Sandbox_P1 пересоздаются.
    /// </summary>
    public static class PlayerSandboxBuilder
    {
        const string SquarePath = "Assets/_Project/Art/Placeholder_Square.png";
        const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        const string ScenePath = "Assets/_Project/Scenes/Sandbox/Sandbox_P1.unity";

        // Размеры коридора в юнитах. Камера ортографическая 5.4 → видно 19.2 × 10.8.
        const float HalfWidth = 30f;
        const float HalfHeight = 5.4f;
        // Линия пола (горизонт): пол занимает нижние 1.2 юнита кадра.
        const float FloorY = -4.2f;
        // Запас границ камеры по вертикали. Если границы ровно равны кадру, Confiner2D
        // сжимает их в отрезок и прибивает камеру к центру — она перестаёт ехать за игроком.
        const float BoundsMargin = 0.25f;

        [MenuItem("Overworked/P1/Build Player Sandbox")]
        public static void Run()
        {
            var square = EnsureSquareSprite();
            var prefab = EnsurePlayerPrefab(square);
            BuildScene(square, prefab);
            Debug.Log("[Setup] Sandbox_P1: коридор и игрок готовы");
        }

        /// <summary>Белый квадрат 1×1 юнит — заглушка для всех прямоугольников до прихода арта.</summary>
        static Sprite EnsureSquareSprite()
        {
            if (!File.Exists(SquarePath))
            {
                var tex = new Texture2D(4, 4);
                var pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                tex.SetPixels(pixels);
                File.WriteAllBytes(SquarePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(SquarePath);

                var importer = (TextureImporter)AssetImporter.GetAtPath(SquarePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 4;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        }

        static GameObject EnsurePlayerPrefab(Sprite square)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (existing != null) return existing;

            var root = new GameObject("Player");
            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.8f);
            root.AddComponent<PlayerController>();
            root.AddComponent<PlayerAnimator>();

            var visual = new GameObject("Sprite");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(0.8f, 1.8f, 1f);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = new Color(0.85f, 0.82f, 0.7f);
            sr.sortingOrder = 10;

            // Маркер «нос» — видно, куда смотрит Дэнни, пока нет арта.
            var nose = new GameObject("FacingMarker");
            nose.transform.SetParent(visual.transform, false);
            nose.transform.localPosition = new Vector3(0.35f, 0.3f, 0f);
            nose.transform.localScale = new Vector3(0.3f, 0.1f, 1f);
            var nsr = nose.AddComponent<SpriteRenderer>();
            nsr.sprite = square;
            nsr.color = new Color(0.2f, 0.2f, 0.2f);
            nsr.sortingOrder = 11;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void BuildScene(Sprite square, GameObject playerPrefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects())
                if (go.name is "Environment" or "Player" or "Cameras") Object.DestroyImmediate(go);

            var env = new GameObject("Environment");
            float top = HalfHeight + BoundsMargin, bottom = -HalfHeight - BoundsMargin;
            Rect(env, "BackWall", square, new Vector2(0, (top + FloorY) / 2), new Vector2(HalfWidth * 2, top - FloorY), new Color(0.32f, 0.36f, 0.33f), -10);
            Rect(env, "Floor", square, new Vector2(0, (FloorY + bottom) / 2), new Vector2(HalfWidth * 2, FloorY - bottom), new Color(0.2f, 0.22f, 0.21f), -5);
            // Двери-ориентиры: видно, что игрок движется и камера едет.
            for (int i = -4; i <= 4; i++)
                Rect(env, $"Door_{i + 5}", square, new Vector2(i * 6f, FloorY + 1.25f), new Vector2(1.4f, 2.5f), new Color(0.45f, 0.4f, 0.3f), -8);

            Wall(env, "WallLeft", square, -HalfWidth - 0.5f);
            Wall(env, "WallRight", square, HalfWidth + 0.5f);

            // Границы камеры: Confiner2D не выпускает камеру за коридор.
            var bounds = new GameObject("CameraBounds");
            bounds.transform.SetParent(env.transform, false);
            var poly = bounds.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            poly.points = new[]
            {
                new Vector2(-HalfWidth - 1, bottom), new Vector2(HalfWidth + 1, bottom),
                new Vector2(HalfWidth + 1, top), new Vector2(-HalfWidth - 1, top),
            };

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.position = new Vector3(0, FloorY + 0.9f, 0);

            var cameras = new GameObject("Cameras");
            var vcamGo = new GameObject("CM Corridor");
            vcamGo.transform.SetParent(cameras.transform, false);
            vcamGo.transform.position = new Vector3(0, 0, -10);
            var vcam = vcamGo.AddComponent<CinemachineCamera>();
            vcam.Follow = player.transform;
            var lens = vcam.Lens;
            lens.OrthographicSize = HalfHeight;
            vcam.Lens = lens;

            var composer = vcamGo.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 10f;
            composer.Damping = new Vector3(0.8f, 0f, 0f);
            // Смещение в сторону движения (паспорт, раздел 27).
            composer.Lookahead = new LookaheadSettings { Enabled = true, Time = 0.6f, Smoothing = 8f, IgnoreY = true };

            var confiner = vcamGo.AddComponent<CinemachineConfiner2D>();
            confiner.BoundingShape2D = poly;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static GameObject Rect(GameObject parent, string name, Sprite square, Vector2 pos, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = color;
            sr.sortingOrder = order;
            return go;
        }

        static void Wall(GameObject parent, string name, Sprite square, float x)
        {
            var wall = Rect(parent, name, square, new Vector2(x, 0f), new Vector2(1f, (HalfHeight + BoundsMargin) * 2), new Color(0.15f, 0.16f, 0.15f), -4);
            wall.AddComponent<BoxCollider2D>();
        }
    }
}
