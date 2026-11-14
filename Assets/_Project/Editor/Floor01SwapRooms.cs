using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Floor_01: комнаты 101 и 102 меняются местами. Дверь 102 (с подписью и точкой возврата) встаёт
    /// ближе к началу коридора, дверь 101 — дальше; сами комнаты (Room_X со всем содержимым, камерой
    /// и границами) меняются местами под коридором. Результат один и тот же при любом числе запусков:
    /// 102 всегда получает меньший X двери и верхнюю комнату.
    /// Меню: Overworked → P2 → Swap Floor_01 Rooms 101 and 102.
    /// </summary>
    [InitializeOnLoad]
    public static class Floor01SwapRooms
    {
        const string ScenePath = "Assets/_Project/Scenes/Floor_01.unity";
        const string RequestFile = "Temp/p2_swap_rooms.request";

        static Floor01SwapRooms()
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
            Swap();
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            RunRequest();
        }

        [MenuItem("Overworked/P2/Swap Floor_01 Rooms 101 and 102")]
        public static void Swap()
        {
            // Несохранённые правки в открытой сцене — спросить, иначе OpenScene их выбросит.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform env = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Environment") env = root.transform;
            var doors = env != null ? env.Find("Corridor/Doors") : null;
            var returns = env != null ? env.Find("Corridor/ReturnPoints") : null;
            var room101 = env != null ? env.Find("Rooms/Room_101") : null;
            var room102 = env != null ? env.Find("Rooms/Room_102") : null;
            var door101 = doors != null ? doors.Find("Door_Room_101") : null;
            var door102 = doors != null ? doors.Find("Door_Room_102") : null;
            if (room101 == null || room102 == null || door101 == null || door102 == null)
            {
                Debug.LogError("[Setup] Floor_01: нет Room_101/Room_102 или их дверей в Corridor/Doors");
                return;
            }

            // --- Двери в коридоре: 102 — ближе к началу (меньший X), 101 — дальше.
            float nearX = Mathf.Min(door101.localPosition.x, door102.localPosition.x);
            float farX = Mathf.Max(door101.localPosition.x, door102.localPosition.x);
            SetX(door102, nearX);
            SetX(door101, farX);
            // Подписи над дверями — по тексту «101» / «102».
            foreach (Transform child in doors)
            {
                if (!child.TryGetComponent(out TMP_Text label)) continue;
                if (label.text.Trim() == "102") SetX(child, nearX);
                else if (label.text.Trim() == "101") SetX(child, farX);
            }
            // Точки возврата — напротив своих дверей.
            if (returns != null)
            {
                var back101 = returns.Find("Room_101");
                var back102 = returns.Find("Room_102");
                if (back102 != null) SetX(back102, nearX);
                if (back101 != null) SetX(back101, farX);
            }

            // --- Сами комнаты под коридором: 102 — верхняя, 101 — нижняя (со всем содержимым).
            float upperY = Mathf.Max(room101.localPosition.y, room102.localPosition.y);
            float lowerY = Mathf.Min(room101.localPosition.y, room102.localPosition.y);
            room102.localPosition = new Vector3(room102.localPosition.x, upperY, room102.localPosition.z);
            room101.localPosition = new Vector3(room101.localPosition.x, lowerY, room101.localPosition.z);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(previous) && previous != ScenePath)
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log($"[Setup] Floor_01: комнаты поменяны — дверь 102 на X={nearX}, дверь 101 на X={farX}");
        }

        // Подписи — RectTransform (TextMeshPro): их место задаёт anchoredPosition, не localPosition.
        static void SetX(Transform t, float x)
        {
            if (t is RectTransform rt) rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
            else t.localPosition = new Vector3(x, t.localPosition.y, t.localPosition.z);
        }
    }
}
