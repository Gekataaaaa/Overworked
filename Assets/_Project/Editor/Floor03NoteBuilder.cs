using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Overworked.EditorTools
{
    /// <summary>
    /// P3: записка Чарльза на Floor_03 (нарратив 1.1) — объект P3_CharlesNote на столе Чарльза
    /// с NoteInteractable (P2): «[E] Take the note» → текст на весь экран → тост «Received: Charles's note.»
    /// → реплики Дэнни F3_CharlesNote_After. Кладётся в группу Interactables (не в Chain — её пересобирает P1).
    /// Меню Overworked → P3 → Add Charles Note To Floor 03; повторный запуск пересоздаёт только P3_CharlesNote.
    /// </summary>
    public static class Floor03NoteBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Floor_03.unity";
        const string RootName = "P3_CharlesNote";
        const string NoteText = "THE BOSS DOESN'T LIKE IT WHEN I LEAVE BEFORE HIM!!!";

        [MenuItem("Overworked/P3/Add Charles Note To Floor 03")]
        public static void Run()
        {
            // Сцену открываем ДО загрузки ассетов: OpenScene(Single) выгружает загруженные раньше ассеты.
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var after = AssetDatabase.LoadAssetAtPath<DialogueData>("Assets/_Project/Data/Dialogues/F3_CharlesNote_After.asset");
            var square = PlayerSandboxBuilder.EnsureSquareSprite();

            Transform desk = null;
            GameObject interactables = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Interactables") interactables = root;
                if (desk == null) desk = FindDeep(root.transform, "CharlesDesk");
            }
            if (desk == null)
            {
                Debug.LogError("[Setup] на Floor_03 нет CharlesDesk — сначала Overworked → Build Game Chain → Floor 03");
                return;
            }
            if (interactables == null) interactables = new GameObject("Interactables");
            var old = interactables.transform.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // Стол — растянутый квадрат: верх стола = центр + половина высоты.
            var deskTop = desk.position + new Vector3(0f, desk.lossyScale.y * 0.5f, 0f);

            var note = new GameObject(RootName);
            note.transform.SetParent(interactables.transform, false);
            note.transform.position = new Vector3(deskTop.x + 0.5f, deskTop.y + 0.25f, 0f);

            // Жёлтый листок, приклеенный к монитору (заглушка до арта).
            var paper = PlayerSandboxBuilder.Rect(note, "Paper", square, Vector2.zero, new Vector2(0.35f, 0.35f),
                new Color(0.95f, 0.88f, 0.45f), 4);
            paper.transform.localRotation = Quaternion.Euler(0, 0, 8f);

            // Зона взаимодействия: Дэнни стоит на полу у стола — по высоте до пола.
            var col = note.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2f, 3.5f);
            col.offset = new Vector2(0f, -1.2f);

            var interact = note.AddComponent<NoteInteractable>();
            var so = new SerializedObject(interact);
            so.FindProperty("prompt").stringValue = "Take the note";
            so.FindProperty("noteText").stringValue = NoteText;
            so.FindProperty("receivedToast").stringValue = "Received: Charles's note.";
            so.FindProperty("afterDialogue").objectReferenceValue = after;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Setup] Floor_03: записка Чарльза на столе (P3_CharlesNote)");
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                var r = FindDeep(c, name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
