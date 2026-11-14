using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SB = Overworked.EditorTools.PlayerSandboxBuilder;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Аркадный автомат в Room_102 (комната отдыха) Floor_01, нарратив сцена 3.2: «кубик»-заглушка,
    /// триггер, ArcadeInteractable («[E] Play») и TinyUfoMiniGame — по E запускается Tiny UFO.
    /// Реплики после победы и задание «Finish work in room 102» подключаются, когда P3 сделает их ассеты.
    /// Повторный запуск пересоздаёт автомат на том же месте. Арт потом меняется в дочернем Sprite.
    /// Меню: Overworked → P2 → Place Floor_01 Arcade.
    /// </summary>
    [InitializeOnLoad]
    public static class Floor01Arcade
    {
        const string ScenePath = "Assets/_Project/Scenes/Floor_01.unity";

        /// <summary>Файл-запрос: есть при загрузке редактора — автомат ставится сам один раз, файл удаляется.</summary>
        const string RequestFile = "Temp/p2_arcade.request";

        // Справа от двери выхода (она по центру комнаты), чтобы подсказки не перекрывались.
        const float ArcadeX = 4f;
        static readonly Vector2 Size = new Vector2(1.4f, 2.6f);

        static Floor01Arcade()
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
            Place();
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            RunRequest();
        }

        [MenuItem("Overworked/P2/Place Floor_01 Arcade")]
        public static void Place()
        {
            // Несохранённые правки в открытой сцене — спросить, иначе OpenScene их выбросит.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform room = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Environment") room = root.transform.Find("Rooms/Room_102");
            if (room == null)
            {
                Debug.LogError("[Setup] Floor_01: нет Environment/Rooms/Room_102");
                return;
            }

            var props = room.Find("Props");
            if (props == null)
            {
                props = new GameObject("Props").transform;
                props.SetParent(room, false);
            }
            var old = props.Find("Arcade");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // Пол комнаты — на 0.9 ниже точки входа (ноги Дэнни), автомат стоит на нём.
            var entry = room.Find("EntryPoint");
            float floorY = entry != null ? entry.localPosition.y - 0.9f : -4.2f;

            var arcade = new GameObject("Arcade");
            arcade.transform.SetParent(props, false);
            arcade.transform.localPosition = new Vector3(ArcadeX, floorY + Size.y / 2f, 0f);

            var col = arcade.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Size;

            var square = SB.EnsureSquareSprite();
            SB.Rect(arcade, "Sprite", square, Vector2.zero, Size, new Color(0.18f, 0.22f, 0.42f), -7);

            var game = arcade.AddComponent<TinyUfoMiniGame>();
            RoomWiring.Set(arcade.AddComponent<ArcadeInteractable>(), ("prompt", "Play"), ("miniGame", game));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(previous) && previous != ScenePath)
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log("[Setup] Floor_01: аркадный автомат с Tiny UFO стоит в Room_102");
        }
    }
}
