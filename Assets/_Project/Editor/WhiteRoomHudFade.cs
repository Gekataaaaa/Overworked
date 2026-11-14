using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Overworked.EditorTools
{
    /// <summary>
    /// P3: кладёт в сцену WhiteRoom объект P3_HudFade с HudFadeWithOverlay и связывает его с белым слоем
    /// WhiteRoomSequence (P4) — HUD гаснет вместе с белым. Меню Overworked → P3 → Add HUD Fade To White Room.
    /// Объекты P4 не трогает. После пересборки сцены сборщиком P4 (Build White Room) запустить ещё раз:
    /// белый слой создаётся заново, и ссылка на него теряется.
    /// </summary>
    public static class WhiteRoomHudFade
    {
        const string ScenePath = "Assets/_Project/Scenes/WhiteRoom.unity";
        const string RootName = "P3_HudFade";

        [MenuItem("Overworked/P3/Add HUD Fade To White Room")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var sequence = Object.FindAnyObjectByType<WhiteRoomSequence>();
            var overlay = sequence != null
                ? new SerializedObject(sequence).FindProperty("whiteOverlay").objectReferenceValue as Graphic
                : null;
            if (overlay == null)
            {
                Debug.LogError("[Setup] в WhiteRoom нет WhiteRoomSequence с белым слоем — сначала Overworked → P4 → Build White Room");
                return;
            }

            foreach (var go in scene.GetRootGameObjects())
                if (go.name == RootName) Object.DestroyImmediate(go);

            var fade = new GameObject(RootName).AddComponent<HudFadeWithOverlay>();
            var so = new SerializedObject(fade);
            so.FindProperty("overlay").objectReferenceValue = overlay;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Setup] WhiteRoom: HUD гаснет вместе с белым слоем (P3_HudFade)");
        }
    }
}
