using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SB = Overworked.EditorTools.PlayerSandboxBuilder;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Кухня Floor_03 — вид «как было до пулла main»: дверь EXIT и пропуск Макса снова серые заглушки
    /// (прямоугольники того же размера и цвета, что делал каркас и Floor03Turnstile), вместо арта.
    /// Трогает только Environment/Rooms/Kitchen/ExitDoor/Door и Kitchen/Props/MaxPass/Sprite —
    /// компоненты, подписки сценария и остальной Floor_03 не меняются.
    /// Меню: Overworked → P2 → Floor_03 Kitchen Placeholders.
    /// </summary>
    [InitializeOnLoad]
    public static class Floor03KitchenPlaceholders
    {
        const string ScenePath = "Assets/_Project/Scenes/Floor_03.unity";
        const string RequestFile = "Temp/p2_kitchen_placeholders.request";

        static Floor03KitchenPlaceholders()
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
            Apply();
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            RunRequest();
        }

        [MenuItem("Overworked/P2/Floor_03 Kitchen Placeholders")]
        public static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform kitchen = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Environment") kitchen = root.transform.Find("Rooms/Kitchen");
            if (kitchen == null)
            {
                Debug.LogError("[Setup] Floor_03: нет Environment/Rooms/Kitchen");
                return;
            }
            var square = SB.EnsureSquareSprite();

            // Дверь выхода: прямоугольник 1.4 × 2.5 на полу (низ на -4.2), коричневый — как у каркаса.
            var door = kitchen.Find("ExitDoor/Door");
            if (door != null) Placeholder(door, square, new Vector2(1.4f, 2.5f), -4.2f + 1.25f, new Color(0.55f, 0.47f, 0.3f));

            // Пропуск Макса: светлая карточка 0.6 × 0.4 — как делал Floor03Turnstile.
            var pass = kitchen.Find("Props/MaxPass/Sprite");
            if (pass != null) Placeholder(pass, square, new Vector2(0.6f, 0.4f), pass.localPosition.y, new Color(0.9f, 0.88f, 0.8f));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(previous) && previous != ScenePath)
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log("[Setup] Floor_03: кухня — дверь EXIT и пропуск снова заглушки");
        }

        // Спрайт 1×1 юнит, растянутый масштабом до size; коллайдер (если есть) — по спрайту.
        static void Placeholder(Transform t, Sprite square, Vector2 size, float y, Color color)
        {
            if (!t.TryGetComponent(out SpriteRenderer sr)) return;
            sr.sprite = square;
            sr.drawMode = SpriteDrawMode.Simple;
            sr.color = color;
            t.localScale = new Vector3(size.x, size.y, 1f);
            t.localPosition = new Vector3(t.localPosition.x, y, t.localPosition.z);
            if (t.TryGetComponent(out BoxCollider2D col))
            {
                col.size = Vector2.one;
                col.offset = Vector2.zero;
            }
            EditorUtility.SetDirty(t.gameObject);
        }
    }
}
