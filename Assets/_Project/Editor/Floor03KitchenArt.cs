using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Кухня Floor_03 — арт вместо заглушек: дверь EXIT — спрайт Kitchen_ExitDoor, пропуск Макса — Max_Pass
    /// (Art/Environment/Floor03). Спрайты в родном размере (масштаб 1), дверь стоит на полу (-4.2),
    /// коллайдер двери — по размеру спрайта, чтобы E срабатывала по всей двери.
    /// Трогает только Kitchen/ExitDoor/Door и Kitchen/Props/MaxPass/Sprite.
    /// Меню: Overworked → P2 → Floor_03 Kitchen Art.
    /// </summary>
    [InitializeOnLoad]
    public static class Floor03KitchenArt
    {
        const string ScenePath = "Assets/_Project/Scenes/Floor_03.unity";
        const string RequestFile = "Temp/p2_kitchen_art.request";
        const string Art = "Assets/_Project/Art/Environment/Floor03/";
        const float FloorY = -4.2f;

        static Floor03KitchenArt()
        {
            if (File.Exists(RequestFile)) EditorApplication.delayCall += RunRequest;
        }

        static void RunRequest()
        {
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

        [MenuItem("Overworked/P2/Floor_03 Kitchen Art")]
        public static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            // Спрайты — после OpenScene (иначе сцена выгрузит их и сохранится пустая ссылка).
            var doorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Kitchen_ExitDoor.png");
            var passSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Max_Pass.png");

            Transform kitchen = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Environment") kitchen = root.transform.Find("Rooms/Kitchen");
            if (kitchen == null || doorSprite == null || passSprite == null)
            {
                Debug.LogError("[Setup] Floor_03: нет кухни или спрайтов Kitchen_ExitDoor / Max_Pass");
                return;
            }

            var door = kitchen.Find("ExitDoor/Door");
            if (door != null && door.TryGetComponent(out SpriteRenderer doorSr))
            {
                Sprite(door, doorSr, doorSprite);
                // Низ двери — на полу комнаты.
                float h = doorSprite.bounds.size.y;
                door.localPosition = new Vector3(door.localPosition.x, FloorY + h / 2f, door.localPosition.z);
                if (door.TryGetComponent(out BoxCollider2D col))
                {
                    col.size = doorSprite.bounds.size;
                    col.offset = Vector2.zero;
                }
            }

            var pass = kitchen.Find("Props/MaxPass/Sprite");
            if (pass != null && pass.TryGetComponent(out SpriteRenderer passSr)) Sprite(pass, passSr, passSprite);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(previous) && previous != ScenePath)
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log("[Setup] Floor_03: кухня — дверь EXIT и пропуск со спрайтами");
        }

        // Спрайт в родном размере и цвете.
        static void Sprite(Transform t, SpriteRenderer sr, Sprite sprite)
        {
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Simple;
            sr.color = Color.white;
            t.localScale = Vector3.one;
            EditorUtility.SetDirty(t.gameObject);
        }
    }
}
